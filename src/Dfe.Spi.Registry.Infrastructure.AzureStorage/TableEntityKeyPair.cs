namespace Dfe.Spi.Registry.Infrastructure.AzureStorage
{
    internal class TableEntityKeyPair
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
    }
}