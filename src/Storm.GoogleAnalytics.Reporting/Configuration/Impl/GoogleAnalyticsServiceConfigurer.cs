﻿using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Google.Apis.AnalyticsReporting.v4;

namespace Storm.GoogleAnalytics.Reporting.v2.Configuration.Impl
{
    public class GoogleAnalyticsServiceConfigurer : IGoogleAnalyticsServiceConfigurer, IGoogleAnalyticsServiceConfiguration
    {
        public GoogleAnalyticsServiceConfigurer()
        {
            Scope = AnalyticsReportingService.Scope.AnalyticsReadonly;
            ApplicationName = string.Empty;
            GZipEnabled = true;
        }

        public IGoogleAnalyticsServiceConfiguration Build()
        {
            var hasError = Validate();
            if (hasError != string.Empty)
            {
                throw new ApplicationException($"Invalid Google Analytics Service Configuration - {hasError}");
            }
            return this;
        }

        public IGoogleAnalyticsServiceConfigurer WithApplicationName(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                ApplicationName = value;
            }
            return this;
        }

        public IGoogleAnalyticsServiceConfigurer WithServiceAccount(string serviceAccount, X509Certificate2 certificate)
        {
            return WithServiceAccountId(serviceAccount).WithServiceAccountCertificate(certificate);
        }

        public IGoogleAnalyticsServiceConfigurer WithServiceAccountId(string value)
        {
            ServiceAccountId = value;
            return this;
        }

        public IGoogleAnalyticsServiceConfigurer WithServiceAccountCertificate(X509Certificate2 certificate)
        {
            if (certificate != null && certificate.HasPrivateKey)
            {
                ServiceAccountCertificate = certificate;
            }
            else
            {
                throw new ApplicationException("Unable to verify certificate has a private key");
            }
            return this;
        }

        public IGoogleAnalyticsServiceConfigurer WithServiceAccountCertificate(string keyFile, string password)
        {
            if (!string.IsNullOrWhiteSpace(keyFile) && File.Exists(keyFile))
            {
                WithServiceAccountCertificate(new X509Certificate2(keyFile, password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet));
            }
            else
            {
                throw new ApplicationException($"Unable to locate service account certificate : [{keyFile}]");
            }

            return this;
        }

        public IGoogleAnalyticsServiceConfigurer WithServiceAccountCertificate(byte[] keyFile, string password)
        {
            if (keyFile != null && keyFile.Length > 0)
            {
                WithServiceAccountCertificate(new X509Certificate2(keyFile, password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet));
            }

            return this;
        }

        public IGoogleAnalyticsServiceConfigurer WithScope(string value)
        {
            Scope = string.IsNullOrWhiteSpace(value) ? AnalyticsReportingService.Scope.AnalyticsReadonly : value;
            return this;
        }

        public IGoogleAnalyticsServiceConfigurer WithGZipEnabled(bool value = true)
        {
            GZipEnabled = value;
            return this;
        }

        public string ServiceAccountId { get; private set; }
        public X509Certificate2 ServiceAccountCertificate { get; private set; }
        public string Scope { get; private set; }
        public bool GZipEnabled { get; private set; }
        public string ApplicationName { get; private set; }

        private bool IsOneOf(string input, params string[] values)
        {
            return values.Any(x => x.Equals(input));
        }

        private string Validate()
        {
            if (string.IsNullOrWhiteSpace(Scope))
            {
                Scope = AnalyticsReportingService.Scope.AnalyticsReadonly;
            }

            if (!IsOneOf(Scope, AnalyticsReportingService.Scope.Analytics, AnalyticsReportingService.Scope.AnalyticsReadonly))
            {
                return $"Invalid analytics scope : [{Scope}]";
            }

            if (string.IsNullOrWhiteSpace(ServiceAccountId))
            {
                return "No service account id specified, use .WithServiceAccount()";
            }

            if (ServiceAccountCertificate == null || !ServiceAccountCertificate.HasPrivateKey)
            {
                return "Service account certificate is null or does not contain private key";
            }

            return string.Empty;
        }
    }
}