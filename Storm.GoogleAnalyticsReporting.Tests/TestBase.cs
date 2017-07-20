using Moq;
using NUnit.Framework;

namespace Storm.GoogleAnalyticsReporting.Tests
{
    [TestFixture]
    public class TestBase
    {
        [SetUp]
        public virtual void SetUp() { }

        [TearDown]
        public virtual void TearDown() { }
    }
}