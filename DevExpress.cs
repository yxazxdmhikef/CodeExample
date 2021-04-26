using System;
using System.Linq;
using System.Text;
using DevExpress.ExpressApp;
using DevExpress.Data.Filtering;
using System.Collections.Generic;
using DevExpress.Persistent.Base;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Layout;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Templates;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.PivotGrid.Win;
using DevExpress.XtraPivotGrid;
using ComBase;
using System.Collections;

////////////////////////////////////////////////
/////
///// кту по проектам для руководителя проектов
/////
///////////////////////////////////////////////
namespace Modules.KTU.Controllers
{
    // For more typical usage scenarios, be sure to check out http://documentation.devexpress.com/#Xaf/clsDevExpressExpressAppViewControllertopic.
    public partial class KtuProjectManagerViewController : ViewController
    {

        private PivotGridControl PV = null;
        public KtuProjectManagerViewController()
        {
            InitializeComponent();
            RegisterActions(components);
            // Target required Views (via the TargetXXX properties) and create their Actions.
        }
        protected override void OnActivated()
        {
            base.OnActivated();
            // Perform various tasks depending on the target View.
            try
            {
                if (View == null)
                    return;
                View.AllowNew.SetItemValue("Ktu.AllowNew", false);
                View.AllowEdit.SetItemValue("Ktu.AllowEdit", false);
                View.AllowDelete.SetItemValue("Ktu.AllowDelete", false);
                    
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError(ex.ToString()); }

        }

        protected override void OnViewControlsCreated()
        {
            base.OnViewControlsCreated();
            // Access and customize the target View control.
            try
            {
                if (View == null )  return;
                DevExpress.ExpressApp.ListView lv = View as DevExpress.ExpressApp.ListView;
                if (lv == null) return;                
                PivotGridListEditor editor = lv.Editor as PivotGridListEditor;
                if (editor == null) {
                    if (lv.Model != null)
                    {
                        editor = new PivotGridListEditor(lv.Model);
                        lv.Editor = editor;
                    }
                    else return;
                }               
                    
                if (editor.PivotGridControl != null) PV = editor.PivotGridControl;
                else { PV = new PivotGridControl(); PV.DataSource = editor.DataSource; }
                    if (PV == null) return;

                    PV.FieldAreaChanging += PV_FieldAreaChanging;
                    PV.PopupMenuShowing += PV_PopupMenuShowing;
                    PV.CustomCellValue += PV_CustomCellValue;                    

                    PV.OptionsView.ShowTotalsForSingleValues = true;
                    PV.OptionsView.ShowGrandTotalsForSingleValues = true;
                    PV.OptionsView.ShowRowGrandTotalHeader = true;
                    PV.OptionsView.ShowRowGrandTotals = true;
                    PV.OptionsView.ShowRowTotals = false;
                    PV.OptionsView.ShowColumnTotals = true;
                    PV.OptionsView.ShowColumnGrandTotals = true;
                    PV.OptionsView.ShowColumnGrandTotalHeader = true;                                 

                    PV.Fields.Clear();
                    // в строку: сотрудник и его должность и фот ручками
                    PV.Fields.Add(new PivotGridField() { Name = "Person", FieldName = "Person.ShortName", Area = PivotArea.RowArea, Caption = "Сотрудник" });
                    PV.Fields.Add(new PivotGridField() { Name = "StaffNumber", FieldName = "DeptPost.StaffNumber", Area = PivotArea.RowArea, Caption = "Табельный номер"});
                    PV.Fields.Add(new PivotGridField() { Name = "DeptPost", FieldName = "DeptPost.idPost.PostName", Area = PivotArea.RowArea, Caption = "Должность" });
                    DevExpress.XtraEditors.Repository.RepositoryItemSpinEdit decimaledit = new DevExpress.XtraEditors.Repository.RepositoryItemSpinEdit();
                    //колонки из проектов и этапов этих проектов (вложенные банды)               
                    PV.Fields.Add(new PivotGridField() { Name = "Project", FieldName = "Project.Name", Area = PivotArea.ColumnArea, Caption = "Проект" });
                    PV.Fields.Add(new PivotGridField() { Name = "Task", FieldName = "Task.Name", Area = PivotArea.ColumnArea, Caption = "Этап" });
                    //значение в срезе - кту
                    PivotGridField ktu = new PivotGridField();
                    ktu.Name = "ktu";
                    ktu.FieldName = "KTU";
                    ktu.Area = PivotArea.DataArea;
                    ktu.AreaIndex = 0;
                    ktu.Caption = "КТУ";
                    ktu.CellFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                    ktu.CellFormat.FormatString = "N2";
                    //editor для редактирования                
                    PV.RepositoryItems.Add(decimaledit);
                    ktu.FieldEdit = decimaledit;
                    ktu.Options.AllowEdit = false;
                    ktu.Options.ShowTotals = true;
                    ktu.Options.ShowGrandTotal = true;
                   
                    PV.Fields.Add(ktu);

                    PV.Fields.Add(new PivotGridField() { Name = "Month", FieldName = "Form.Period.PeriodMounth", Area = PivotArea.FilterArea, Caption = "Месяц", AreaIndex = 0 });
                    PV.Fields.Add(new PivotGridField() { Name = "Year", FieldName = "Form.Period.PeriodYear", Area = PivotArea.FilterArea, Caption = "Год", AreaIndex = 1 });
                    //по подразделениям
                    PV.Fields.Add(new PivotGridField() { Name = "Dept", FieldName = "Form.DeptBook.SubjDept.SubjName", Area = PivotArea.RowArea, Caption = "Подразделение", AreaIndex = 0 });
                    //связанная колонка с выражением - фот по проекту у сотрудника
                    PivotGridField fot = new PivotGridField();
                    fot.Name = "fot";
                    fot.FieldName = "SumFOT";
                    fot.Area = PivotArea.DataArea;
                    fot.Caption = "ФОТ";
                    fot.CellFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
                    fot.CellFormat.FormatString = "C";
                    fot.Visible = true;
                    fot.SummaryType = DevExpress.Data.PivotGrid.PivotSummaryType.Sum;
                    fot.Options.ShowValues = false;
                    fot.Options.ShowGrandTotal = true;
                    fot.Options.ShowTotals = true;
                    PV.Fields.Add(fot);
                    PV.Visible = true;
                
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError(ex.ToString()); }
        }

