using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Configuration;
using Dfe.Spi.Registry.Domain.Entities;
using Dfe.Spi.Registry.Domain.Links;
using Dfe.Spi.Registry.Infrastructure.AzureStorage.Entities;
using Dfe.Spi.Registry.Infrastructure.AzureStorage.Links;

namespace PopulateRegistryFromGiasEstablishments
{
    class Program
    {
        private static Logger _logger;
        private static IEntityRepository _entityRepository;
        private static ILinkRepository _linkRepository;

        static async Task Run(CommandLineOptions options, CancellationToken cancellationToken = default)
        {
            Init(options);

            _logger.Info("Reading establishments");
            var establishments = await GetEstablishments(options, cancellationToken);
            _logger.Info($"Read {establishments.Length} establishments");

            await ProcessEstablishments(establishments, cancellationToken);
        }

        static void Init(CommandLineOptions options)
        {
            _entityRepository = new TableEntityRepository(new EntitiesConfiguration
            {
                TableStorageConnectionString = options.StorageConnectionString,
                TableStorageTableName = options.EntitiesTableName,
            });

            _linkRepository = new TableLinkRepository(new LinksConfiguration
            {
                TableStorageConnectionString = options.StorageConnectionString,
                TableStorageTableName = options.LinksTableName,
            });
        }

        static async Task ProcessEstablishments(Establishment[] establishments, CancellationToken cancellationToken)
        {
            for (var i = 0; i < establishments.Length; i++)
            {
                var establishment = establishments[i];
                _logger.Info($"Processing establishment {i} of {establishments.Length}: {establishment.Urn}");

                var giasEntity = new Entity
                {
                    Type = TypeNames.LearningProvider,
                    SourceSystemName = SourceSystemNames.GetInformationAboutSchools,
                    SourceSystemId = establishment.Urn.ToString(),
                };
                var ukrlpEntity = establishment.Ukprn.HasValue
                    ? new Entity
                    {
                        Type = TypeNames.LearningProvider,
                        SourceSystemName = SourceSystemNames.UkRegisterOfLearningProviders,
                        SourceSystemId = establishment.Ukprn.Value.ToString(),
                    }
                    : null;
                Link link = null;

                if (ukrlpEntity != null)
                {
                    link = new Link
                    {
                        Type = "Synonym",
                        Id = Guid.NewGuid().ToString(),
                        LinkedEntities = new[]
                        {
                            GetEntityLinkFromEntity(giasEntity),
                            GetEntityLinkFromEntity(ukrlpEntity),
                        },
                    };

                    giasEntity.Links = new[]
                    {
                        new LinkPointer {LinkType = link.Type, LinkId = link.Id},
                    };
                    ukrlpEntity.Links = new[]
                    {
                        new LinkPointer {LinkType = link.Type, LinkId = link.Id},
                    };
                }

                _logger.Info($"Storing establishment {i} of {establishments.Length}: {establishment.Urn}");
                await StoreAsync(giasEntity, ukrlpEntity, link, cancellationToken);
            }
        }

        static async Task StoreAsync(Entity giasEntity, Entity ukrlpEntity, Link link,
            CancellationToken cancellationToken)
        {
            await _entityRepository.StoreAsync(giasEntity, cancellationToken);

            if (ukrlpEntity != null)
            {
                await _entityRepository.StoreAsync(ukrlpEntity, cancellationToken);
            }

            if (link != null)
            {
                await _linkRepository.StoreAsync(link, cancellationToken);
            }
        }

        static EntityLink GetEntityLinkFromEntity(Entity entity)
        {
            return new EntityLink
            {
                EntityType = entity.Type,
                EntitySourceSystemName = entity.SourceSystemName,
                EntitySourceSystemId = entity.SourceSystemId,
                CreatedAt = DateTime.Now,
                CreatedBy = "PopulateRegistryFromGiasEstablishments",
                CreatedReason = "BulkGiasLoad"
            };
        }

        static async Task<Establishment[]> GetEstablishments(CommandLineOptions options,
            CancellationToken cancellationToken)
        {
            _logger.Info($"Reading establishments from {options.EstablishmentsFilePath}");
            Establishment[] establishments;
            using (var stream = new FileStream(options.EstablishmentsFilePath, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(stream))
            using (var parser = new CsvFileParser<Establishment>(reader, new EstablishmentCsvMapping()))
            {
                establishments = parser.GetRecords();
            }

            return establishments;
        }

        static void Main(string[] args)
        {
            _logger = new Logger();

            CommandLineOptions options = null;
            Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed((parsed) => options = parsed);
            if (options != null)
            {
                try
                {
                    Run(options).Wait();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }

                _logger.Info("Done. Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}