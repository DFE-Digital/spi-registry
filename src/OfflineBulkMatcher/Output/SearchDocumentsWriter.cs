using System;
using System.IO;
using System.Linq;
using Dfe.Spi.Models.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OfflineBulkMatcher.Output
{
    internal class SearchDocumentsWriter
    {
        private readonly string _path;
        private readonly JObject _root;
        private readonly JArray _value;

        public SearchDocumentsWriter(string path)
        {
            _path = path;
            _value = new JArray();
            _root = new JObject(
                new JProperty("value", _value));
        }

        public void WriteSynonym(Link link, LearningProvider giasLearningProvider,
            LearningProvider ukrlpLearningProvider)
        {
            if (giasLearningProvider == null || ukrlpLearningProvider == null)
            {
                throw new NullReferenceException(
                    $"Missing provider for synonym {link.Id}. GIAS {giasLearningProvider?.Urn}. UKRLP {ukrlpLearningProvider?.Ukprn}." +
                    $"Contents {link.Contents.Select(x => x.ToString()).Aggregate((x, y) => x + ", " + y)}");
            }

            var id = Guid.NewGuid().ToString().ToLower();
            var entityType = "learning-provider";
            var referencePointer = $"link:synonym:{link.Id.ToLower()}";
            var name = GetDistinctArray(giasLearningProvider.Name, ukrlpLearningProvider.Name);
            var sortableName = name.FirstOrDefault()?.ToLower();
            var type = GetDistinctArray(giasLearningProvider.Type, ukrlpLearningProvider.Type);
            var subType = GetDistinctArray(giasLearningProvider.SubType, ukrlpLearningProvider.SubType);
            var status = GetDistinctArray(giasLearningProvider.Status, ukrlpLearningProvider.Status);
            var openDate = GetDistinctArray(giasLearningProvider.OpenDate, ukrlpLearningProvider.OpenDate);
            var closeDate = GetDistinctArray(giasLearningProvider.CloseDate, ukrlpLearningProvider.CloseDate);
            var urn = GetDistinctArray(giasLearningProvider.Urn, ukrlpLearningProvider.Urn);
            var ukprn = GetDistinctArray(giasLearningProvider.Ukprn, ukrlpLearningProvider.Ukprn);
            var uprn = GetDistinctArray(giasLearningProvider.Uprn, ukrlpLearningProvider.Uprn);
            var companiesHouseNumber = GetDistinctArray(giasLearningProvider.CompaniesHouseNumber,
                ukrlpLearningProvider.CompaniesHouseNumber);
            var charityCommisionNumber = GetDistinctArray(giasLearningProvider.CharitiesCommissionNumber,
                ukrlpLearningProvider.CharitiesCommissionNumber);
            var academyTrustCode = GetDistinctArray(giasLearningProvider.AcademyTrustCode,
                ukrlpLearningProvider.AcademyTrustCode);
            var dfeNumber = GetDistinctArray(giasLearningProvider.DfeNumber, ukrlpLearningProvider.DfeNumber);
            var localAuthorityCode = GetDistinctArray(giasLearningProvider.LocalAuthorityCode,
                ukrlpLearningProvider.LocalAuthorityCode);
            var managementGroupType = GetDistinctArray(giasLearningProvider.ManagementGroup?.Type,
                ukrlpLearningProvider.ManagementGroup?.Type);
            var managementGroupId = GetDistinctArray(giasLearningProvider.ManagementGroup?.Code,
                ukrlpLearningProvider.ManagementGroup?.Code);
            var managementGroupUkprn = GetDistinctArray(giasLearningProvider.ManagementGroup?.Ukprn, 
                ukrlpLearningProvider.ManagementGroup?.Ukprn);
            var managementGroupCompaniesHouseNumber = GetDistinctArray(giasLearningProvider.ManagementGroup?.CompaniesHouseNumber,
                ukrlpLearningProvider.ManagementGroup?.CompaniesHouseNumber);

            WriteEntry(id, sortableName, entityType, referencePointer,
                name, type, subType, status, openDate, closeDate,
                urn, ukprn, uprn, companiesHouseNumber, charityCommisionNumber,
                academyTrustCode, dfeNumber, localAuthorityCode,
                managementGroupType, managementGroupId, managementGroupUkprn, managementGroupCompaniesHouseNumber);
        }

        public void WriteEntity(LearningProvider learningProvider, string sourceSystemName)
        {
            var sourceSystemId = sourceSystemName == "UKRLP"
                ? learningProvider.Ukprn.ToString()
                : learningProvider.Urn.ToString();

            var id = Guid.NewGuid().ToString().ToLower();
            var entityType = "learning-provider";
            var referencePointer = $"entity:learning-provider:{sourceSystemName}:{sourceSystemId.ToLower()}";
            var name = GetDistinctArray(learningProvider.Name);
            var sortableName = name.FirstOrDefault()?.ToLower();
            var type = GetDistinctArray(learningProvider.Type);
            var subType = GetDistinctArray(learningProvider.SubType);
            var status = GetDistinctArray(learningProvider.Status);
            var openDate = GetDistinctArray(learningProvider.OpenDate);
            var closeDate = GetDistinctArray(learningProvider.CloseDate);
            var urn = GetDistinctArray(learningProvider.Urn);
            var ukprn = GetDistinctArray(learningProvider.Ukprn);
            var uprn = GetDistinctArray(learningProvider.Uprn);
            var companiesHouseNumber = GetDistinctArray(learningProvider.CompaniesHouseNumber);
            var charityCommisionNumber = GetDistinctArray(learningProvider.CharitiesCommissionNumber);
            var academyTrustCode = GetDistinctArray(learningProvider.AcademyTrustCode);
            var dfeNumber = GetDistinctArray(learningProvider.DfeNumber);
            var localAuthorityCode = GetDistinctArray(learningProvider.LocalAuthorityCode);
            var managementGroupType = GetDistinctArray(learningProvider.ManagementGroup?.Type);
            var managementGroupId = GetDistinctArray(learningProvider.ManagementGroup?.Code);
            var managementGroupUkprn = GetDistinctArray(learningProvider.ManagementGroup?.Ukprn);
            var managementGroupCompaniesHouseNumber = GetDistinctArray(learningProvider.ManagementGroup?.CompaniesHouseNumber);

            WriteEntry(id, sortableName, entityType, referencePointer,
                name, type, subType, status, openDate, closeDate,
                urn, ukprn, uprn, companiesHouseNumber, charityCommisionNumber,
                academyTrustCode, dfeNumber, localAuthorityCode,
                managementGroupType, managementGroupId, managementGroupUkprn, managementGroupCompaniesHouseNumber);
        }

        public void WriteEntity(ManagementGroup managementGroup, string sourceSystemName)
        {
            var id = Guid.NewGuid().ToString().ToLower();
            var entityType = "management-group";
            var referencePointer = $"entity:management-group:{sourceSystemName}:{managementGroup.Code.ToLower()}";
            var name = new string[0];
            string sortableName = null;
            var type = new string[0];
            var subType = new string[0];
            var status = new string[0];
            var openDate = new DateTime[0];
            var closeDate = new DateTime[0];
            var urn = new long[0];
            var ukprn = new long[0];
            var uprn = new string[0];
            var companiesHouseNumber = new string[0];
            var charityCommisionNumber = new string[0];
            var academyTrustCode = new string[0];
            var dfeNumber = new string[0];
            var localAuthorityCode = new string[0];
            var managementGroupType = GetDistinctArray(managementGroup.Type);
            var managementGroupId = GetDistinctArray(managementGroup.Code);
            var managementGroupUkprn = GetDistinctArray(managementGroup?.Ukprn);
            var managementGroupCompaniesHouseNumber = GetDistinctArray(managementGroup?.CompaniesHouseNumber);

            WriteEntry(id, sortableName, entityType, referencePointer,
                name, type, subType, status, openDate, closeDate,
                urn, ukprn, uprn, companiesHouseNumber, charityCommisionNumber,
                academyTrustCode, dfeNumber, localAuthorityCode,
                managementGroupType, managementGroupId, managementGroupUkprn, managementGroupCompaniesHouseNumber);
        }

        public void Save()
        {
            var json = _root.ToString(Formatting.Indented);
            File.WriteAllText(_path, json);
        }


        private void WriteEntry(string id, string sortableName, string entityType, string referencePointer,
            string[] name, string[] type, string[] subType, string[] status, DateTime[] openDate,
            DateTime[] closeDate,
            long[] urn, long[] ukprn, string[] uprn, string[] companiesHouseNumber, string[] charityCommisionNumber,
            string[] academyTrustCode, string[] dfeNumber, string[] localAuthorityCode,
            string[] managementGroupType, string[] managementGroupId, long[] managementGroupUkprn, string[] managementGroupCompaniesHouseNumber)
        {
            _value.Add(new JObject(
                new JProperty("@search.action", "upload"),
                new JProperty("Id", id),
                new JProperty("SortableEntityName", sortableName),
                new JProperty("EntityType", entityType),
                new JProperty("ReferencePointer", referencePointer),
                new JProperty("Name", name != null ? new JArray(name) : null),
                new JProperty("Status", status != null ? new JArray(status) : null),
                new JProperty("Type", type != null ? new JArray(type) : null),
                new JProperty("SubType", subType != null ? new JArray(subType) : null),
                new JProperty("OpenDate", openDate != null ? new JArray(ToString(openDate)) : null),
                new JProperty("CloseDate", closeDate != null ? new JArray(ToString(closeDate)) : null),
                new JProperty("Urn", urn != null ? new JArray(urn) : null),
                new JProperty("Ukprn", ukprn != null ? new JArray(ukprn) : null),
                new JProperty("Uprn", uprn != null ? new JArray(uprn) : null),
                new JProperty("CompaniesHouseNumber",
                    companiesHouseNumber != null ? new JArray(companiesHouseNumber) : null),
                new JProperty("CharitiesCommissionNumber",
                    charityCommisionNumber != null ? new JArray(charityCommisionNumber) : null),
                new JProperty("AcademyTrustCode", academyTrustCode != null ? new JArray(academyTrustCode) : null),
                new JProperty("DfeNumber", dfeNumber != null ? new JArray(dfeNumber) : null),
                new JProperty("LocalAuthorityCode",
                    localAuthorityCode != null ? new JArray(localAuthorityCode) : null),
                new JProperty("ManagementGroupType",
                    managementGroupType != null ? new JArray(managementGroupType) : null),
                new JProperty("ManagementGroupId",
                    managementGroupId != null ? new JArray(managementGroupId) : null),
                new JProperty("ManagementGroupUkprn",
                    managementGroupId != null ? new JArray(managementGroupUkprn) : null),
                new JProperty("ManagementGroupCompaniesHouseNumber",
                    managementGroupId != null ? new JArray(managementGroupCompaniesHouseNumber) : null)));
        }

        private string[] GetDistinctArray(params string[] values)
        {
            return values.Distinct().Where(v => !string.IsNullOrEmpty(v)).ToArray();
        }

        private long[] GetDistinctArray(params long?[] values)
        {
            var distinct = values.Where(v => v.HasValue).Distinct().ToArray();
            return distinct.Select(v => v.Value).ToArray();
        }

        private DateTime[] GetDistinctArray(params DateTime?[] values)
        {
            var distinct = values.Where(v => v.HasValue).Distinct().ToArray();
            return distinct.Select(v => v.Value).ToArray();
        }

        private string[] ToString(DateTime[] values)
        {
            return values
                .Select(dtm => dtm.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
                .ToArray();
        }
    }
}