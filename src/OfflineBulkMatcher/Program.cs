using System;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Dfe.Spi.Models.Entities;
using OfflineBulkMatcher.Output;

namespace OfflineBulkMatcher
{
    class Program
    {
        private static Logger _logger;

        private static async Task Run(CommandLineOptions options, CancellationToken cancellationToken = default)
        {
            // Load data
            var sourceData = new SourceData(_logger);
            await sourceData.LoadAsync(
                options.GiasLearningProviderFilePath,
                options.UkrlpLearningProviderFilePath,
                options.GiasManagementGroupFilePath);

            // Process
            var matches = new Matches(_logger);
            
            var tasks = new Task[options.Threads];
            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = MatchAsync(i, sourceData, matches, cancellationToken);
            }
            await Task.WhenAll(tasks);
            
            // Output
            matches.LogLinkStatus();
            var outputWriter = new OutputWriter(options.OutputDirectory, sourceData, matches, _logger);
            outputWriter.WriteOutput();
        }


        private static Task MatchAsync(int taskIndex, SourceData sourceData, Matches matches, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                LearningProvider learningProvider = sourceData.GetNextLearningProviderToMatch();
                while (learningProvider != null)
                {
                    _logger.Info(
                        $"task{taskIndex}: Dequeued {learningProvider.Urn} {learningProvider.Name}. {sourceData.GetCountOfOutstanding()} outstanding");

                    var matchingProvider =
                        sourceData.GetUkrlpLearningProvider(learningProvider.Ukprn, learningProvider.Urn);
                    var matchingManagementGroup =
                        sourceData.GetGiasManagementGroup(learningProvider.ManagementGroup.Code);

                    matches.AddMatch(learningProvider, matchingProvider, matchingManagementGroup);

                    learningProvider = sourceData.GetNextLearningProviderToMatch();
                }

                _logger.Info($"task{taskIndex}: No more provides to match, finishing");
            }, cancellationToken);
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
            }
        }
    }
}