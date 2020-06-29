using System;

namespace Dfe.Spi.Registry.Functions.UnitTests
{
    public static class ObjectAssert
    {
        public static bool AreEqual<T>(T expected, T actual)
        {
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                var expectedValue = property.GetValue(expected);
                var actualValue = property.GetValue(actual);

                if (property.PropertyType.IsClass)
                {
                    var objectsAreEqual = AreEqual(expectedValue, actualValue);
                    if (!objectsAreEqual)
                    {
                        return false;
                    }

                    continue;
                }

                if (expectedValue == null && actualValue != null ||
                    expectedValue != null && actualValue == null)
                {
                    return false;
                }

                if (expectedValue != null && !expectedValue.Equals(actualValue))
                {
                    return false;
                }
            }

            return true;
        }
    }
}