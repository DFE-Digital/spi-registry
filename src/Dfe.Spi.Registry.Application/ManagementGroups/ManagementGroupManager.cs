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

namespace Dfe.Spi.Registry.Application.ManagementGroups
{
    public interface IManagementGroupManager
    {
        Task SyncManagementGroupAsync(string source, ManagementGroup managementGroup,
            CancellationToken cancellationToken);
    }
    
    public class ManagementGroupManager : IManagementGroupManager
    {
        private readonly IEntityManager _entityManager;
        private readonly ILoggerWrapper _logger;

        public ManagementGroupManager(
            IEntityManager entityManager,
            ILoggerWrapper logger)
        {
            _entityManager = entityManager;
            _logger = logger;
        }

        public async Task SyncManagementGroupAsync(string source, ManagementGroup managementGroup,
            CancellationToken cancellationToken)
        {
            var entity = new Entity
            {
                Type = TypeNames.ManagementGroup,
                SourceSystemName = source,
                SourceSystemId = managementGroup.Code,
                Data = new Dictionary<string, string>
                {
                    { "code", managementGroup.Code },
                },
            };
            
            _logger.Info(
                $"Mapped management group with code {managementGroup.Code} to: {JsonConvert.SerializeObject(entity)}");

            await _entityManager.SyncEntityAsync(entity, cancellationToken);
        }
    }
}