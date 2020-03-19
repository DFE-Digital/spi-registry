using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.Models;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Search;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Search.Models;

namespace BulkMatch
{
    public class CachedSearchIndex : ISearchIndex
    {
        private readonly ISearchIndex _innerIndex;
        private readonly ILoggerWrapper _logger;
        private readonly Dictionary<string, SearchDocument> _searchDocuments;
        private readonly Dictionary<long, string[]> _urnPointers;
        private readonly Dictionary<long, string[]> _ukprnPointers;
        private readonly Dictionary<string, string[]> _managementGroupIdPointers;
        private readonly Dictionary<string, string[]> _referencePointerPointers;

        public CachedSearchIndex(ISearchIndex innerIndex, ILoggerWrapper logger)
        {
            _innerIndex = innerIndex;
            _logger = logger;

            _searchDocuments = new Dictionary<string, SearchDocument>();
            _urnPointers = new Dictionary<long, string[]>();
            _ukprnPointers = new Dictionary<long, string[]>();
            _managementGroupIdPointers = new Dictionary<string, string[]>();
            _referencePointerPointers = new Dictionary<string, string[]>();
        }

        public Task<SearchIndexResult> SearchAsync(SearchRequest request, string entityType,
            CancellationToken cancellationToken)
        {
            _logger.Debug($"Cached search for {request}");

            if (request.Groups.Length > 1)
            {
                throw new Exception("Only supports single group");
            }

            if (request.CombinationOperator != "and" || request.Groups[0].CombinationOperator != "and")
            {
                throw new Exception("Only supports and CombinationOperator");
            }

            if (request.Groups[0].Filter.Length > 1)
            {
                throw new Exception("Only supports single filter");
            }

            if (request.Groups[0].Filter[0].Operator != DataOperator.Equals &&
                !(request.Groups[0].Filter[0].Operator == DataOperator.In && request.Groups[0].Filter[0].Field == "ReferencePointer"))
            {
                throw new Exception(
                    $"Only support Equals (received {request.Groups[0].Filter[0].Field} {request.Groups[0].Filter[0].Operator.ToString()} {request.Groups[0].Filter[0].Value}");
            }

            var filter = request.Groups[0].Filter[0];

            string[] documentIds;
            switch (filter.Field)
            {
                case "ReferencePointer":
                    var referencePointerValues = filter.Value.Split(',');
                    var allDocumentIds = new List<string>();
                    
                    foreach (var value in referencePointerValues)
                    {
                        if (_referencePointerPointers.ContainsKey(value))
                        {
                            allDocumentIds.AddRange(_referencePointerPointers[value]);
                        }
                    }

                    documentIds = allDocumentIds.Distinct().ToArray();
                    break;
                case DataAttributeNames.Urn:
                    if (long.TryParse(filter.Value, out var urn))
                    {
                        documentIds = _urnPointers.ContainsKey(urn) ? _urnPointers[urn] : new string[0];
                    }
                    else
                    {
                        documentIds = new string[0];
                    }
                    break;
                case DataAttributeNames.Ukprn:
                    if (long.TryParse(filter.Value, out var ukprn))
                    {
                        documentIds = _ukprnPointers.ContainsKey(ukprn) ? _ukprnPointers[ukprn] : new string[0];
                    }
                    else
                    {
                        documentIds = new string[0];
                    }
                    break;
                case DataAttributeNames.ManagementGroupId:
                    var managementGroupId = filter.Value.ToLower();
                    documentIds = _managementGroupIdPointers.ContainsKey(managementGroupId)
                        ? _managementGroupIdPointers[managementGroupId]
                        : new string[0];
                    break;
                default:
                    throw new Exception($"Unexpected filter field {filter.Field}");
            }

            var candidates = new SearchDocument[documentIds.Length];
            for (var i = 0; i < documentIds.Length; i++)
            {
                candidates[i] = _searchDocuments[documentIds[i]];
            }

            var matches = candidates.Where(doc => doc.EntityType.Equals(entityType, StringComparison.InvariantCulture))
                .ToArray();

            // var group = request.Groups[0];
            // IEnumerable<SearchDocument> query = _searchDocuments.Where(doc =>
            //     doc.EntityType.Equals(entityType, StringComparison.InvariantCultureIgnoreCase));
            // foreach (var filter in group.Filter)
            // {
            //     if (filter.Operator != DataOperator.Equals)
            //     {
            //         throw new Exception(
            //             $"Only support Equals (received {filter.Field} {filter.Operator.ToString()} {filter.Value}");
            //     }
            //
            //     switch (filter.Field)
            //     {
            //         case DataAttributeNames.Urn:
            //             query = query.Where(doc => doc.Urn.Any(urn => urn.ToString() == filter.Value));
            //             break;
            //         case DataAttributeNames.Ukprn:
            //             query = query.Where(doc => doc.Ukprn.Any(ukprn => ukprn.ToString() == filter.Value));
            //             break;
            //         case DataAttributeNames.ManagementGroupId:
            //             query = query.Where(doc => doc.ManagementGroupId.Any(managementGroupId => managementGroupId == filter.Value));
            //             break;
            //         default:
            //             throw new Exception($"Unexpected filter field {filter.Field}");
            //     }
            // }
            //
            // var matches = query.ToArray();

            _logger.Debug($"Found {matches.Length} matches");
            return Task.FromResult(new SearchIndexResult
            {
                Results = matches,
                Skipped = 0,
                Taken = matches.Length,
                TotalNumberOfRecords = matches.Length,
            });
        }

