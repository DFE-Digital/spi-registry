using Dfe.Spi.Common.UnitTesting;
using Dfe.Spi.Registry.Domain;
using Newtonsoft.Json.Linq;

namespace Dfe.Spi.Registry.IntegrationTests
{
    public class TestScenario
    {
        public TestScenarioEvent[] Events { get; set; }
        public TestScenarioOutcome[] ExpectedEndState { get; set; }
    }

    public class TestScenarioEvent
    {
        public string Name { get; set; }
        public string EntityType { get; set; }
        public string SourceSystemName { get; set; }
        public JObject Payload { get; set; }
    }

    public class TestScenarioOutcome
    {
        public string Name { get; set; }
        public RegisteredEntity Entity { get; set; }
    }
}