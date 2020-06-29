using NUnit.Framework;

namespace Dfe.Spi.Registry.Domain.UnitTests.EntityNameTranslatorTests
{
    public class WhenPluralisingAnEntityName
    {
        [TestCase("learning-provider", "learning-providers")]
        [TestCase("LEARNING-PROVIDER", "learning-providers")]
        [TestCase("LEaRNiNG-PrOViDeR", "learning-providers")]
        [TestCase("management-group", "management-groups")]
        [TestCase("MANAGEMENT-GROUP", "management-groups")]
        [TestCase("mANAgEMENT-GrOUp", "management-groups")]
        public void ThenItShouldReturnThePluralNameForAValidSingularName(string singularName, string expectedPluralName)
        {
            var actual = EntityNameTranslator.Pluralise(singularName);
            
            Assert.AreEqual(expectedPluralName, actual);
        }
        
        [TestCase("learning-providers", "learning-providers")]
        [TestCase("LEARNING-PROVIDERS", "learning-providers")]
        [TestCase("leaRNinG-pRovIders", "learning-providers")]
        [TestCase("management-groups", "management-groups")]
        [TestCase("MANAGEMENT-GROUPS", "management-groups")]
        [TestCase("ManAGement-grOups", "management-groups")]
        public void ThenItShouldReturnThePluralNameForAValidPluralName(string singularName, string expectedPluralName)
        {
            var actual = EntityNameTranslator.Pluralise(singularName);
            
            Assert.AreEqual(expectedPluralName, actual);
        }
        
        [TestCase("establishment")]
        [TestCase("provider")]
        [TestCase("authority")]
        public void ThenItShouldReturnNullForInvalidName(string singularName)
        {
            var actual = EntityNameTranslator.Pluralise(singularName);
            
            Assert.IsNull(actual);
        }
    }
}