// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0


using System;
using System.Collections.Generic;
using System.Linq;
using MultiFunctionApplicationHelpers;
using NewRelic.Agent.IntegrationTestHelpers;
using NewRelic.Agent.IntegrationTests.Shared;
using NewRelic.Testing.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace NewRelic.Agent.UnboundedIntegrationTests.Postgres
{
    public abstract class PostgresSqlStoredProcedureTestsBase<TFixture> : NewRelicIntegrationTest<TFixture> where TFixture : ConsoleDynamicMethodFixture
    {
        private readonly ConsoleDynamicMethodFixture _fixture;
        private readonly string _procedureName = $"PostgresTestStoredProc{Guid.NewGuid():N}";

        public PostgresSqlStoredProcedureTestsBase(TFixture fixture, ITestOutputHelper output) : base(fixture)
        {
            _fixture = fixture;
            _fixture.TestLogger = output;

            _fixture.AddCommand($"PostgresSqlExerciser ParameterizedStoredProcedure {_procedureName}");

            _fixture.Actions
            (
                setupConfiguration: () =>
                {
                    var configPath = fixture.DestinationNewRelicConfigFilePath;
                    var configModifier = new NewRelicConfigModifier(configPath);

                    configModifier.ForceTransactionTraces()
                    .SetLogLevel("finest");

                    CommonUtils.ModifyOrCreateXmlAttributeInNewRelicConfig(configPath, new[] { "configuration", "transactionTracer" }, "explainEnabled", "true");
                    CommonUtils.ModifyOrCreateXmlAttributeInNewRelicConfig(configPath, new[] { "configuration", "transactionTracer" }, "recordSql", "raw");
                    CommonUtils.ModifyOrCreateXmlAttributeInNewRelicConfig(configPath, new[] { "configuration", "transactionTracer" }, "explainThreshold", "1");
                    CommonUtils.ModifyOrCreateXmlAttributeInNewRelicConfig(configPath, new[] { "configuration", "datastoreTracer", "queryParameters" }, "enabled", "true");
                }
            );

            // Confirm transaction transform has completed before moving on to host application shutdown, and final sendDataOnExit harvest
            _fixture.AddActions(exerciseApplication: () => _fixture.AgentLog.WaitForLogLine(AgentLogBase.TransactionTransformCompletedLogLineRegex, TimeSpan.FromMinutes(2)));

            _fixture.Initialize();
        }

        [Fact]
        public void Test()
        {
            var expectedTransactionName = "OtherTransaction/Custom/MultiFunctionApplicationHelpers.NetStandardLibraries.PostgresSql.PostgresSqlExerciser/ParameterizedStoredProcedure";

            var expectedMetrics = new List<Assertions.ExpectedMetric>
            {
                new Assertions.ExpectedMetric { metricName = $@"Datastore/statement/Postgres/{_procedureName.ToLower()}/ExecuteProcedure", callCount = 1 },
                new Assertions.ExpectedMetric { metricName = $@"Datastore/statement/Postgres/{_procedureName.ToLower()}/ExecuteProcedure", callCount = 1, metricScope = expectedTransactionName}
            };

            var expectedTransactionTraceSegments = new List<string>
            {
                $"Datastore/statement/Postgres/{_procedureName.ToLower()}/ExecuteProcedure"
            };

            var expectedQueryParameters = DbParameterData.PostgresParameters.ToDictionary(p => p.ParameterName, p => p.ExpectedValue);

            var expectedTransactionTraceQueryParameters = new Assertions.ExpectedSegmentQueryParameters { segmentName = $"Datastore/statement/Postgres/{_procedureName.ToLower()}/ExecuteProcedure", QueryParameters = expectedQueryParameters };

            var expectedSqlTraces = new List<Assertions.ExpectedSqlTrace>
            {
                new Assertions.ExpectedSqlTrace
                {
                    TransactionName = expectedTransactionName,
                    Sql = _procedureName,
                    DatastoreMetricName = $"Datastore/statement/Postgres/{_procedureName.ToLower()}/ExecuteProcedure",
                    QueryParameters = expectedQueryParameters,
                    HasExplainPlan = false
                }
            };

            var metrics = _fixture.AgentLog.GetMetrics().ToList();
            var transactionSample = _fixture.AgentLog.TryGetTransactionSample(expectedTransactionName);
            var transactionEvent = _fixture.AgentLog.TryGetTransactionEvent(expectedTransactionName);
            var sqlTraces = _fixture.AgentLog.GetSqlTraces().ToList();

            NrAssert.Multiple(
                () => Assert.NotNull(transactionSample),
                () => Assert.NotNull(transactionEvent)
            );

            NrAssert.Multiple
            (
                () => Assertions.MetricsExist(expectedMetrics, metrics),
                () => Assertions.TransactionTraceSegmentsExist(expectedTransactionTraceSegments, transactionSample),
                () => Assertions.TransactionTraceSegmentQueryParametersExist(expectedTransactionTraceQueryParameters, transactionSample),
                () => Assertions.SqlTraceExists(expectedSqlTraces, sqlTraces)
            );
        }
    }

    [NetFrameworkTest]
    public class PostgresSqlStoredProcedureTestsFW462 : PostgresSqlStoredProcedureTestsBase<ConsoleDynamicMethodFixtureFW462>
    {
        public PostgresSqlStoredProcedureTestsFW462(ConsoleDynamicMethodFixtureFW462 fixture, ITestOutputHelper output) : base(fixture, output)
        {

        }
    }

    [NetFrameworkTest]
    public class PostgresSqlStoredProcedureTestsFW471 : PostgresSqlStoredProcedureTestsBase<ConsoleDynamicMethodFixtureFW471>
    {
        public PostgresSqlStoredProcedureTestsFW471(ConsoleDynamicMethodFixtureFW471 fixture, ITestOutputHelper output) : base(fixture, output)
        {

        }
    }

    [NetFrameworkTest]
    public class PostgresSqlStoredProcedureTestsFW48 : PostgresSqlStoredProcedureTestsBase<ConsoleDynamicMethodFixtureFW48>
    {
        public PostgresSqlStoredProcedureTestsFW48(ConsoleDynamicMethodFixtureFW48 fixture, ITestOutputHelper output) : base(fixture, output)
        {

        }
    }

    [NetFrameworkTest]
    public class PostgresSqlStoredProcedureTestsFWLatest : PostgresSqlStoredProcedureTestsBase<ConsoleDynamicMethodFixtureFWLatest>
    {
        public PostgresSqlStoredProcedureTestsFWLatest(ConsoleDynamicMethodFixtureFWLatest fixture, ITestOutputHelper output) : base(fixture, output)
        {

        }
    }

    [NetCoreTest]
    public class PostgresSqlStoredProcedureTestsCore31 : PostgresSqlStoredProcedureTestsBase<ConsoleDynamicMethodFixtureCore31>
    {
        public PostgresSqlStoredProcedureTestsCore31(ConsoleDynamicMethodFixtureCore31 fixture, ITestOutputHelper output) : base(fixture, output)
        {

        }
    }

    [NetCoreTest]
    public class PostgresSqlStoredProcedureTestsCore50 : PostgresSqlStoredProcedureTestsBase<ConsoleDynamicMethodFixtureCore50>
    {
        public PostgresSqlStoredProcedureTestsCore50(ConsoleDynamicMethodFixtureCore50 fixture, ITestOutputHelper output) : base(fixture, output)
        {

        }
    }

    [NetCoreTest]
    public class PostgresSqlStoredProcedureTestsCore60 : PostgresSqlStoredProcedureTestsBase<ConsoleDynamicMethodFixtureCore60>
    {
        public PostgresSqlStoredProcedureTestsCore60(ConsoleDynamicMethodFixtureCore60 fixture, ITestOutputHelper output) : base(fixture, output)
        {

        }
    }

    [NetCoreTest]
    public class PostgresSqlStoredProcedureTestsCoreLatest : PostgresSqlStoredProcedureTestsBase<ConsoleDynamicMethodFixtureCoreLatest>
    {
        public PostgresSqlStoredProcedureTestsCoreLatest(ConsoleDynamicMethodFixtureCoreLatest fixture, ITestOutputHelper output) : base(fixture, output)
        {

        }
    }
}
