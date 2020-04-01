using System;
using Dfe.Spi.Common.WellKnownIdentifiers;

namespace Dfe.Spi.Registry.Functions
{
    internal static class ValidationHelpers
    {
        public static string GetValidSourceSystemName(string value)
        {
            if (value.Equals(SourceSystemNames.GetInformationAboutSchools, StringComparison.InvariantCultureIgnoreCase))
            {
                return SourceSystemNames.GetInformationAboutSchools;
            }
            if (value.Equals(SourceSystemNames.UkRegisterOfLearningProviders, StringComparison.InvariantCultureIgnoreCase))
            {
                return SourceSystemNames.UkRegisterOfLearningProviders;
            }

            return null;
        }
    }
}