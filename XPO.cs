using ComBase;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Expert.Module.Models.Project.layerModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//Individual Performance Factor
//коэффициент трудового участия

namespace Modules.KTU
{
    [Persistent(@"KTU.Ktu")]
    [ModelDefault("Caption", @"КТУ")]
    [VisibleInReports(true)]
    [ImageName(@"Ktu")]
    public class Ktu : XPCustomObject
    {
        private Guid fOid;
        [Key(true), VisibleInDetailView(false), VisibleInListView(false), VisibleInLookupListView(false)]
        public Guid Oid
        {
            get { return fOid; }
            set { SetPropertyValue<Guid>("Oid", ref fOid, value); }
        }

        private DateTime fStart;
        [DisplayName(@"С")]
        [VisibleInDetailView(false), VisibleInListView(false), VisibleInLookupListView(false)]
        public DateTime Start
        {
            get { return fStart; }
            set { SetPropertyValue<DateTime>("Start", ref fStart, value); }
        }       

        private DateTime fEnd;
        [DisplayName(@"По")]
        [VisibleInDetailView(false), VisibleInListView(false), VisibleInLookupListView(false)]
        public DateTime End
        {
            get { return fEnd; }
            set { SetPropertyValue<DateTime>("End", ref fEnd, value); }
        }

        private subj_Dept fSubjDept;
        [DisplayName(@"Подразделение")]
        [VisibleInDetailView(false), VisibleInListView(false), VisibleInLookupListView(false)]
        public subj_Dept SubjDept
                    {
            get { return fSubjDept; }
            set { SetPropertyValue<subj_Dept>("SubjDept", ref fSubjDept, value); }
            }

        private subj_Person fPerson;
        [DisplayName(@"Сотрудник")]      
        [RuleRequiredField("KTU_Person", "Save", "Не заполнено поле 'Сотрудник'")]
        [VisibleInDetailView(false), VisibleInListView(false), VisibleInLookupListView(false)]
        public subj_Person Person
        {
            get { return fPerson; }
            set { SetPropertyValue<subj_Person>("Person", ref fPerson, value); }
        }

        private subj_DeptPost fDeptPost;
        [DisplayName(@"Должность")]
        [VisibleInDetailView(false), VisibleInListView(false), VisibleInLookupListView(false)]
        public subj_DeptPost DeptPost
        {
            get { return fDeptPost; }
            set { SetPropertyValue<subj_DeptPost>("DeptPost", ref fDeptPost, value); }
        }

       
        private project_Project fProject;
        [DisplayName(@"Проект")]
        [VisibleInDetailView(false), VisibleInListView(false), VisibleInLookupListView(false)]   
        public project_Project Project
        {
            get { return fProject; }
            set { 

                SetPropertyValue<project_Project>("Project", ref fProject, value);
                if (this.fProject != null && !String.IsNullOrEmpty(this.fProject.Name))
                    this.PrName = this.fProject.Name;
        }        
        }
    
        private String fPrName;
        [DisplayName(@"Наименование проекта")]
        [VisibleInDetailView(false), VisibleInListView(false), VisibleInLookupListView(false)]
        [Size(255)]
        public String PrName
        {
            get { return fPrName; }
            set { SetPropertyValue<String>("PrName", ref fPrName, value); }
        }
                
        private project_ProjectTask fTask;
        [DisplayName(@"Этап")]
        [DataSourceProperty("Project.ProjectProjectTasks")]
        [DataSourceCriteriaProperty("IdTaskType.Code = 'Stage'")]
        [VisibleInDetailView(false), VisibleInListView(false), VisibleInLookupListView(false)]
        public project_ProjectTask Task
        {
            get { return fTask; }
            set { 
                SetPropertyValue<project_ProjectTask>("Task", ref fTask, value);
                if (this.fTask != null && !String.IsNullOrEmpty(this.fTask.Name))
                    this.TskName = this.fTask.Name;
        }
        }

        private String fTskName;
        [DisplayName(@"Наименование этапа")]
        [VisibleInDetailView(false), VisibleInListView(false), VisibleInLookupListView(false)]
        [Size(255)]
        public String TskName
        {
            get { return fTskName; }
            set { SetPropertyValue<String>("TskName", ref fTskName, value); }
        }

        private decimal fKTU;        
        [DisplayName("КТУ")]
        [ImmediatePostData(true)]
        [EditorAlias("KTU")]
        [VisibleInDetailView(false), VisibleInListView(false), VisibleInLookupListView(false)]
        public decimal KTU
        {
            get { return fKTU; }
            set { SetPropertyValue<decimal>("KTU", ref fKTU, value); }
        }

        
        // значение, вычисляемое по формуле:
        // ∑ФОТс/с = КТУ*(Начисления (с НДФЛ) - Все виды начислений, не подлежащих отнесению на себестоимость проектов(см ЧТЗ)
        /// <summary>
        /// Сумма ФОТ сотрудника по этапу
        /// </summary>
        private decimal fSumFOT;
        [DisplayName(@"ФОТ")]
        [ModelDefault("DisplayFormat", "{0:N6}")]
        [ModelDefault("EditMask", "N6")]
        [VisibleInDetailView(false), VisibleInListView(false), VisibleInLookupListView(false)]
        public decimal SumFOT
        {
            get { return fSumFOT; }
            set { SetPropertyValue<decimal>("SumFOT", ref fSumFOT, value); }
        }

        private Ktu_Form fForm;
        [DisplayName("Форма КТУ")]       
        [Association(@"Form-KTUs")]
        [VisibleInDetailView(false), VisibleInListView(false), VisibleInLookupListView(false)]   
        public Ktu_Form Form
        {
            get { return fForm; }
            set { SetPropertyValue<Ktu_Form>("Form", ref fForm, value); }
        }

        private FakeTotal fFakeTotal;
        [DisplayName("Свод")]
        [Association(@"FakeTotal-Ktu")]
        [VisibleInDetailView(false), VisibleInListView(false), VisibleInLookupListView(false)]
        public FakeTotal FakeTotal
        {
            get { return fFakeTotal; }
            set { SetPropertyValue<FakeTotal>("FakeTotal", ref fFakeTotal, value); }
        }

       
        [DisplayName("Кол-во КТУ сотрудника")]       
        [NonPersistent, VisibleInDetailView(false), VisibleInListView(false), VisibleInLookupListView(false)]
        public int CountKtu
        {
            get 
            {
                if (Form != null && Person != null && Form.KTUs != null)
                    return Form.KTUs.Where(n => n.Person == this.Person && n.DeptPost == this.DeptPost).Count();
                else
                    return 0;                                 
            }            
        }

        
        [DisplayName("Сумма КТУ сотрудника")]
        [ModelDefault("DisplayFormat", "{0:N2}")]
        [ModelDefault("EditMask", "N2")]
        [NonPersistent, VisibleInDetailView(false), VisibleInListView(false), VisibleInLookupListView(false)]
        public decimal SumKtu
        {
            get 
            {
                if (Form != null && Person != null && Form.KTUs != null)
                    return Form.KTUs.Where(n => n.Person == this.Person && n.DeptPost == this.DeptPost).Sum(n => n.KTU);
                else
                    return 0m;      
            }            
        }


        public Ktu(Session session) : base(session) { }
        public Ktu() : base(Session.DefaultSession) { }
        public override void AfterConstruction()
        {
            base.AfterConstruction();
            this.Oid = Guid.NewGuid();
        }
    }//Ktu
}
