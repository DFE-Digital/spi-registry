using System;

namespace Dfe.Spi.Registry.Domain.Search
{
    public class SearchFieldTypeAttribute : Attribute
    {
        public SearchFieldType FieldType { get; }

        public SearchFieldTypeAttribute(SearchFieldType fieldType)
        {
            FieldType = fieldType;
        }
    }
}