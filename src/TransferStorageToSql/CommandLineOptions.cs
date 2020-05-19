using CommandLine;

namespace TransferStorageToSql
{
    public class CommandLineOptions
    {
        [Option("storage-connection-string", Required = true, HelpText = "Connection for source Azure Storage")]
        public string StorageConnectionString { get; set; }
        
        [Option("sql-connection-string", Required = true, HelpText = "Connection for destination SQL Server")]
        public string SqlConnectionString { get; set; }
    }
}