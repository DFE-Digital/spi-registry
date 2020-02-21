using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.Models.Entities;
using Dfe.Spi.Registry.Application.Entities;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Entities;
using Newtonsoft.Json;

namespace Dfe.Spi.Registry.Application.LearningProviders
{
    public interface ILearningProviderManager
    {
        Task SyncLearningProviderAsync(string source, LearningProvider learningProvider,
            CancellationToken cancellationToken);
    }

    public class LearningProviderManager : ILearningProviderManager
    {
        private readonly IEntityManager _entityManager;
        private readonly ILoggerWrapper _logger;

        public LearningProviderManager(
            IEntityManager entityManager,
            ILoggerWrapper logger)
        {
            _entityManager = entityManager;
            _logger = logger;
        }

        public async Task SyncLearningProviderAsync(string source, LearningProvider learningProvider,
            CancellationToken cancellationToken)
        {
            var entity = new Entity
            {
                Type = TypeNames.LearningProvider,
                SourceSystemName = source,
                SourceSystemId = GetSourceSystemId(source, learningProvider),
                Data = new Dictionary<string, string>
                {
                    {"urn", learningProvider.Urn.ToString()},
                    {"ukprn", learningProvider.Ukprn.HasValue ? learningProvider.Ukprn.Value.ToString() : null},
                },
            };
            if (learningProvider.ManagementGroup != null)
            {
                entity.Data.Add("managementGroupCode", learningProvider.ManagementGroup.Code);
            }
            

            _logger.Info(
                $"Mapped learning provider with urn {learningProvider.Urn} to: {JsonConvert.SerializeObject(entity)}");

            await _entityManager.SyncEntityAsync(entity, cancellationToken);
        }


        private string GetSourceSystemId(string source, LearningProvider learningProvider)
        {
            if (source.Equals(SourceSystemNames.UkRegisterOfLearningProviders,
                StringComparison.InvariantCultureIgnoreCase))
            {
                return learningProvider.Ukprn.Value.ToString();
            }

            return learningProvider.Urn.ToString();
        }
    }
}