using CommandLine;

namespace SeedFromLearningProviderFile
{
    class CommandLineOptions
    {
        [Option('s', "storage-connection-string", Required = true, HelpText = "Azure storage connection string")]
        public string StorageConnectionString { get; set; }

        [Option('e', "entities-table-name", Required = false, Default = "entities", HelpText = "Entities table name")]
        public string EntitiesTableName { get; set; }

        [Option('l', "links-table-name", Required = false, Default = "links", HelpText = "Links table name")]
        public string LinksTableName { get; set; }
    }
}