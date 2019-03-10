namespace Storm.GoogleAnalytics.Reporting.v2.Configuration
{
    public interface IGoogleAnalyticsRequestCustomConfigurer
    {
        IGoogleAnalyticsRequestCustomConfigurer Segment(string value);
        IGoogleAnalyticsRequestCustomConfigurer Filter(string value);
        IGoogleAnalyticsRequestCustomConfigurer Sort(string value);
        IGoogleAnalyticsRequestCustomConfigurer MaxResults(int value = 1000);
    }
}