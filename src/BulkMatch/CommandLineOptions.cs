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

        [Option('n', "acs-instance-name", Required = true, HelpText = "Azure Cognitive Search instance name")]
        public string AcsInstanceName { get; set; }

        [Option('k', "acs-key", Required = true, HelpText = "Azure Cognitive Search admin key")]
        public string AcsAdminKey { get; set; }

        [Option('x', "acs-index-name", Required = true, HelpText = "Azure Cognitive Search index name")]
        public string AcsIndexName { get; set; }
    }
}