        public Task AddOrUpdateAsync(SearchDocument document, CancellationToken cancellationToken)
        {
            lock (this)
            {
                LockedDelete(document);
                _searchDocuments.Add(document.Id, document);
                AddPointers(_urnPointers, document.Urn, document.Id);
                AddPointers(_ukprnPointers, document.Ukprn, document.Id);
                AddPointers(_managementGroupIdPointers, document.ManagementGroupId, document.Id);
                AddPointers(_referencePointerPointers, new[] {document.ReferencePointer}, document.Id);
            }

            _logger.Debug($"Added {document.ReferencePointer} - {document.Name} / {document.Urn}");
            return Task.CompletedTask;
        }

        public async Task AddOrUpdateBatchAsync(SearchDocument[] documents, CancellationToken cancellationToken)
        {
            foreach (var document in documents)
            {
                await AddOrUpdateAsync(document, cancellationToken);
            }
        }

        public Task DeleteAsync(SearchDocument document, CancellationToken cancellationToken)
        {
            lock (this)
            {
                LockedDelete(document);
            }
            
            return Task.CompletedTask;
        }

        public async Task DeleteBatchAsync(SearchDocument[] documents, CancellationToken cancellationToken)
        {
            foreach (var document in documents)
            {
                await DeleteAsync(document, cancellationToken);
            }
        }


        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            _logger.Debug($"Saving {_searchDocuments.Count} documents to Azure Search...");

            var index = 0;
            while (index < _searchDocuments.Count)
            {
                var batch = _searchDocuments.Skip(index).Take(100).Select(kvp => kvp.Value).ToArray();
                _logger.Debug($"Saving batch {index} to {index + batch.Length} of {_searchDocuments.Count}");

                await _innerIndex.AddOrUpdateBatchAsync(batch, cancellationToken);
            }

            _logger.Debug($"Saved {_searchDocuments.Count} documents to Azure Search");
        }

        private void LockedDelete(SearchDocument document)
        {
            if (_searchDocuments.ContainsKey(document.Id))
            {
                var listInstance = _searchDocuments[document.Id];
                RemovePointers(_urnPointers, listInstance.Urn, document.Id);
                RemovePointers(_ukprnPointers, listInstance.Ukprn, document.Id);
                RemovePointers(_managementGroupIdPointers, listInstance.ManagementGroupId, document.Id);
                RemovePointers(_referencePointerPointers, new[]{document.ReferencePointer}, document.Id);
                _searchDocuments.Remove(document.Id);
            }
        }

        private void RemovePointers<T>(Dictionary<T, string[]> pointers, T[] values, string documentId)
        {
            foreach (var value in values)
            {
                if (pointers.ContainsKey(value))
                {
                    var pointerIds = pointers[value];
                    pointerIds = pointerIds.Where(id => id != documentId).ToArray();
                    if (pointerIds.Length > 0)
                    {
                        pointers[value] = pointerIds;
                    }
                    else
                    {
                        pointers.Remove(value);
                    }
                }
            }
        }

        private void AddPointers<T>(Dictionary<T, string[]> pointers, T[] values, string documentId)
        {
            foreach (var value in values)
            {
                if (pointers.ContainsKey(value))
                {
                    var pointerIds = pointers[value];
                    pointerIds = pointerIds.Concat(new[] {documentId}).ToArray();
                    pointers[value] = pointerIds;
                }
                else
                {
                    pointers.Add(value, new[] {documentId});
                }
            }
        }
    }
}