using AutoMapper.Data;
using AutoMapper.Mappers;

namespace Storm.GoogleAnalytics.Reporting
{
    public class StormGoogleAnalyticsReporting
    {
        public static void Init()
        {
            MapperRegistry.Mappers.Insert(0, new DataReaderMapper()); 
        }
    }
}