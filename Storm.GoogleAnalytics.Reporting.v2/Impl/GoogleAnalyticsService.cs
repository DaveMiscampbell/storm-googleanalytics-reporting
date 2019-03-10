using System;
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
        }
    }
}