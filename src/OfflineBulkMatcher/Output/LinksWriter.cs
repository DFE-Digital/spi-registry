using System;
using System.IO;

namespace OfflineBulkMatcher.Output
{
    internal class LinksWriter : IDisposable
    {
        private readonly TextWriter _writer;

        public LinksWriter(string path)
        {
            _writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write));

            _writer.WriteLine(
                "PartitionKey,RowKey,LinkId,LinkType,EntityType,EntitySourceSystemName,EntitySourceSystemId,CreatedBy,CreatedReason,CreatedAt");
        }


        public void WriteLink(Link link)
        {
            var partitionKey = $"{link.Type}:{link.Id}".ToLower();
            var createdAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            var createdBy = "BulkMatcher";
            var createdReason = "Bulk match on URN/UKPRN/ManagementGroupCode";
            foreach (var pointer in link.Contents)
            {
                var rowKey =
                    $"{pointer.EntityType.ToLower()}:{pointer.SourceSystemName.ToUpper()}:{pointer.SourceSystemId.ToLower()}";
                WriteRow(
                    partitionKey,
                    rowKey,
                    link.Id,
                    link.Type,
                    pointer.EntityType,
                    pointer.SourceSystemName,
                    pointer.SourceSystemId,
                    createdBy,
                    createdReason,
                    createdAt);
            }
        }


        private void WriteRow(string partitionKey, string rowKey, string linkId, string linkType,
            string entityType, string sourceSystemName, string sourceSystemId,
            string createdBy, string createdReason, string createdAt)
        {
            _writer.Write($"\"{partitionKey}\",\"{rowKey}\",\"{linkId}\",\"{linkType}\",");
            _writer.Write($"\"{entityType}\",\"{sourceSystemName}\",\"{sourceSystemId}\",");
            _writer.WriteLine($"\"{createdBy}\",\"{createdReason}\",\"{createdAt}\"");
            _writer.Flush();
        }


        public void Dispose()
        {
            _writer?.Dispose();
        }
    }
}