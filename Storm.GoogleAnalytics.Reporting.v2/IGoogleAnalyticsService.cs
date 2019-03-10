using System;
using Storm.GoogleAnalytics.Reporting.v2.Configuration;
using Storm.GoogleAnalytics.Reporting.v2.Core;

namespace Storm.GoogleAnalytics.Reporting.v2
{
    public interface IGoogleAnalyticsService
    {
        /// <summary>
        /// Query the Google Analytics Reporting API using the given request configuration
        /// </summary>
        /// <param name="configurer">configures the request with which to query the Google Analytics Reporting API</param>
        /// <returns>Google Analytics Response</returns>
        IGoogleAnalyticsResponse Query(Func<IGoogleAnalyticsRequestConfigurerProfileId, IGoogleAnalyticsRequestConfigurerProfileId> configurer);
    }
}