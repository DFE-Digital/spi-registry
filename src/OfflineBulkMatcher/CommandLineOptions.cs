using CommandLine;

namespace OfflineBulkMatcher
{
    public class CommandLineOptions
    {
        [Option("gias-provider-file", Required = true, HelpText = "Path to GIAS learning provider file")]
        public string GiasLearningProviderFilePath { get; set; }
        
        [Option("ukrlp-provider-file", Required = true, HelpText = "Path to UKRLP learning provider file")]
        public string UkrlpLearningProviderFilePath { get; set; }
        
        [Option("gias-mgmtgrp-file", Required = true, HelpText = "Path to GIAS management group file")]
        public string GiasManagementGroupFilePath { get; set; }
        
        [Option("output-dir", Required = true, HelpText = "Output directory")]
        public string OutputDirectory { get; set; }
        
        [Option("threads", Required = false, Default = 4, HelpText = "Number of threads to use to match")]
        public int Threads { get; set; }
    }
}