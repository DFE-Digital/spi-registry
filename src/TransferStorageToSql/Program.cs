using System;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Dfe.Spi.Common.Logging.Definitions;

namespace TransferStorageToSql
{
    class Program
    {
        private static ILoggerWrapper _logger;

        private static async Task Run(CommandLineOptions options, CancellationToken cancellationToken = default)
        {
            var storageReader = new StorageReader(options.StorageConnectionString, _logger);
            
            using var bulkSqlWriter = new BulkSqlWriter(options.SqlConnectionString, _logger);

            var entities = await ReadEntitiesFromStorage(storageReader, cancellationToken);
            var links = await ReadLinksFromStorage(storageReader, cancellationToken);
            await WriteToSql(bulkSqlWriter, entities, links, cancellationToken);

            // using var sqlWriter = new SqlWriter(options.SqlConnectionString, _logger);

            // await TransferEntities(storageReader, sqlWriter, cancellationToken);
            // await TransferLinks(storageReader, sqlWriter, cancellationToken);
        }

        static async Task<Entity[]> ReadEntitiesFromStorage(StorageReader storageReader, CancellationToken cancellationToken)
        {
            _logger.Info("Reading entities...");
            var entities = await storageReader.ReadAllEntitiesAsync(cancellationToken);
            _logger.Info($"Read {entities.Length} entities");
            return entities;
        }
        static async Task<Link[]> ReadLinksFromStorage(StorageReader storageReader, CancellationToken cancellationToken)
        {
            _logger.Info("Reading links...");
            var links = await storageReader.ReadAllLinks(cancellationToken);
            _logger.Info($"Read {links.Length} links");
            return links;
        }

        static async Task WriteToSql(BulkSqlWriter bulkSqlWriter, Entity[] entities, Link[] links, CancellationToken cancellationToken)
        {
            _logger.Info("Writing to SQL...");
            await bulkSqlWriter.StoreAsync(entities, links, cancellationToken);
        }

        static async Task TransferEntities(StorageReader storageReader, SqlWriter sqlWriter, CancellationToken cancellationToken)
        {
            _logger.Info("Reading entities...");
            var entities = await storageReader.ReadAllEntitiesAsync(cancellationToken);
            _logger.Info($"Read {entities.Length} entities");

            _logger.Info("Storing entities...");
            for (var i = 0; i < entities.Length; i++)
            {
                _logger.Debug($"Storing {i} of {entities.Length} entities");
                var entity = entities[i];
                
                await sqlWriter.StoreAsync(entity, cancellationToken);
            }
            _logger.Info($"Stored {entities.Length} entities");
        }

        static async Task TransferLinks(StorageReader storageReader, SqlWriter sqlWriter, CancellationToken cancellationToken)
        {
            _logger.Info("Reading links...");
            var links = await storageReader.ReadAllLinks(cancellationToken);
            _logger.Info($"Read {links.Length} links");

            _logger.Info("Storing links...");
            for (var i = 0; i < links.Length; i++)
            {
                _logger.Debug($"Storing {i} of {links.Length} links");
                var link = links[i];
                
                await sqlWriter.StoreAsync(link, cancellationToken);
            }
            _logger.Info($"Stored {links.Length} links");
        }

        
        static void Main(string[] args)
        {
            _logger = new TimedLogger(new Logger());

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
                    _logger.Error(ex.Message, ex);
                }

                var duration = DateTime.Now - startTime;
                _logger.Info($"Completed in {duration:c}");

                _logger.Info("Done. Press any key to exit...");
            }
        }
    }
}