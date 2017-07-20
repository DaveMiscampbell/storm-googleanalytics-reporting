using System;
using System.Data;
using System.Linq;
using NUnit.Framework;
using Storm.GoogleAnalytics.Reporting.Core.Impl;

namespace Storm.GoogleAnalyticsReporting.Tests
{
    public class GoogleAnalyticsDataResponseSpec : ContextSpecification
    {
        protected static readonly string[] People = { "Dave Miscampbell", "Phil Oyston" };

        protected GoogleAnalyticsDataResponse DataResponse;
        protected DataTable Data;

        protected override void SharedContext()
        {
            Data = BuildDataTable();
            DataResponse = new GoogleAnalyticsDataResponse(Data, false);
        }

        private static DataTable BuildDataTable()
        {
            var resultTable = new DataTable("test");
            resultTable.Columns.AddRange(new[] { new DataColumn("FirstName", typeof(string)), new DataColumn("LastName", typeof(string)) });
            foreach (var person in People)
            {
                var dtRow = resultTable.NewRow();
                var splitName = person.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < resultTable.Columns.Count; i++)
                {
                    var col = resultTable.Columns[i];
                    dtRow.SetField(col, splitName[i]);
                }
                resultTable.Rows.Add(dtRow);
            }
            resultTable.AcceptChanges();
            return resultTable;
        }
    }

    public class TestDataTableRow
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    [TestFixture]
    public class when_accessing_data_as_data_table : GoogleAnalyticsDataResponseSpec
    {
        [Test]
        public void should_set_data()
        {
            Assert.IsNotNull(DataResponse.AsDataTable());
        }

        [Test]
        public void should_set_correct_data()
        {
            Assert.AreEqual(DataResponse.AsDataTable(), Data);
        }
    }

    [TestFixture]
    public class when_accessing_data_as_json : GoogleAnalyticsDataResponseSpec
    {
        [Test]
        public void should_return_data()
        {
            Assert.IsNotNull(DataResponse.AsJson());
            Assert.IsNotEmpty(DataResponse.AsJson());
        }
    }

    [TestFixture]
    public class when_accessing_data_as_object :
        GoogleAnalyticsDataResponseSpec
    {
        [Test]
        public void should_return_data()
        {
            Assert.IsNotNull(DataResponse.ToObject<TestDataTableRow>());
            Assert.IsNotEmpty(DataResponse.ToObject<TestDataTableRow>());
        }

        [Test]
        public void should_return_correct_data()
        {
            var objects = DataResponse.ToObject<TestDataTableRow>().ToList();

            var firstPerson = objects.First();
            Assert.AreEqual(firstPerson.FirstName + ' ' + firstPerson.LastName, People[0]);

            var secondPerson = objects.Last();
            Assert.AreEqual(secondPerson.FirstName + ' ' + secondPerson.LastName, People[1]);
        }
    }
}