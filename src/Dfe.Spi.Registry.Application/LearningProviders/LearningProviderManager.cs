using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Extensions;
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
            var data = (new Dictionary<string, string>
                {
                    {DataAttributeNames.Name, learningProvider.Name},
                    {DataAttributeNames.Type, learningProvider.Type},
                    {DataAttributeNames.SubType, learningProvider.SubType},
                    {DataAttributeNames.OpenDate, learningProvider.OpenDate?.ToSpiString()},
                    {DataAttributeNames.CloseDate, learningProvider.CloseDate?.ToSpiString()},
                    {DataAttributeNames.Urn, learningProvider.Urn.ToString()},
                    {DataAttributeNames.Ukprn, learningProvider.Ukprn?.ToString()},
                    {DataAttributeNames.Uprn, learningProvider.Uprn},
                    {DataAttributeNames.CompaniesHouseNumber, learningProvider.CompaniesHouseNumber},
                    {DataAttributeNames.CharitiesCommissionNumber, learningProvider.CharitiesCommissionNumber},
                    {DataAttributeNames.AcademyTrustCode, learningProvider.AcademyTrustCode},
                    {DataAttributeNames.DfeNumber, learningProvider.DfeNumber},
                    {DataAttributeNames.LocalAuthorityCode, learningProvider.LocalAuthorityCode},
                    {DataAttributeNames.ManagementGroupType, learningProvider.ManagementGroup?.Type},
                    {DataAttributeNames.ManagementGroupId, learningProvider.ManagementGroup?.Code},
                }).Where(kvp => kvp.Value != null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            
            var entity = new Entity
            {
                Type = TypeNames.LearningProvider,
                SourceSystemName = source,
                SourceSystemId = GetSourceSystemId(source, learningProvider),
                Data = data,
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