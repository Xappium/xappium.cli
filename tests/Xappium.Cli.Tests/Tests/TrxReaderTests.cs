using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Moq;
using Xappium.Tools;
using Xunit;

namespace Xappium.Cli.Tests
{
    public class TrxReaderTests
    {
        private static readonly FileInfo TrxFile = new FileInfo(Path.Combine("Resources", "SampleTrx.xml"));

        public TrxReaderTests()
        {
            TestEnvironmentHost.Init();
        }

        [Fact]
        public void DoesNotThrowException()
        {
            var trx = new TrxReader(Mock.Of<ILogger<TrxReader>>());
            var ex = Record.Exception(() => trx.Load(TrxFile));
            Assert.Null(ex);
        }

        [Fact]
        public void ContainsTwoTestDefinitions()
        {
            var reader = new TrxReader(Mock.Of<ILogger<TrxReader>>());
            var trx = reader.Load(TrxFile);
            Assert.Equal(2, trx.TestDefinitions.UnitTest.Count);
        }

        [Fact]
        public void TwoTestsPassed()
        {
            var reader = new TrxReader(Mock.Of<ILogger<TrxReader>>());
            var trx = reader.Load(TrxFile);
            Assert.Equal(2, trx.ResultSummary.Counters.Passed);
        }
    }
}
