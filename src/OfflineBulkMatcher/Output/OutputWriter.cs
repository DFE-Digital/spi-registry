using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.Models.Entities;

namespace OfflineBulkMatcher.Output
{
    public class OutputWriter
    {
        private readonly string _outputDir;
        private readonly SourceData _sourceData;
        private readonly Matches _matches;
        private readonly ILoggerWrapper _logger;

        public OutputWriter(
            string outputDir,
            SourceData sourceData,
            Matches matches,
            ILoggerWrapper logger)
        {
            _outputDir = outputDir;
            _sourceData = sourceData;
            _matches = matches;
            _logger = logger;
        }

        public void WriteOutput()
        {
            WriteEntities();
            WriteLinks();
            WriteSearchDocuments();
        }


        private void WriteEntities()
        {
            var entitiesPath = Path.Combine(_outputDir, "entities.csv");
            using (var entities = new EntitiesWriter(entitiesPath))
            {
                _logger.Info("Writing GIAS learning provider entities...");
                WriteLearningProviderEntities(entities, _sourceData.GetGiasLearningProviders(),
                    SourceSystemNames.GetInformationAboutSchools);

                _logger.Info("Writing UKRLP learning provider entities...");
                WriteLearningProviderEntities(entities, _sourceData.GetUkrlpLearningProviders(),
                    SourceSystemNames.UkRegisterOfLearningProviders);

                _logger.Info("Writing GIAS management group entities...");
                WriteManagementGroupEntities(entities, _sourceData.GetGiasManagementGroups(),
                    SourceSystemNames.GetInformationAboutSchools);
            }

            _logger.Info($"Saved entities to {entitiesPath}");
        }

        private void WriteLearningProviderEntities(EntitiesWriter entities,
            IEnumerable<LearningProvider> learningProviders, string sourceSystemName)
        {
            foreach (var learningProvider in learningProviders)
            {
                entities.WriteEntity(learningProvider, sourceSystemName);

                var entityLinks = _matches.GetEntityLinks(learningProvider, sourceSystemName);
                entities.WriteLinks(learningProvider, sourceSystemName, entityLinks);
            }
        }

        private void WriteManagementGroupEntities(EntitiesWriter entities,
            IEnumerable<ManagementGroup> managementGroups, string sourceSystemName)
        {
            foreach (var managementGroup in managementGroups)
            {
                entities.WriteEntity(managementGroup, sourceSystemName);

                var entityLinks = _matches.GetEntityLinks(managementGroup, sourceSystemName);
                entities.WriteLinks(managementGroup, sourceSystemName, entityLinks);
            }
        }

        private void WriteLinks()
        {
            var linksPath = Path.Combine(_outputDir, "links.csv");
            using (var links = new LinksWriter(linksPath))
            {
                _logger.Info("Writing links...");
                foreach (var link in _matches.GetAllLinks())
                {
                    links.WriteLink(link);
                }
            }

            _logger.Info($"Saved links to {linksPath}");
        }

        private void WriteSearchDocuments()
        {
            _logger.Info("Writing search documents...");

            var searchDocumentsPath = Path.Combine(_outputDir, "search-documents.json");
            var searchDocuments = new SearchDocumentsWriter(searchDocumentsPath);

            var giasProvidersWithSynonyms = new HashSet<long>();
            var ukrlpProvidersWithSynonyms = new HashSet<long>();

            // Write links
            foreach (var link in _matches.GetSynonyms())
            {
                var giasProvider = _sourceData.GetGiasLearningProvider(null,
                    long.Parse(link.Contents
                        .Single(p => p.SourceSystemName == SourceSystemNames.GetInformationAboutSchools)
                        .SourceSystemId));
                var ukrlpProvider = _sourceData.GetUkrlpLearningProvider(
                    long.Parse(link.Contents
                        .Single(p => p.SourceSystemName == SourceSystemNames.UkRegisterOfLearningProviders)
                        .SourceSystemId), null);
                searchDocuments.WriteSynonym(link, giasProvider, ukrlpProvider);

                giasProvidersWithSynonyms.Add(giasProvider.Urn.Value);
                ukrlpProvidersWithSynonyms.Add(ukrlpProvider.Ukprn.Value);
            }

            // Write GIAS providers
            foreach (var learningProvider in _sourceData.GetGiasLearningProviders())
            {
                if (!giasProvidersWithSynonyms.Contains(learningProvider.Urn.Value))
                {
                    searchDocuments.WriteEntity(learningProvider, SourceSystemNames.GetInformationAboutSchools);
                }
            }

            // Write UKRLP providers
            foreach (var learningProvider in _sourceData.GetUkrlpLearningProviders())
            {
                if (!ukrlpProvidersWithSynonyms.Contains(learningProvider.Ukprn.Value))
                {
                    searchDocuments.WriteEntity(learningProvider, SourceSystemNames.UkRegisterOfLearningProviders);
                }
            }

            // Write management groups
            foreach (var managementGroup in _sourceData.GetGiasManagementGroups())
            {
                searchDocuments.WriteEntity(managementGroup, SourceSystemNames.GetInformationAboutSchools);
            }

            // Save
            searchDocuments.Save();
        }
    }
}