        /// <summary>
        /// отобразим сумму ФОТ по проекту только в итоговой строке 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PV_CustomCellValue(object sender, PivotCellValueEventArgs e)
        {
           
            if (e != null && e.DataField != null && e.DataField.FieldName == "SumFOT" && e.ColumnValueType != PivotGridValueType.Value && e.RowValueType == PivotGridValueType.Value)
            {
                e.Value = 0;

            }
        }
        protected override void OnDeactivated()
        {
            
            base.OnDeactivated();
            if (PV == null) return;
            PV.FieldAreaChanging -= PV_FieldAreaChanging;
            PV.PopupMenuShowing -= PV_PopupMenuShowing;
            PV.CustomCellValue -= PV_CustomCellValue;
        }

        /// <summary>
        /// Вызов меню
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PV_PopupMenuShowing(object sender, PopupMenuShowingEventArgs e)
        {
            //блокировка меню "Показать список полей", "Вызвать мастер" и "Сброс параметров"
            if (e != null && e.Menu != null)
                foreach (DevExpress.Utils.Menu.DXMenuItem item in e.Menu.Items)
                {
                    if ("показать список полей, вызвать мастер, сброс параметров".Contains(item.Caption.ToLower()))
                        item.Enabled = false;
                }
        }


        /// <summary>
        /// запрет на перетаскивание полей
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PV_FieldAreaChanging(object sender, PivotAreaChangingEventArgs e)
        {
            e.Allow = false;
        }

    }
}
