using CommandLine;

namespace PopulateRegistryFromGiasEstablishments
{
    class CommandLineOptions
    {
        [Option('p', "establishments-path", HelpText = "Path to Establishments file to load into index")]
        public string EstablishmentsFilePath { get; set; }

        [Option('s', "storage-connection-string", Required = true, HelpText = "Azure storage connection string")]
        public string StorageConnectionString { get; set; }

        [Option('e', "entities-table-name", Required = false, Default = "entities", HelpText = "Entities table name")]
        public string EntitiesTableName { get; set; }

        [Option('l', "links-table-name", Required = false, Default = "links", HelpText = "Links table name")]
        public string LinksTableName { get; set; }
    }
}