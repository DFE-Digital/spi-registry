using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.Models.Entities;
using Newtonsoft.Json.Converters;

namespace OfflineBulkMatcher
{
    public class Matches
    {
        private readonly ILoggerWrapper _logger;
        private readonly ConcurrentDictionary<string, Link> _links;
        private readonly ConcurrentDictionary<string, List<string>> _entityLinkReferences;

        public Matches(ILoggerWrapper logger)
        {
            _logger = logger;
            _links = new ConcurrentDictionary<string, Link>();
            _entityLinkReferences = new ConcurrentDictionary<string, List<string>>();
        }
        
        public void AddMatch(LearningProvider giasLearningProvider, LearningProvider ukrlpLearningProvider,
            ManagementGroup managementGroup)
        {
            if (ukrlpLearningProvider != null)
            {
                var link = new Link
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "Synonym",
                    Contents = new[]
                    {
                        new Pointer
                        {
                            EntityType = "learning-provider",
                            SourceSystemName = SourceSystemNames.GetInformationAboutSchools,
                            SourceSystemId = giasLearningProvider.Urn.ToString(),
                        },
                        new Pointer
                        {
                            EntityType = "learning-provider",
                            SourceSystemName = SourceSystemNames.UkRegisterOfLearningProviders,
                            SourceSystemId = ukrlpLearningProvider.Ukprn.ToString(),
                        },
                    },
                };
                SaveLink(link);
            }

            if (managementGroup != null)
            {
                var pointers = new List<Pointer>
                {
                    new Pointer
                    {
                        EntityType = "learning-provider",
                        SourceSystemName = SourceSystemNames.GetInformationAboutSchools,
                        SourceSystemId = giasLearningProvider.Urn.ToString(),
                    },
                    new Pointer
                    {
                        EntityType = "management-group",
                        SourceSystemName = SourceSystemNames.GetInformationAboutSchools,
                        SourceSystemId = managementGroup.Code,
                    },
                };
                if (ukrlpLearningProvider != null)
                {
                    pointers.Add(new Pointer
                    {
                        EntityType = "learning-provider",
                        SourceSystemName = SourceSystemNames.UkRegisterOfLearningProviders,
                        SourceSystemId = ukrlpLearningProvider.Ukprn.ToString(),
                    });
                }
                
                var link = new Link
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "ManagementGroup",
                    Contents = pointers.ToArray(),
                };
                SaveLink(link);
            }
        }

        public void LogLinkStatus()
        {
            _logger.Info($"Created {_links.Count} links");

            var synonymLinks = _links.Keys.Count(k => k.StartsWith("Synonym:", StringComparison.InvariantCultureIgnoreCase));
            _logger.Info($"Created {synonymLinks} synonyms");

            var groupLinks = _links.Keys.Count(k => k.StartsWith("ManagementGroup:", StringComparison.InvariantCultureIgnoreCase));
            _logger.Info($"Created {groupLinks} management groups");
        }

        public Link[] GetEntityLinks(LearningProvider learningProvider, string sourceSystemName)
        {
            var sourceSystemId = sourceSystemName == "UKRLP"
                ? learningProvider.Ukprn.ToString()
                : learningProvider.Urn.ToString();
            return GetEntityLinks($"learning-provider:{sourceSystemName.ToUpper()}:{sourceSystemId.ToLower()}");
        }
        public Link[] GetEntityLinks(ManagementGroup managementGroup, string sourceSystemName)
        {
            return GetEntityLinks($"management-group:{sourceSystemName.ToUpper()}:{managementGroup.Code.ToLower()}");
        }

        public IEnumerable<Link> GetSynonyms()
        {
            return _links
                .Where(kvp => kvp.Key.StartsWith("Synonym:", StringComparison.InvariantCultureIgnoreCase))
                .Select(kvp => kvp.Value);
        }

        public IEnumerable<Link> GetAllLinks()
        {
            return _links.Values;
        }

        public Link[] GetEntityLinks(string entityKey)
        {
            if (_entityLinkReferences.TryGetValue(entityKey, out var linkIds))
            {
                var links = new Link[linkIds.Count];
                
                for (var i = 0; i < linkIds.Count; i++)
                {
                    if (string.IsNullOrEmpty(linkIds[i]))
                    {
                        throw new Exception($"Somehow got a blank link id in entity {entityKey}");
                    }
                    if (!_links.TryGetValue(linkIds[i], out var link))
                    {
                        throw new Exception($"Failed to get referenced link {linkIds[i]} - should not happen");
                    }

                    links[i] = link;
                }

                return links;
            }
            
            return  new Link[0];
        }


        private void SaveLink(Link link)
        {
            var linkKey = $"{link.Type.ToLower()}:{link.Id.ToLower()}";
            
            if (!_links.TryAdd(linkKey, link))
            {
                throw new Exception($"Failed to add link {linkKey} - this should not happen!");
            }

            foreach (var pointer in link.Contents)
            {
                var key = $"{pointer.EntityType.ToLower()}:{pointer.SourceSystemName.ToUpper()}:{pointer.SourceSystemId.ToLower()}";
                List<string> entityLinks = null;
                var attempts = 0;

                while (entityLinks == null && attempts < 2)
                {
                    if (!_entityLinkReferences.TryGetValue(key, out entityLinks))
                    {
                        entityLinks = new List<string>();
                        if (!_entityLinkReferences.TryAdd(key, entityLinks))
                        {
                            entityLinks = null;
                        }
                    }

                    attempts++;
                }

                if (entityLinks == null)
                {
                    throw new Exception($"Failed to add or get entity links for {key}");
                }

                entityLinks.Add(linkKey);
            }
        }
    }
        
    public class Link
    {
        public string Type { get; set; }
        public string Id { get; set; }
        public Pointer[] Contents { get; set; }
    }
    public class Pointer
    {
        public string EntityType { get; set; }
        public string SourceSystemName { get; set; }
        public string SourceSystemId { get; set; }

        public override string ToString()
        {
            return $"{EntityType.ToLower()}:{SourceSystemName.ToUpper()}:{SourceSystemId.ToLower()}";
        }
    }
}