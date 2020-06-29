using NUnit.Framework;

namespace Dfe.Spi.Registry.Domain.UnitTests.EntityNameTranslatorTests
{
    public class WhenSingularisingAnEntityName
    {
        [TestCase("learning-providers", "learning-provider")]
        [TestCase("LEARNING-PROVIDERS", "learning-provider")]
        [TestCase("leaRNinG-pRovIders", "learning-provider")]
        [TestCase("management-groups", "management-group")]
        [TestCase("MANAGEMENT-GROUPS", "management-group")]
        [TestCase("ManAGement-grOups", "management-group")]
        public void ThenItShouldReturnTheSingularNameForAValidPluralName(string singularName, string expectedPluralName)
        {
            var actual = EntityNameTranslator.Singularise(singularName);
            
            Assert.AreEqual(expectedPluralName, actual);
        }
        
        [TestCase("learning-provider", "learning-provider")]
        [TestCase("LEARNING-PROVIDER", "learning-provider")]
        [TestCase("LEaRNiNG-PrOViDeR", "learning-provider")]
        [TestCase("management-group", "management-group")]
        [TestCase("MANAGEMENT-GROUP", "management-group")]
        [TestCase("mANAgEMENT-GrOUp", "management-group")]
        public void ThenItShouldReturnTheSingularNameForAValidSingularName(string singularName, string expectedPluralName)
        {
            var actual = EntityNameTranslator.Singularise(singularName);
            
            Assert.AreEqual(expectedPluralName, actual);
        }
        
        [TestCase("establishment")]
        [TestCase("provider")]
        [TestCase("authority")]
        public void ThenItShouldReturnNullForInvalidName(string singularName)
        {
            var actual = EntityNameTranslator.Singularise(singularName);
            
            Assert.IsNull(actual);
        }
    }
}