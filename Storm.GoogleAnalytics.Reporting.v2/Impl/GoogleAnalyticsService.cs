using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Google.Apis.AnalyticsReporting.v4;
using Google.Apis.AnalyticsReporting.v4.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Services;
using Storm.GoogleAnalytics.Reporting.v2.Configuration;
using Storm.GoogleAnalytics.Reporting.v2.Configuration.Impl;
using Storm.GoogleAnalytics.Reporting.v2.Core;
using Storm.GoogleAnalytics.Reporting.v2.Core.Impl;

namespace Storm.GoogleAnalytics.Reporting.v2.Impl
{
    public sealed class GoogleAnalyticsService : IGoogleAnalyticsService
    {
        public static IGoogleAnalyticsService Create(string serviceAccountId, X509Certificate2 certificate)
        {
            return new GoogleAnalyticsService(x=>x
                .WithServiceAccountId(serviceAccountId)
                .WithServiceAccountCertificate(certificate));
        }

        public static IGoogleAnalyticsService Create(string serviceAccountId, string certificateAbsolutePath, string certificatePassword = "notasecret")
        {
            return new GoogleAnalyticsService(x=>x
                .WithServiceAccountId(serviceAccountId)
                .WithServiceAccountCertificate(certificateAbsolutePath, certificatePassword));
        }

        private readonly IGoogleAnalyticsServiceConfiguration _serviceConfiguration;

        public GoogleAnalyticsService(Func<IGoogleAnalyticsServiceConfigurer, IGoogleAnalyticsServiceConfigurer> configurer)
        {
            var config = new GoogleAnalyticsServiceConfigurer();
            configurer(config);
            _serviceConfiguration = config.Build();
        }

        public IGoogleAnalyticsResponse Query(Func<IGoogleAnalyticsRequestConfigurerProfileId, IGoogleAnalyticsRequestConfigurerProfileId> configurer)
        {
            var config = new GoogleAnalyticsRequestConfigurer();
            configurer(config);
            return Query(config.Build());
        }

        public IGoogleAnalyticsResponse Query(IGoogleAnalyticsRequestConfiguration requestConfig)
        {
            return Task.Run(() => QueryAsync(requestConfig)).Result;
        }

        public async Task<IGoogleAnalyticsResponse> QueryAsync(Func<IGoogleAnalyticsRequestConfigurerProfileId, IGoogleAnalyticsRequestConfigurerProfileId> configurer)
        {
            var config = new GoogleAnalyticsRequestConfigurer();
            configurer(config);
            return await QueryAsync(config.Build());
        }

        public async Task<IGoogleAnalyticsResponse> QueryAsync(IGoogleAnalyticsRequestConfiguration requestConfig)
        {
            using (var service = AnalyticsService)
            {
                try
                {
                    var request = AnalyticsRequest(service, requestConfig);

                    var data = await service.Reports.BatchGet(request).ExecuteAsync();
                    var dataTable = ToDataTable(data);
                    

                    // Paging

                    return new GoogleAnalyticsResponse(requestConfig, true, new GoogleAnalyticsDataResponse(dataTable, data.Reports.First().Data.SamplesReadCounts.Any() && data.Reports.First().Data.SamplingSpaceSizes.Any()));
                }
                catch (Exception ex)
                {
                    return new GoogleAnalyticsResponse(requestConfig, false, errorResponse: new GoogleAnalyticsErrorResponse(ex.Message, ex));
                }
            }
        }

        private AnalyticsReportingService AnalyticsService =>
            new AnalyticsReportingService(new BaseClientService.Initializer
            {
                ApplicationName = string.Concat(_serviceConfiguration.ApplicationName, _serviceConfiguration.GZipEnabled ? " (gzip)" : ""),
                GZipEnabled = _serviceConfiguration.GZipEnabled,
                DefaultExponentialBackOffPolicy = ExponentialBackOffPolicy.Exception,
                HttpClientInitializer = new ServiceAccountCredential(new ServiceAccountCredential.Initializer(_serviceConfiguration.ServiceAccountId)
                {
                    Scopes = new [] { _serviceConfiguration.Scope }
                }.FromCertificate(_serviceConfiguration.ServiceAccountCertificate))
            });

        private GetReportsRequest AnalyticsRequest(AnalyticsReportingService service, IGoogleAnalyticsRequestConfiguration requestConfig)
        {
            var metrics = string.Join(",", requestConfig.Metrics.Select(GaMetadata.WithPrefix));
            var dimensions = string.Join(",", requestConfig.Dimensions.Select(GaMetadata.WithPrefix));

            var request = new ReportRequest
            {
                ViewId = requestConfig.ProfileId,
                DateRanges = new List<DateRange>
                {
                    new DateRange {StartDate = requestConfig.StartDate.ToString("yyyy-MM-dd"), EndDate = requestConfig.EndDate.ToString("yyyy-MM-dd")}
                },
                Metrics = requestConfig.Metrics.Select(x => new Metric {Expression = GaMetadata.WithPrefix(x)}).ToList(),
                Dimensions = requestConfig.Dimensions.Select(x => new Dimension {Name = GaMetadata.WithPrefix(x)}).ToList(),
                PageSize = requestConfig.MaxResults
                // Filtering
                // Sorting
                // Segmenting
            };

            return new GetReportsRequest {ReportRequests = new List<ReportRequest> {request}};
        }

        private static DataTable ToDataTable(GetReportsResponse response, string name = "GA")
        {
            var requestResultTable = new DataTable(name);
            var report = response?.Reports.FirstOrDefault();
            if (report != null)
            {
                requestResultTable.Columns.AddRange(report.ColumnHeader.MetricHeader.MetricHeaderEntries.Select(x=> new DataColumn(x.Name, GetDataType(x))).ToArray());

                if (report.Data?.Rows != null)
                {
                    foreach (var row in report.Data.Rows)
                    {

                        var dataTableRow = requestResultTable.NewRow();

                        for (var index = 0; index != requestResultTable.Columns.Count; index++)
                        {
                            var column = requestResultTable.Columns[index];
                            if (column.DataType == typeof(DateTime))
                            {
                                // Set Field
                            }
                            else
                            {
                                // Set Field
                            }
                        }

                        requestResultTable.Rows.Add(dataTableRow);
                    }
                }

                requestResultTable.AcceptChanges();
            }

            return requestResultTable;
        }

        private static Type GetDataType(MetricHeaderEntry gaColumn)
        {
            switch (gaColumn.Type.ToLowerInvariant())
            {
                case "integer":
                    return typeof(int);
                case "double":
                    return typeof(double);
                case "currency":
                    return typeof(decimal);
                case "time":
                    return typeof(float);
                default:
                    if (gaColumn.Name.ToLowerInvariant().Equals(GaMetadata.WithPrefix(GaMetadata.Dimensions.Time.Date)))
                    {
                        return typeof(DateTime);
                    }
                    return typeof(string);
            }
        }
    }
}