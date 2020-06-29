using System;
using System.Linq;

namespace Dfe.Spi.Registry.Domain
{
    public static class EntityNameTranslator
    {
        public const string LearningProviderSingular = "learning-provider";
        public const string LearningProviderPlural = "learning-providers";
        public const string ManagementGroupSingular = "management-group";
        public const string ManagementGroupPlural = "management-groups";
        
        private static EntityName[] EntityNames = new[]
        {
            new EntityName {SingularName = LearningProviderSingular, PluralName = LearningProviderPlural},
            new EntityName {SingularName = ManagementGroupSingular, PluralName = ManagementGroupPlural},
        };
        
        public static string Pluralise(string singularName)
        {
            var entityName = EntityNames.SingleOrDefault(x =>
                x.SingularName.Equals(singularName, StringComparison.InvariantCultureIgnoreCase) ||
                x.PluralName.Equals(singularName, StringComparison.InvariantCultureIgnoreCase));
            return entityName?.PluralName;
        }
        public static string Singularise(string pluralName)
        {
            var entityName = EntityNames.SingleOrDefault(x =>
                x.SingularName.Equals(pluralName, StringComparison.InvariantCultureIgnoreCase) ||
                x.PluralName.Equals(pluralName, StringComparison.InvariantCultureIgnoreCase));
            return entityName?.SingularName;
        }
        
        private class EntityName
        {
            public string SingularName { get; set; }
            public string PluralName { get; set; }
        }
    }
}