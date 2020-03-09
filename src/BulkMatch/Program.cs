using System;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Dfe.Spi.Registry.Application.Matching;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Configuration;
using Dfe.Spi.Registry.Domain.Matching;
using Dfe.Spi.Registry.Infrastructure.AzureStorage.Entities;
using Dfe.Spi.Registry.Infrastructure.AzureStorage.Links;
using SeedFromLearningProviderFile;

namespace BulkMatch
{
    class Program
    {
        private static Logger _logger;
        private static CachedEntityRepository _entityRepository;
        private static IMatchManager _matchManager;

        static async Task Run(CommandLineOptions options, CancellationToken cancellationToken = default)
        {
            Init(options);

            _logger.Info("Loading learning providers...");
            var learningProviders =
                await _entityRepository.GetEntitiesOfTypeAsync(TypeNames.LearningProvider, cancellationToken);
            _logger.Info($"Loaded {learningProviders.Length} learning providers for matching");
            
            for (var i = 0; i < learningProviders.Length; i++)
            {
                var learningProvider = learningProviders[i];
                _logger.Info($"Matching {i} of {learningProviders.Length}: {learningProvider.SourceSystemName}.{learningProvider.SourceSystemId}");

                await _matchManager.UpdateLinksAsync(
                    new EntityForMatching
                    {
                        Type = learningProvider.Type,
                        SourceSystemName = learningProvider.SourceSystemName,
                        SourceSystemId = learningProvider.SourceSystemId,
                    },
                    cancellationToken);
            }
        }

        static void Init(CommandLineOptions options)
        {
            _entityRepository = new CachedEntityRepository(
                new TableEntityRepository(
                    new EntitiesConfiguration
                    {
                        TableStorageConnectionString = options.StorageConnectionString,
                        TableStorageTableName = options.EntitiesTableName,
                    }));

            var linkRepository = new TableLinkRepository(
                new LinksConfiguration
                {
                    TableStorageConnectionString = options.StorageConnectionString,
                    TableStorageTableName = options.LinksTableName,
                });

            var profileRepository = new WellDefinedMatchingProfileRepository();

            var matchProfileProcessor = new MatchProfileProcessor(
                _entityRepository,
                linkRepository,
                _logger);

            _matchManager = new MatchManager(
                _entityRepository,
                profileRepository,
                matchProfileProcessor,
                _logger);
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