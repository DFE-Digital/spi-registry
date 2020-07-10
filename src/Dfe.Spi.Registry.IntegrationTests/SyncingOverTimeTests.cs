using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.UnitTesting;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Sync;
using Dfe.Spi.Registry.Functions.Sync;
using Dfe.Spi.Registry.IntegrationTests.Stubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Dfe.Spi.Registry.IntegrationTests
{
    public class SyncingOverTimeTests
    {
        [Test]
        public async Task ASingleEventShouldSyncCorrectly()
        {
            var sampleJson = typeof(SyncingOverTimeTests).Assembly.GetSample("SingleEvent.json");
            var sample = JsonConvert.DeserializeObject<TestScenario>(sampleJson);

            await RunScenario(sample);
        }

        [Test]
        public async Task ASimpleOrderedSetOfEventsShouldSyncCorrectly()
        {
            var sampleJson = typeof(SyncingOverTimeTests).Assembly.GetSample("OrderedSeriesOfEvents.json");
            var sample = JsonConvert.DeserializeObject<TestScenario>(sampleJson);

            await RunScenario(sample);
        }


        private async Task RunScenario(TestScenario scenario)
        {
            var cancellationToken = new CancellationToken();
            var host = new TestFunctionHost();
            using (var scope = host.GetScope())
            {
                await ReceiveEventsAsync(scope, scenario, cancellationToken);

                await ProcessEventsAsync(scope, cancellationToken);

                CheckState(scope, scenario);
            }
        }

        private async Task ReceiveEventsAsync(TestScope scope, TestScenario scenario, CancellationToken cancellationToken)
        {
            var receiveFunction = scope.GetService<ReceiveEntityEvent>();
            for (var i = 0; i < scenario.Events.Length; i++)
            {
                var @event = scenario.Events[i];
                try
                {
                    var httpRequest = new DefaultHttpRequest(new DefaultHttpContext())
                    {
                        Body = new MemoryStream(Encoding.UTF8.GetBytes(@event.Payload.ToString())),
                    };
                    await receiveFunction.RunAsync(httpRequest, @event.EntityType, @event.SourceSystemName, cancellationToken);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error receiving event {@event.Name} (index {i}): {ex.Message}", ex);
                }
            }
        }

        private async Task ProcessEventsAsync(TestScope scope, CancellationToken cancellationToken)
        {
            var queue = scope.GetService<SyncQueueStub>();
            await queue.DrainQueueAsync(cancellationToken);
        }

        private void CheckState(TestScope scope, TestScenario scenario)
        {
            var repository = scope.GetService<RepositoryStub>();

            for (var i = 0; i < scenario.ExpectedEndState.Length; i++)
            {
                var outcome = scenario.ExpectedEndState[i];
                var matches = repository.Store.Where(storedEntity =>
                        storedEntity.Type == outcome.Entity.Type &&
                        storedEntity.ValidFrom == outcome.Entity.ValidFrom &&
                        storedEntity.ValidTo == outcome.Entity.ValidTo &&
                        AreEqual(outcome.Entity.Entities, storedEntity.Entities))
                    .ToArray();

                Assert.AreEqual(1, matches.Length, $"Expected outcome {outcome.Name} (index {i}) to match a single stored entity but matched {matches.Length}");
            }
        }

        private bool AreEqual(LinkedEntity[] expected, LinkedEntity[] actual)
        {
            if (expected.Length != actual.Length)
            {
                return false;
            }

            for (var i = 0; i < expected.Length; i++)
            {
                var expectedItem = expected[i];
                var actualItem = actual.SingleOrDefault(a => a.EntityType == expectedItem.EntityType &&
                                                             a.SourceSystemName == expectedItem.SourceSystemName &&
                                                             a.SourceSystemId == expectedItem.SourceSystemId);

                if (actualItem != null &&
                    expectedItem.LinkType != actualItem.LinkType ||
                    expectedItem.LinkedBy != actualItem.LinkedBy ||
                    expectedItem.Name != actualItem.Name ||
                    expectedItem.Type != actualItem.Type ||
                    expectedItem.SubType != actualItem.SubType ||
                    expectedItem.OpenDate != actualItem.OpenDate ||
                    expectedItem.CloseDate != actualItem.CloseDate ||
                    expectedItem.Urn != actualItem.Urn ||
                    expectedItem.Ukprn != actualItem.Ukprn ||
                    expectedItem.Uprn != actualItem.Uprn ||
                    expectedItem.CompaniesHouseNumber != actualItem.CompaniesHouseNumber ||
                    expectedItem.CharitiesCommissionNumber != actualItem.CharitiesCommissionNumber ||
                    expectedItem.AcademyTrustCode != actualItem.AcademyTrustCode ||
                    expectedItem.DfeNumber != actualItem.DfeNumber ||
                    expectedItem.LocalAuthorityCode != actualItem.LocalAuthorityCode ||
                    expectedItem.ManagementGroupType != actualItem.ManagementGroupType ||
                    expectedItem.ManagementGroupId != actualItem.ManagementGroupId ||
                    expectedItem.ManagementGroupCode != actualItem.ManagementGroupCode ||
                    expectedItem.ManagementGroupUkprn != actualItem.ManagementGroupUkprn ||
                    expectedItem.ManagementGroupCompaniesHouseNumber != actualItem.ManagementGroupCompaniesHouseNumber)
                {
                    return false;
                }
            }

            return true;
        }
    }
}