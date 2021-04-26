(function ($) {
    // Shorten references to variables which is better for uglification. kendo = window.kendo,
    ui = kendo.ui,
    Widget = ui.Widget,
    TreeList = ui.TreeList,
    proxy = $.proxy,
    grep = $.grep,
        Query = kendo.data.Query;

    var CHECKBOX = 'k-checkbox';
    var CHECKBOXINPUT = 'input[data-role=\'checkbox\'].' + CHECKBOX;

    //Кастомный виджет иерархичной таблицы с пагинацией на основе kendo TreeList
    var TreeGrid = TreeList.extend({
        init: function (element, options) {
            // The base call to initialize the widget.	            
            TreeList.fn.init.call(this, element, options);
            this._pageable();
            this._setContentHeight();
            this.dataSource._queryProcess = this._queryProcess;
            this.dataSource.rootNodes = this.rootNodes;                

            this.dataSource.tableID = element.id;
            this.serverMode = true;
            this.expandAll = options.dataSource.schema.model.expanded;
            this.toggleItemsKeys = [];
            this._selectedKeys = [];
            pt = kendo.support.isRtl(element);
        },
        options: {
            // The name is what it will appear as the kendo namespace(i.e. kendo.ui.MyWidget).
            // The jQuery plugin would be jQuery.fn.kendoMyWidget.
            name: "TreeGrid"
            // Other options go here.            
        },
        _pageable: function () {
            var that = this, wrapper,
                pageable = that.options.pageable;
            if (pageable) {
                wrapper = that.wrapper.children('div.k-grid-pager');
                if (!wrapper.length) {
                    wrapper = $('<div class="k-pager-wrap k-grid-pager"/>').appendTo(that.wrapper);
                }
                if (that.pager) {
                    that.pager.destroy();
                }
                if (typeof pageable === 'object' && pageable instanceof kendo.ui.Pager) {
                    that.pager = pageable;
                } else {
                    that.pager = new kendo.ui.Pager(wrapper, that.element.extend({}, pageable, { dataSource: that.dataSource }));
                }
                that.pager.bind('pageChange', function (e) {
                    if (that.trigger('page', { page: e.index })) {
                        e.preventDefault();
                    }
                });
            }
        },
        _togglePagerVisibility: function () {
            var that = this;
            if (that.options.pageable.alwaysVisible === false) {
                that.wrapper.find('.k-grid-pager').toggle(that.dataSource.total() >= that.dataSource.pageSize());
            }
        },
        _setContentHeight: function () {
            var outerHeight = kendo._outerHeight;
            var that = this,
                options = that.options,
                height = $(".k-pane").height(),
                header = that.wrapper.children('.k-grid-header'),
                scrollbar = kendo.support.scrollbar(),
                k_toolbar = $(".k-pane").find('.k-toolbar');

            if (options.scrollable && that.wrapper.is(':visible')) {
                height -= outerHeight(header);
                if (that.pager && that.pager.element.is(':visible')) {
                    height -= outerHeight(that.pager.element);
                }
                if (options.groupable) {
                    height -= outerHeight(that.wrapper.children('.k-grouping-header'));
                }
                if (options.toolbar) {
                    height -= outerHeight(that.wrapper.children('.k-grid-toolbar'));
                }
                if (that.footerTemplate) {
                    height -= outerHeight(that.wrapper.children('.k-grid-footer'));
                }
                if (k_toolbar.length > 0) {
                    height -= outerHeight(k_toolbar);
                }
            }
            that.content.height(height);
        },
        _resize: function () {
            this._applyLockedContainersWidth();
            this._adjustHeight();
            this._setContentHeight();
        },
        _queryProcess: function (data, options) {
            options = options || {};
            options.filterCallback = proxy(this._filterCallback, this);
            var defaultParentId = this._defaultParentId();
            var result = Query.process(data, options);
            var map = this._childrenMap(result.data);
            var hasLoadedChildren, i, item, children;
            
            var toggleItemsKeys = [];
            if (this.tableID) {                 
                toggleItemsKeys = jQuery('#' + this.tableID).data('kendoTreeGrid').toggleItemsKeys;
            }

            for (i = 0; i < data.length; i++) {
                item = data[i];
                if (item && toggleItemsKeys && toggleItemsKeys.indexOf(item.ID) > -1)
                    item.expanded = !this.options.schema.model.expanded;
                else
                    item.expanded = this.options.schema.model.expanded;


                if (item.id === defaultParentId) {
                    continue;
                }
                children = map[item.id];
                hasLoadedChildren = !!(children && children.length);
                if (!item.loaded()) {
                    item.loaded(hasLoadedChildren || !item.hasChildren);
                }
                if (item.loaded() || item.hasChildren !== true) {
                    item.hasChildren = hasLoadedChildren;
                }
                //if (hasLoadedChildren) {
                //    data = data.slice(0, i + 1).concat(children, data.slice(i + 1));
                //}
            }
            result.data = data;
            return result;
        },
        //получение корневых нод
        rootNodes: function () {
            //return this._byParentId(this._defaultParentId());
            var result = [];
            var view = this.view();
            var ids = view.map(function (i) { return i.ID; });
            var parents = view.map(function (i) { return i.parentId; });
            result = view.filter(function (f) {
                return !ids.includes(f.parentId);
            });
            return result;
        },
        //рендеринг строки
        _trs: function (options) {
            var model, attr, className, hasChildren, childNodes, i, length;
            var rows = [];
            //var level = options.level;
            var data = options.data;
            var dataSource = this.dataSource;
            var aggregates = dataSource.aggregates() || {};
            var columns = options.columns;
            for (i = 0, length = data.length; i < length; i++) {
                className = [];
                model = data[i];
                childNodes = model.loaded() && dataSource.childNodes(model);
                hasChildren = model.HasChildren && model.HasChildren > 0;//childNodes && childNodes.length;
                //костыль нужен именно с нижнего регистра 
                model.hasChildren = hasChildren;
                attr = { 'role': 'row' };
                attr[kendo.attr('uid')] = model.uid;
                if (hasChildren) {
                    attr['aria-expanded'] = !!model.expanded;
                }
                if (options.visible) {
                    if (this._absoluteIndex % 2 !== 0) {
                        className.push('k-alt');
                    }
                    this._absoluteIndex++;
                } else {
                    attr.style = { display: 'none' };
                }
                if ($.inArray(model.uid, options.selected) >= 0) {
                    className.push('k-state-selected');
                }
                if (hasChildren) {
                    className.push('k-treelist-group');
                }
                if (model._edit) {
                    className.push('k-grid-edit-row');
                }
                if (!model.Level) {
                    model.Level = 0;
                }
                attr.className = className.join(' ');
                rows.push(this._tds({
                    model: model,
                    attr: attr,
                    level: model.Level
                }, columns, proxy(this._td, this)));
                if (hasChildren) {
                    rows = rows.concat(this._trs({
                        columns: columns,
                        aggregates: aggregates,
                        selected: options.selected,
                        visible: options.visible && !!model.expanded,
                        data: childNodes,
                        level: model.Level + 1
                    }));
                }
            }
            if (this._hasFooterTemplate()) {
                attr = {
                    className: 'k-footer-template',
                    'data-parentId': model.parentId
                };
                if (!options.visible) {
                    attr.style = { display: 'none' };
                }
                rows.push(this._tds({
                    model: aggregates[model.parentId],
                    attr: attr,
                    level: level
                }, columns, this._footerTd));
            }
            return rows;
        },
        //определение колонки раскрытия
        _ensureExpandableColumn: function () {
            function is(field) {
                return function (object) {
                    return object[field];
                };
            }
            function not(func) {
                return function (object) {
                    return !func(object);
                };
            }

            if (this._autoExpandable) {
                delete this._autoExpandable.expandable;
            }
            var vColumns = $.grep(this.columns, not(is('hidden')));
            var visibleColumns = $.grep(vColumns, not(is('selectable')));
            var expandableColumns = grep(visibleColumns, is('expandable'));
            if (this.columns.length && !expandableColumns.length) {
                this._autoExpandable = visibleColumns[0];
                visibleColumns[0].expandable = true;
            }
        },
        //инициализация колонок
        _columns: function () {
            var columns = this.options.columns || [];
            this.columns = $.map(columns, function (column) {
                column = typeof column === 'string' ? { field: column } : column;
                return $.extend({ encoded: true }, column);
            });
            var lockedColumns = this._lockedColumns();
            if (lockedColumns.length > 0) {
                this._hasLockedColumns = true;
                this.columns = lockedColumns.concat(this._nonLockedColumns());
            }
            this._ensureExpandableColumn();
            this._columnTemplates();
            this._columnAttributes();
                        
            this._checkBoxSelection = true;
            this.element.on('click.kendoTreeGrid', 'tbody > tr ' + CHECKBOXINPUT, proxy(this._checkboxClick, this));
            this.element.on('click.kendoTreeGrid', 'thead > tr ' + CHECKBOXINPUT, proxy(this._headerCheckboxClick, this));

        },
        _headerCheckboxClick: function (e) {
            var that = this, checkBox = $(e.target),
                checked = checkBox.prop('checked'),
                parentGrid = checkBox.closest('.k-grid.k-widget').getKendoTreeGrid();
            if (that !== parentGrid) {
                return;
            }
            if (checked) {
                that.select(parentGrid.items());
            } else {
                that.clearSelection();
            }
        },
        _checkboxClick: function (e) { 
            var that = this, row = $(e.target).closest('tr'),
                isSelecting = !row.hasClass('k-state-selected'),
                dataItem = that.dataItem(row), children = [], selector = '';
            if (that !== row.closest('.k-grid.k-widget').getKendoTreeGrid()) {
                return;
            }
            if (that._includeChildren) {
                that.dataSource.allChildNodes(dataItem, children);
                for (var i = 0; i < children.length; i++) {
                    selector += 'tr[data-uid=\'' + children[i].uid + '\'],';
                }
            }
            selector += 'tr[data-uid=\'' + dataItem.uid + '\']';
            row = $(selector);
            if (isSelecting) {
                that.select(row);
                that.trigger('change');
            } else {
                that._deselectCheckRows(row);
            }
        },
        _deselectCheckRows: function (items) {
            var that = this;
            items = that.table.add(that.lockedTable).find(items);
            if (that._isLocked()) {
                items = items.add(items.map(function () {
                    return that._relatedRow(this);
                }));
            }
            items.each(function () {
                $(this).removeClass('k-state-selected').find(CHECKBOXINPUT).attr('aria-checked', false).prop('checked', false).attr('aria-label', 'Select row');
                var itemID = that._getRowID(this);
                if (that._selectedKeys.indexOf(itemID) > -1)
                    that._selectedKeys.splice(that._selectedKeys.indexOf(itemID), 1);
            });
            that._toggleHeaderCheckState(false);
            that.trigger('change');
        },       
        _toggleHeaderCheckState: function (checked) {
            var that = this;
            if (checked) {
                that.thead.add(that.lockedHeader).find('tr ' + CHECKBOXINPUT).prop('checked', true).attr('aria-checked', true).attr('aria-label', 'Deselect all rows');
            } else {
                that.thead.add(that.lockedHeader).find('tr ' + CHECKBOXINPUT).prop('checked', false).attr('aria-checked', false).attr('aria-label', 'Select all rows');
            }
            that.trigger('change');
        },        
        _change: function () {
            var that = this;
            var selectedValues;
            if (that._checkBoxSelection) {
                selectedValues = that.selectable.value();
                that._uncheckCheckBoxes();
                that._selectedKeys = [];
                that._checkRows(selectedValues);
                if (selectedValues.length && selectedValues.length === that.items().length) {
                    that._toggleHeaderCheckState(true);
                } else {
                    that._toggleHeaderCheckState(false);
                }
            }
            this.trigger('change');
        },
        _uncheckCheckBoxes: function () {
            var that = this;
            var tables = that.table.add(that.lockedTable);
            tables.find('tbody ' + CHECKBOXINPUT).attr('aria-checked', false).prop('checked', false).attr('aria-label', 'Select row');
        },
        select: function (value) {
            var that = this;
            var selectable = this.selectable;
            if (that._checkBoxSelection) {
                if (value) {
                    that._checkRows(value);
                    if (that.select().length === that.items().length) {
                        that._toggleHeaderCheckState(true);
                    }
                }
                return that.items().filter('.k-state-selected');
            }
            if (!selectable) {
                return $();
            }
            if (typeof value !== 'undefined') {
                if (!selectable.options.multiple) {
                    selectable.clear();
                    that._selectedKeys = [];
                    value = value.first();
                }
                if (this._hasLockedColumns) {
                    value = value.add($.map(value, proxy(this._relatedRow, this)));
                }
            }
            return selectable.value(value);
        },
        _checkRows: function (items) {
            var that = this;
            items.each(function () {
                $(this).addClass("k-state-selected").find("input[data-role='checkbox'].k-checkbox").prop('checked', true).attr('aria-label', 'Deselect row').attr('aria-checked', true);
                var itemID = that._getRowID(this);
                if (that._selectedKeys && that._selectedKeys.indexOf(itemID) === -1)
                    that._selectedKeys.push(itemID);
            });
        },
        clearSelection: function () {
            var that = this;
            var selected = this.select();
            if (that.selectable && !that._checkBoxSelection) {
                that.selectable.clear();
            }
            if (that._checkBoxSelection) {
                that._deselectCheckRows(that.select());
                return;
            }
            if (selected.length || that._checkBoxSelection) {
                that.trigger('change');
            }
        },
        _toggle: function (model, expand) {
            var defaultPromise = $.Deferred().resolve().promise();
            var loaded = model.loaded();
            if (model._error) {
                model.expanded = false;
                model._error = undefined;
            }
            if (!loaded && model.expanded) {
                return defaultPromise;
            }
            if (typeof expand == 'undefined') {
                expand = !model.expanded;
            }
            model.expanded = expand;
            if (model.expanded === this.expandAll) {
                if (this.toggleItemsKeys.indexOf(model.ID) > -1)
                    this.toggleItemsKeys.splice(this.toggleItemsKeys.indexOf(model.ID), 1);
                if (!model.expanded) {
                    this.collapseChildren(model.ID);
                }
            }
            else {
                if (this.toggleItemsKeys.indexOf(model.ID) === -1)
                    this.toggleItemsKeys.push(model.ID);
            }
            if (!loaded) {
                defaultPromise = this.dataSource.load(model).always(proxy(function () {
                    this._render();
                    this._syncLockedContentHeight();
                }, this));
            }
            if (this.serverMode) {
                var readData = {
                    expandAll: this.expandAll,
                    itemKeys: this.toggleItemsKeys.join(';')
                };
                this.dataSource.transport.options.read.data = function () {
                    return readData;
                };
                this.dataSource.read();
                return;
            }

            this._render();
            this._syncLockedContentHeight();
            return defaultPromise;
        },  
        collapseChildren: function(parentID) {
            var childsID = this.dataSource.data().filter(function (f) { if (f.ParentID === parentID) return f; }).map(function (s) { return s.ID; });
            this.toggleItemsKeys = this.toggleItemsKeys.filter(function (f) { return !childsID.includes(f); });
            for (var i = 0; i < childsID.length; i++) {
                this.collapseChildren(childsID[i]);
            }

        },
        selectedKeyNames: function () {
            var that = this,
                ids = [],
                items = that.select(),
                modelId = that.dataSource.options.schema.model.id;

            if (that.selectable.options.multiple)
                return that._selectedKeys;

            for (var i = 0; i < items.length; i++) {
                ids.push(that._getRowID(items[i]));
            }
            ids.sort();
            return ids;
        },
        _getRowID: function (tr) {
            var that = this,
                modelId = that.dataSource.options.schema.model.id;
            var obj = that.dataItem(tr);
            if (obj && obj[modelId])
                return obj[modelId];
            return null;
        },       
        _positionResizeHandle: function (n) {
         //HACK: переопределение метода из-за некорректного вычисления позиции. Исправлено в новых версиях kendo
         //ниже представлен код версии 2021.1.224 с модификацией под текущую
         //https://kendo.cdn.telerik.com/2021.1.224/js/kendo.all.min.js
            function r(n) {
                var i = kendo.attr("index");
                return n.sort(function (n, o) {
                    var r, a;
                    return n = $(n),
                        o = $(o),
                        r = n.attr(i),
                        a = o.attr(i),
                        r === t && (r = $(n).index()),
                        a === t && (a = $(o).index()),
                        r = parseInt(r, 10),
                        a = parseInt(a, 10),
                        r > a ? 1 : r < a ? -1 : 0;
                });
            }
            function a(t) {
                var n = t.find(">tr:not(.k-filter-row)")
                    , i = function () {
                        var t = $(this);
                        return !t.hasClass("k-group-cell") && !t.hasClass("k-hierarchy-cell")
                    }
                    , o = $();
                return n.length > 1 && (o = n.find("th[data-index]").filter(i)),
                    o = o.add(n.last().find("th").filter(i)),
                    r(o);
            }


            var Se = '.kendoTreeGrid';
            var e = $;
            var i, o, r, s, l, d, c, u, h, p, f, t,
                m = e(n.currentTarget),
                g = this.resizeHandle,
                v = m.position(), _ = 0,
                b = kendo._outerWidth(m),
                w = m.closest("div"),
                k = t !== n.buttons ? n.buttons : n.which || n.button,
                y = this.options.columnResizeHandleWidth || 3, x = 3 * y / 2, C = b;
            if (t === k || 0 === k) {
                if (g || (g = this.resizeHandle = e('<div class="k-resize-handle"><div class="k-resize-handle-inner"></div></div>')),
                    h = a(m.closest("thead")).filter(":visible"),
                    pt)
                    u = kendo.scrollLeft(w),
                        (se.mozilla || se.webkit && se.version >= 85) && (u *= -1),
                        c = parseFloat(w.css("borderLeftWidth")),
                        C = m.offset().left + u - parseFloat(m.css("marginLeft")) - (w.offset().left + c),
                        _ = C <= u ? x : 0,
                        i = m.closest(".k-grid-header-wrap, .k-grid-header-locked"),
                        d = i[0].scrollWidth - i[0].offsetWidth,
                        l = parseFloat(i.css("marginLeft")),
                        o = se.msie ? 2 * kendo.scrollLeft(i) + c - l - _ : 0,
                        r = se.webkit && se.version < 85 ? d - _ - l + c : -_,
                        s = se.mozilla ? c - l - _ : 0,
                        C -= r + s + o;
                else
                    for (p = 0; p < h.length && h[p] != m[0]; p++)
                        C += h[p].offsetWidth;
                w.append(g),
                    g.show().css({
                        top: v.top,
                        left: C - x,
                        height: kendo._outerHeight(m),
                        width: 3 * y
                    }).data("th", m),
                    f = this,
                    g.off("dblclick" + Se).on("dblclick" + Se, function () {
                        var t = m.index();
                        e.contains(f.thead[0], m[0]) && (t += me(f.columns, function (e) {
                            return e.locked && !e.hidden;
                        }).length),
                            f.autoFitColumn(t);
                    });
            }
        },
        createNewDataSource: function (options) {
            //создание нового экземпляра DataSource
            //с копированием необходимых функций
            //используется в bind при установлении фильтров и сортировки из пресета
            //https://stackanswers.net/questions/kendoui-programmatically-setting-grid-sort
            var newDS = new kendo.data.TreeListDataSource(options);
            newDS._queryProcess = this._queryProcess;
            newDS.rootNodes = this.rootNodes;
            newDS.tableID = this.dataSource.tableID;
            return newDS;
        }
       
    });

    ui.plugin(TreeGrid);

})(jQuery);