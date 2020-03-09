using CommandLine;

namespace SeedFromLearningProviderFile
{
    class CommandLineOptions
    {
        [Option('i', "input-path", Required = true, HelpText = "Path to learning provider file to load")]
        public string InputPath { get; set; }
        
        [Option('o', "origin-source", Required = true, HelpText = "Originating source of learning provider file")]
        public string OriginSource { get; set; }
        
        [Option('p', "bypass-matching", Default = false, HelpText = "Whether to bypass matching")]
        public bool BypassMatching { get; set; }

        [Option('s', "storage-connection-string", Required = true, HelpText = "Azure storage connection string")]
        public string StorageConnectionString { get; set; }

        [Option('e', "entities-table-name", Required = false, Default = "entities", HelpText = "Entities table name")]
        public string EntitiesTableName { get; set; }

        [Option('l', "links-table-name", Required = false, Default = "links", HelpText = "Links table name")]
        public string LinksTableName { get; set; }
    }
}