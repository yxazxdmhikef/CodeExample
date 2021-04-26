using DAL.EF;
using ReportStorage.Service;
using RestService.Helpers;
using System;
using System.IO;
using System.Linq;
using Telerik.Reporting;
using Telerik.Reporting.Services.WebApi;

namespace RestService
{
    /// <summary>
    /// Кастомизированный распознаватель отчетов, хранящихся в БД.
    /// </summary>
    public class CustomReportDbResolver : ReportResolverBase
    {       
        private readonly IReportFileDBService _reportFileDBService;        

        public CustomReportDbResolver(string path)
        {           
            _reportFileDBService = SimpleInjectorResolver.Container.GetInstance<IReportFileDBService>();
        }

        protected override ReportSource ResolveReport(string reportName)
        {            
            var guid = new Guid(reportName.Split('.')[0]);

            using (var context = new ReportDbContext())
            {
                var reportFile = _reportFileDBService.GetFileAsNoTracking(context, guid);
                var reportPackager = new ReportPackager();

                using (var sourceStream = new MemoryStream(reportFile.Content))
                {
                    return GetReportSource(context, reportFile, null);
                }
            }
        }

        /// <summary>
        /// Устанавливает истоники вложенных отчетов для книги.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="parentReportOid"></param>
        /// <param name="book"></param>
        void SetReportBookSorces(ReportDbContext context, Guid parentReportOid, ref ReportBook book)
        {
            var oldSources = book.ReportSources.ToArray();
            book.ReportSources.Clear();
            int count = oldSources.Count();
            for (int i = 0; i < count; i++)
            {
                book.ReportSources.Add(GetSubReportSorce(context, oldSources[i], parentReportOid));
            }
        }

        /// <summary>
        /// Возвращает источник данных для вложенного отчета.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="subReportSource"></param>
        /// <param name="parentReportOid"></param>
        /// <returns></returns>
        ReportSource GetSubReportSorce(ReportDbContext context, ReportSource subReportSource, Guid parentReportOid)
        {
            var subReportName = String.Empty;
            var subTypes = new System.Collections.Generic.List<string>() {"trdp","trdx","trbp" };
            ParameterCollection parameters = subReportSource.Parameters;

            if (subReportSource is UriReportSource)
                subReportName = ((UriReportSource)subReportSource).Uri;
            else
                subReportName = subReportSource.ToString();

            subReportName = subReportName.Split('/').Last();
            
            if (!subTypes.Contains(subReportName.Split('.').Last()?.ToLower()))
            {
                return subReportSource;
            }
                
            var subReport = _reportFileDBService.GetSubReportFile(context, parentReportOid, subReportName);
           
            return GetReportSource(context, subReport, parentReportOid, parameters);
        }

        /// <summary>
        /// Возвращает источник для отчета.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="reportFile"></param>
        /// <param name="parentOid"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        ReportSource GetReportSource(
            ReportDbContext context
            , DAL.Entities.ReportFileDB reportFile
            , Guid? parentOid
            , ParameterCollection parameters = null)
        {
            ReportSource reportSource = null;
            var reportPackager = new ReportPackager();

            using (var sourceStream = new MemoryStream(reportFile.Content))
            {

                switch (reportFile.Extension?.Split('.')?.Last()?.ToUpper())
                {
                    case "TRDX":
                        reportSource = new XmlReportSource()
                        {
                            Xml = System.Text.Encoding.UTF8.GetString(reportFile.Content)
                        };
                        break;
                    case "TRBP": //книга
                        var reportBook = (ReportBook)reportPackager.UnpackageDocument(sourceStream);
                        var pOid = parentOid.HasValue ? parentOid.Value : reportFile.GuidId;
                        SetReportBookSorces(context, pOid, ref reportBook);
                        reportSource = new InstanceReportSource()
                        {
                            ReportDocument = reportBook
                        };
                        break;
                    case "TRDP":
                    default:
                        var report = (Report)reportPackager.UnpackageDocument(sourceStream);
                        
                        //установка источника для вложенных секций отчета
                        var prOid = parentOid.HasValue ? parentOid.Value : reportFile.GuidId;
                        report.Items.Flatten(f => f.Items)
                            .Where(i => (i.GetType().GetProperty(nameof(ReportSource)) != null)
                                   || (i.Action != null && i.Action.GetType().GetProperty(nameof(ReportSource)) != null))
                            .ToList()
                            .ForEach(f => 
                            {
                                var prop = f.GetType().GetProperty(nameof(ReportSource));
                                if (prop != null)
                                    prop.SetValue(f, GetSubReportSorce(context, (ReportSource)prop.GetValue(f), prOid));

                                if (f.Action != null)
                                {
                                    var aprop = f.Action.GetType().GetProperty(nameof(ReportSource));
                                    if (aprop != null)
                                        aprop.SetValue(f.Action, GetSubReportSorce(context, (ReportSource)aprop.GetValue(f.Action), prOid));

                                }

                            });

                        reportSource = new InstanceReportSource()
                        {
                            ReportDocument = report
                        };

                        //назначение параметров
                        if (parameters != null && parameters.Count() > 0)
                        {
                            reportSource.Parameters.Clear();
                            foreach (var par in parameters)
                            {
                                reportSource.Parameters.Add(par.Name, par.Value);
                            }
                        }

                        
                        break;
                }
            }

            return reportSource;
        }
    }
}