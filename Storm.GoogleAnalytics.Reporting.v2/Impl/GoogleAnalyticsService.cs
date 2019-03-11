using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Google.Apis.Analytics.v3;
using Google.Apis.Analytics.v3.Data;
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
            return new GoogleAnalyticsService(x => x
                .WithServiceAccountId(serviceAccountId)
                .WithServiceAccountCertificate(certificate));
        }

        public static IGoogleAnalyticsService Create(string serviceAccountId, string certificateAbsolutePath, string certificatePassword = "notasecret")
        {
            return new GoogleAnalyticsService(x => x
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

                    var data = await request.ExecuteAsync();

                    var dataTable = ToDataTable(data, data.ProfileInfo.ProfileName);

                    while (data.NextLink != null && data.Rows != null)
                    {
                        if (requestConfig.MaxResults < 10000 && data.Rows.Count <= requestConfig.MaxResults)
                        {
                            break;
                        }

                        request.StartIndex = (request.StartIndex ?? 1) + data.Rows.Count;
                        data = await request.ExecuteAsync();
                        dataTable.Merge(ToDataTable(data));
                    }

                    return new GoogleAnalyticsResponse(requestConfig, true, new GoogleAnalyticsDataResponse(dataTable, data.ContainsSampledData.GetValueOrDefault(false)));
                }
                catch (Exception ex)
                {
                    return new GoogleAnalyticsResponse(requestConfig, false, errorResponse: new GoogleAnalyticsErrorResponse(ex.Message, ex));
                }
            }
        }

        private AnalyticsService AnalyticsService =>
            new AnalyticsService(new BaseClientService.Initializer
            {
                ApplicationName = string.Concat(_serviceConfiguration.ApplicationName, _serviceConfiguration.GZipEnabled ? " (gzip)" : ""),
                GZipEnabled = _serviceConfiguration.GZipEnabled,
                DefaultExponentialBackOffPolicy = ExponentialBackOffPolicy.Exception,
                HttpClientInitializer = new ServiceAccountCredential(new ServiceAccountCredential.Initializer(_serviceConfiguration.ServiceAccountId)
                {
                    Scopes = new[] { _serviceConfiguration.Scope }
                }.FromCertificate(_serviceConfiguration.ServiceAccountCertificate))
            });

        private DataResource.GaResource.GetRequest AnalyticsRequest(AnalyticsService service, IGoogleAnalyticsRequestConfiguration requestConfig)
        {
            var metrics = string.Join(",", requestConfig.Metrics.Select(GaMetadata.WithPrefix));
            var dimensions = string.Join(",", requestConfig.Dimensions.Select(GaMetadata.WithPrefix));

            var request = service.Data.Ga.Get(
                GaMetadata.WithPrefix(requestConfig.ProfileId),
                requestConfig.StartDate.ToString("yyyy-MM-dd"),
                requestConfig.EndDate.ToString("yyyy-MM-dd"),
                metrics);
            request.Dimensions = dimensions;
            request.MaxResults = requestConfig.MaxResults;
            request.Filters = requestConfig.Filter;
            request.Sort = requestConfig.Sort;
            request.Segment = requestConfig.Segment;

            return request;
        }

        private static DataTable ToDataTable(GaData response, string name = "GA")
        {
            var requestResultTable = new DataTable(name);
            if (response != null)
            {
                requestResultTable.Columns.AddRange(response.ColumnHeaders.Select(x => new DataColumn(x.Name, GetDataType(x))).ToArray());

                if (response.Rows != null)
                {
                    foreach (var row in response.Rows)
                    {
                        var dataTableRow = requestResultTable.NewRow();

                        for (var index = 0; index != requestResultTable.Columns.Count; index++)
                        {
                            var column = requestResultTable.Columns[index];
                            if (column.DataType == typeof(DateTime))
                            {
                                dataTableRow.SetField(column, DateTime.ParseExact(row[index], "yyyyMMdd", CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                dataTableRow.SetField(column, row[index]);
                            }
                        }

                        requestResultTable.Rows.Add(dataTableRow);
                    }
                }

                requestResultTable.AcceptChanges();
            }

            return requestResultTable;
        }

        private static Type GetDataType(GaData.ColumnHeadersData gaColumn)
        {
            switch (gaColumn.DataType.ToLowerInvariant())
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