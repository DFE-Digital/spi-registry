using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dfe.Spi.Common.Extensions;
using Dfe.Spi.Models.Entities;
using Dfe.Spi.Registry.Domain;
using Newtonsoft.Json;

namespace OfflineBulkMatcher.Output
{
    internal class EntitiesWriter : IDisposable
    {
        private readonly TextWriter _writer;

        public EntitiesWriter(string path)
        {
            _writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write));

            _writer.WriteLine("PartitionKey,RowKey,Data,SourceSystemId,SourceSystemName,Type,LinkId,LinkType");
        }

        public void WriteEntity(LearningProvider learningProvider, string sourceSystemName)
        {
            var sourceSystemId = sourceSystemName == "UKRLP"
                ? learningProvider.Ukprn.ToString()
                : learningProvider.Urn.ToString();

            var data = (new Dictionary<string, string>
                {
                    {DataAttributeNames.Name, learningProvider.Name},
                    {DataAttributeNames.Type, learningProvider.Type},
                    {DataAttributeNames.SubType, learningProvider.SubType},
                    {DataAttributeNames.OpenDate, learningProvider.OpenDate?.ToSpiString()},
                    {DataAttributeNames.CloseDate, learningProvider.CloseDate?.ToSpiString()},
                    {DataAttributeNames.Urn, learningProvider.Urn.ToString()},
                    {DataAttributeNames.Ukprn, learningProvider.Ukprn?.ToString()},
                    {DataAttributeNames.Uprn, learningProvider.Uprn},
                    {DataAttributeNames.CompaniesHouseNumber, learningProvider.CompaniesHouseNumber},
                    {DataAttributeNames.CharitiesCommissionNumber, learningProvider.CharitiesCommissionNumber},
                    {DataAttributeNames.AcademyTrustCode, learningProvider.AcademyTrustCode},
                    {DataAttributeNames.DfeNumber, learningProvider.DfeNumber},
                    {DataAttributeNames.LocalAuthorityCode, learningProvider.LocalAuthorityCode},
                    {DataAttributeNames.ManagementGroupType, learningProvider.ManagementGroup?.Type},
                    {DataAttributeNames.ManagementGroupId, learningProvider.ManagementGroup?.Code},
                }).Where(kvp => kvp.Value != null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            WriteRow(
                "learning-provider",
                $"{sourceSystemName.ToUpper()}:{sourceSystemId.ToLower()}",
                data.Count > 0 ? JsonConvert.SerializeObject(data) : null,
                sourceSystemId,
                sourceSystemName,
                "learning-provider",
                null,
                null);
        }

        public void WriteEntity(ManagementGroup managementGroup, string sourceSystemName)
        {
            var sourceSystemId = managementGroup.Code;

            var data = (new Dictionary<string, string>
                {
                    {DataAttributeNames.Name, managementGroup.Name},
                    {DataAttributeNames.ManagementGroupType, managementGroup.Type},
                    {DataAttributeNames.ManagementGroupId, managementGroup.Code},
                }).Where(kvp => kvp.Value != null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            WriteRow(
                "management-group",
                $"{sourceSystemName.ToUpper()}:{sourceSystemId.ToLower()}",
                data.Count > 0 ? JsonConvert.SerializeObject(data) : null,
                sourceSystemId,
                sourceSystemName,
                "management-group",
                null,
                null);
        }

        public void WriteLinks(LearningProvider learningProvider, string sourceSystemName, Link[] links)
        {
            var sourceSystemId = sourceSystemName == "UKRLP"
                ? learningProvider.Ukprn.ToString()
                : learningProvider.Urn.ToString();
            WriteLinks("learning-provider", sourceSystemName, sourceSystemId, links);
        }

        public void WriteLinks(ManagementGroup managementGroup, string sourceSystemName, Link[] links)
        {
            WriteLinks("management-group", sourceSystemName, managementGroup.Code, links);
        }


        private void WriteLinks(string entityType, string sourceSystemName, string sourceSystemId, Link[] links)
        {
            var entityKey = $"{entityType.ToLower()}:{sourceSystemName.ToUpper()}:{sourceSystemId.ToLower()}";
            foreach (var link in links)
            {
                var linkKey = $"{link.Type}:{link.Id}".ToLower();
                WriteRow(
                    entityKey,
                    linkKey,
                    null,
                    sourceSystemId,
                    sourceSystemName,
                    entityType,
                    link.Id,
                    link.Type);
            }
        }

        private void WriteRow(string partitionKey, string rowKey, string data, string sourceSystemId,
            string sourceSystemName, string type, string linkId, string linkType)
        {
            _writer.Write($"\"{partitionKey}\",\"{rowKey}\",");
            _writer.Write(data == null ? "," : $"\"{data.Replace("\"", "\"\"")}\",");
            _writer.Write(sourceSystemId == null ? "," : $"\"{sourceSystemId.Replace("\"", "\"\"")}\",");
            _writer.Write(sourceSystemName == null
                ? ","
                : $"\"{sourceSystemName.Replace("\"", "\"\"")}\",");
            _writer.Write(type == null ? "," : $"\"{type.Replace("\"", "\"\"")}\",");
            _writer.Write(linkId == null ? "," : $"\"{linkId.Replace("\"", "\"\"")}\",");
            _writer.WriteLine(linkType == null ? "" : $"\"{linkType.Replace("\"", "\"\"")}\"");
            _writer.Flush();
        }


        public void Dispose()
        {
            _writer?.Dispose();
        }
    }
}