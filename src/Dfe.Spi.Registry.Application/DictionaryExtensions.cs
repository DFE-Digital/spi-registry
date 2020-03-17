using System;
using System.Collections.Generic;

namespace Dfe.Spi.Registry.Application
{
    public static class DictionaryExtensions
    {
        public static TValue GetValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key,
            TValue defaultValue = default)
        {
            if (dictionary == null || !dictionary.ContainsKey(key))
            {
                return defaultValue;
            }

            return dictionary[key];
        }

        public static DateTime? GetValueAsDateTime<TKey>(this Dictionary<TKey, string> dictionary, TKey key)
        {
            var s = dictionary.GetValue(key);
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }

            return DateTime.Parse(s);
        }

        public static long? GetValueAsLong<TKey>(this Dictionary<TKey, string> dictionary, TKey key)
        {
            var s = dictionary.GetValue(key);
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }

            return long.Parse(s);
        }
    }
}