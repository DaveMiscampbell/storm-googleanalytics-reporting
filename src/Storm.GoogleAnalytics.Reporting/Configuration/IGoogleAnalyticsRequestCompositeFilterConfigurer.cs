namespace Storm.GoogleAnalytics.Reporting.v2.Configuration
{
    public interface IGoogleAnalyticsRequestCompositeFilterConfigurer : IGoogleAnalyticsRequestConfigurer
    {
        IGoogleAnalyticsRequestCompositeFilterConfigurer OrFilterBy(string field, string @operator, string value);
        IGoogleAnalyticsRequestCompositeFilterConfigurer AndFilterBy(string field, string @operator, string value);
    }
}