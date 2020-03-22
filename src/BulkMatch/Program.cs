using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Dfe.Spi.Registry.Application;
using Dfe.Spi.Registry.Application.Matching;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Configuration;
using Dfe.Spi.Registry.Domain.Entities;
using Dfe.Spi.Registry.Domain.Matching;
using Dfe.Spi.Registry.Domain.Search;
using Dfe.Spi.Registry.Infrastructure.AzureCognitiveSearch;
using Dfe.Spi.Registry.Infrastructure.AzureStorage.Entities;
using Dfe.Spi.Registry.Infrastructure.AzureStorage.Links;
using SeedFromLearningProviderFile;

namespace BulkMatch
{
    class Program
    {
        private static Logger _logger;
        private static CachedEntityRepository _entityRepository;
        private static ISearchIndex _searchIndex;
        private static IMatchManager _matchManager;

        static async Task Run(CommandLineOptions options, CancellationToken cancellationToken = default)
        {
            Init(options);

            _logger.Info("Loading learning providers...");
            var learningProviders =
                await _entityRepository.GetEntitiesOfTypeAsync(TypeNames.LearningProvider, cancellationToken);
            _logger.Info($"Loaded {learningProviders.Length} learning providers for matching");

            learningProviders = learningProviders.Where(lp => lp.Links == null || lp.Links.Length == 0).ToArray();
            _logger.Info($"Filtered learning providers down to {learningProviders.Length} that have no links");

            if (options.InitializeSearchIndexWithAllEntities)
            {
                _logger.Info("Loading learning providers into search...");
                var starterSearchDocuments = learningProviders.Select(e => e.ToSearchDocument()).ToArray();
                await _searchIndex.AddOrUpdateBatchAsync(starterSearchDocuments, cancellationToken);
                _logger.Info($"Loaded {starterSearchDocuments.Length} learning providers into search");
            }

            await Match(learningProviders, cancellationToken);
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

            _searchIndex = new AcsSearchIndex(new SearchConfiguration
            {
                AzureCognitiveSearchServiceName = options.AcsInstanceName,
                AzureCognitiveSearchKey = options.AcsAdminKey,
                IndexName = options.AcsIndexName,
            }, _logger);

            var entityLinker = new EntityLinker(_entityRepository, linkRepository, _searchIndex, _logger);

            var matchProfileProcessor = new MatchProfileProcessor(
                _entityRepository,
                linkRepository,
                _searchIndex,
                entityLinker,
                _logger);

            _matchManager = new MatchManager(
                _entityRepository,
                profileRepository,
                matchProfileProcessor,
                _searchIndex,
                _logger);
        }

        static async Task Match(Entity[] learningProviders, CancellationToken cancellationToken)
        {
            var tasks = new Task[4];
            var batchSize = (int) Math.Ceiling(learningProviders.Length / (float) tasks.Length);
            for (var i = 0; i < tasks.Length; i++)
            {
                var taskBatch = learningProviders.Skip(i * batchSize).Take(batchSize).ToArray();
                tasks[i] = MatchBatch(i, taskBatch, cancellationToken);
            }

            await Task.WhenAll(tasks);
        }

        static async Task MatchBatch(int taskId, Entity[] batch, CancellationToken cancellationToken)
        {
            for (var i = 0; i < batch.Length; i++)
            {
                var learningProvider = batch[i];
                _logger.Info(
                    $"task{taskId}: Matching {i} of {batch.Length}: {learningProvider.SourceSystemName}.{learningProvider.SourceSystemId}");

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


        static void Main(string[] args)
        {
            _logger = new Logger();

            CommandLineOptions options = null;
            Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed((parsed) => options = parsed);
            if (options != null)
            {
                var startTime = DateTime.Now;
                try
                {
                    Run(options).Wait();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }

                var duration = DateTime.Now - startTime;
                _logger.Info($"Completed in {duration:c}");

                _logger.Info("Done. Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}