using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Models.Entities;
using Newtonsoft.Json;

namespace OfflineBulkMatcher
{
    public class SourceData
    {
        private readonly ILoggerWrapper _logger;
        private LearningProvider[] _giasLearningProviders;
        private LearningProvider[] _ukrlpLearningProviders;
        private ManagementGroup[] _giasManagementGroups;
        private ConcurrentQueue<LearningProvider> _matchingQueue;

        public SourceData(ILoggerWrapper logger)
        {
            _logger = logger;
        }

        public async Task LoadAsync(
            string giasLearningProvidersPath,
            string ukrlpLearningProvidersPath,
            string giasManagementGroupsPath)
        {
            _logger.Info($"Reading GIAS learning providers from {giasLearningProvidersPath}");
            _giasLearningProviders = await ReadAndDeserializeAsync<LearningProvider>(giasLearningProvidersPath);
            _logger.Info($"Found {_giasLearningProviders.Length} GIAS learning providers");
            
            _logger.Info($"Reading UKRLP learning providers from {ukrlpLearningProvidersPath}");
            _ukrlpLearningProviders = await ReadAndDeserializeAsync<LearningProvider>(ukrlpLearningProvidersPath);
            _logger.Info($"Found {_ukrlpLearningProviders.Length} UKRLP learning providers");
            
            _logger.Info($"Reading GIAS management groups from {giasManagementGroupsPath}");
            _giasManagementGroups = await ReadAndDeserializeAsync<ManagementGroup>(giasManagementGroupsPath);
            _logger.Info($"Found {_giasManagementGroups.Length} GIAS management groups");

            _matchingQueue = new ConcurrentQueue<LearningProvider>();
            foreach (var learningProvider in _giasLearningProviders)
            {
                _matchingQueue.Enqueue(learningProvider);
            }
        }

        public LearningProvider GetNextLearningProviderToMatch()
        {
            return _matchingQueue.TryDequeue(out var learningProvider) ? learningProvider : null;
        }

        public int GetCountOfOutstanding()
        {
            return _matchingQueue.Count;
        }

        public LearningProvider GetGiasLearningProvider(long? ukprn, long? urn)
        {
            return _giasLearningProviders
                .FirstOrDefault(p => 
                    (urn.HasValue && p.Urn == urn) || 
                    (ukprn.HasValue && p.Ukprn == ukprn));
        }

        public LearningProvider GetUkrlpLearningProvider(long? ukprn, long? urn)
        {
            return _ukrlpLearningProviders
                .FirstOrDefault(p => 
                    (urn.HasValue && p.Urn == urn) || 
                    (ukprn.HasValue && p.Ukprn == ukprn));
        }

        public ManagementGroup GetGiasManagementGroup(string code)
        {
            return _giasManagementGroups
                .FirstOrDefault(mg =>
                    mg.Code.Equals(code, StringComparison.InvariantCultureIgnoreCase));
        }

        public IEnumerable<LearningProvider> GetGiasLearningProviders()
        {
            return _giasLearningProviders;
        }

        public IEnumerable<LearningProvider> GetUkrlpLearningProviders()
        {
            return _ukrlpLearningProviders;
        }

        public IEnumerable<ManagementGroup> GetGiasManagementGroups()
        {
            return _giasManagementGroups;
        }


        private async Task<T[]> ReadAndDeserializeAsync<T>(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(stream))
            {
                var json = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<T[]>(json);
            }
        }
    }
}