using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BbGit.ConsoleUtils;
using BbGit.Git;
using Colorful;
using MoreLinq;
using Console = Colorful.Console;

namespace BbGit.Framework
{
    internal static class EnumerableExtensions
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            return enumerable == null || !enumerable.Any();
        }

        public static DisposableColleciton<T> ToDisposableColleciton<T>(this IEnumerable<T> items) where T : IDisposable
        {
            var collection = items as ICollection<T> ?? items.ToList();
            return new DisposableColleciton<T>(collection);
        }

        public static T SafeFromIndex<T>(this IList<T> items, int index)
        {
            return items.Count > index ? items[index] : default(T);
        }

        public static T SafeFromIndex<T>(this T[] items, int index)
        {
            return items.Length > index ? items[index] : default(T);
        }

        public static void SafelyForEach(this IEnumerable<RemoteRepo> values, Action<RemoteRepo> action, int allowedErrorsInARow = 2, bool summarizeErrors = false)
        {
            values.SafelyForEach(r => r.Name, action, allowedErrorsInARow, summarizeErrors);
        }

        public static void SafelyForEach(this IEnumerable<LocalRepo> values, Action<LocalRepo> action, int allowedErrorsInARow = 2, bool summarizeErrors = false)
        {
            values.SafelyForEach(r => r.Name, action, allowedErrorsInARow, summarizeErrors);
        }

        public static void SafelyForEach<T>(this IEnumerable<T> values, Func<T, string> getName, Action<T> action, int allowedErrorsInARow = 2, bool summarizeErrors = false)
        {
            var items = values as ICollection<T> ?? values.ToList();

            var errors = new List<(T item, int index, Exception ex)>();

            var errorsInARow = 0;
            items.ForEach((r, i) =>
            {
                try
                {
                    action(r);
                    errorsInARow = 0;
                }
                catch (Exception e)
                {
                    errorsInARow++;

                    if (errorsInARow > allowedErrorsInARow)
                    {
                        throw;
                    }

                    e.Print();

                    errors.Add((r, i + 1, e));
                }

                Console.WriteLineFormatted(
                    $"{i + 1} of {items.Count} - " + "{0}",
                    new Formatter(getName(r), Colors.BranchColor),
                    Colors.DefaultColor);
            });

            if (summarizeErrors && errors.Count > 0)
            {
                Console.Out.WriteLine("");
                Colorful.Console.WriteLine($"The following {errors.Count} of {items.Count} operations failed", Color.Red);
                Console.Out.WriteLine("");

                errors.ForEach(e => Colorful.Console.WriteLineFormatted(
                    "{0} of {1}: {2} > {3}",
                    new Formatter(e.index, Colors.DefaultColor),
                    new Formatter(items.Count, Colors.DefaultColor),
                    new Formatter(e.item, Colors.RepoColor),
                    new Formatter(e.ex.Message, Color.Red),
                    Colors.DefaultColor));
            }
        }

        internal static HashSet<T> ToSet<T>(this IEnumerable<T> enumerable)
        {
            return new HashSet<T>(enumerable);
        }

        internal static string ToCsv<T>(this IEnumerable<T> values, string separator = ",")
        {
            return string.Join(separator, values);
        }

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