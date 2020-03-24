using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Dfe.Spi.Models.Entities;
using Dfe.Spi.Registry.Application.Entities;
using Dfe.Spi.Registry.Application.LearningProviders;
using Dfe.Spi.Registry.Domain.Configuration;
using Dfe.Spi.Registry.Domain.Queuing;
using Dfe.Spi.Registry.Infrastructure.AzureCognitiveSearch;
using Dfe.Spi.Registry.Infrastructure.AzureStorage.Entities;
using Dfe.Spi.Registry.Infrastructure.AzureStorage.Links;
using Dfe.Spi.Registry.Infrastructure.AzureStorage.Queuing;
using Newtonsoft.Json;

namespace SeedFromLearningProviderFile
{
    class Program
    {
        private static Logger _logger;
        private static ILearningProviderManager _learningProviderManager;

        static async Task Run(CommandLineOptions options, CancellationToken cancellationToken = default)
        {
            Init(options);

            var learningProviders = await ReadLearningProviders(options.InputPath);
            _logger.Info($"Read {learningProviders.Length} learning providers");

            await SyncLearningProviders(learningProviders, options.OriginSource, cancellationToken);
        }

        static void Init(CommandLineOptions options)
        {
            var entityRepository = new CompositeTableEntityRepository(new EntitiesConfiguration
            {
                TableStorageConnectionString = options.StorageConnectionString,
                TableStorageTableName = options.EntitiesTableName,
            }, _logger);

            var linkRepository = new TableLinkRepository(new LinksConfiguration
            {
                TableStorageConnectionString = options.StorageConnectionString,
                TableStorageTableName = options.LinksTableName,
            });

            var matchingQueue = options.BypassMatching
                ? (IMatchingQueue)new NoopMatchingQueue()
                : (IMatchingQueue)new StorageMatchingQueue(
                    new QueueConfiguration
                    {
                        StorageQueueConnectionString = options.StorageConnectionString,
                    });

            var searchIndex = new AcsSearchIndex(new SearchConfiguration
            {
                AzureCognitiveSearchServiceName = options.AcsInstanceName,
                AzureCognitiveSearchKey = options.AcsAdminKey,
                IndexName = options.AcsIndexName,
            }, _logger);
            
            var entityManager = new EntityManager(
                entityRepository,
                linkRepository,
                matchingQueue,
                searchIndex,
                _logger);
            
            _learningProviderManager = new LearningProviderManager(
                entityManager,
                _logger);
        }

        static async Task<LearningProvider[]> ReadLearningProviders(string path)
        {
            _logger.Info($"Reading learning providers from {path}");
            using(var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(stream))
            {
                var json = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<LearningProvider[]>(json);
            }
        }

        static async Task SyncLearningProviders(LearningProvider[] learningProviders, string source,
            CancellationToken cancellationToken)
        {
            for (var i = 0; i < learningProviders.Length; i++)
            {
                _logger.Info($"Syncing learning provider {i} of {learningProviders.Length}");
                await _learningProviderManager.SyncLearningProviderAsync(source, learningProviders[i], cancellationToken);
            }
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