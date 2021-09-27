using System;
using System.Collections.Generic;

namespace BbGit.Framework
{
    internal static class DictionaryExtensions
    {
        internal static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dict,
            TKey key,
            Func<TKey, TValue> defaultProvider = null)
        {
            return dict.TryGetValue(key, out var value)
                ? value
                : defaultProvider == null
                    ? default(TValue)
                    : defaultProvider(key);
        }

        internal static TValue GetValueOrAdd<TKey, TValue>(
            this IDictionary<TKey, TValue> dict,
            TKey key,
            Func<TKey, TValue> valueProvider)
        {
            if (!dict.TryGetValue(key, out var value))
            {
                dict[key] = value = valueProvider(key);
            }

            return value;
        }
    }
}