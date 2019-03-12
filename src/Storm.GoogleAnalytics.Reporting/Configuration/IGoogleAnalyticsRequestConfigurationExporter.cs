using System.IO;

namespace Storm.GoogleAnalytics.Reporting.v2.Configuration
{
    public interface IGoogleAnalyticsRequestConfigurationExporter
    {
        /// <summary>
        /// Allows your request to be exported as a json object
        /// </summary>
        /// <param name="writer"></param>
        void ExportTo(TextWriter writer);
    }
}