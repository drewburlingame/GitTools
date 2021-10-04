using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        public static bool IsLast<T>(this ICollection<T> collection, int index) => index == collection.Count - 1;

        public static ICollection<T> ToCollection<T>(this IEnumerable<T> enumerable)
        {
            return enumerable is ICollection<T> coll ? coll : enumerable.ToList();
        }

        public static IEnumerable<T> WhereMatches<T>(this IEnumerable<T> items, Func<T, string> getValue, string? regexPattern)
            => string.IsNullOrEmpty(regexPattern)
                ? items 
                : items.WhereMatches(getValue, new Regex(regexPattern));

        public static IEnumerable<T> WhereMatches<T>(this IEnumerable<T> items, Func<T, string> getValue, Regex? regex)
            => items.Where(t =>
            {
                if (regex is null) return true;

                var value = getValue(t);
                return value is not null && regex.IsMatch(value);
            });

        public static DisposableCollection<T> ToDisposableCollection<T>(this IEnumerable<T> items) where T : IDisposable
        {
            var collection = items as ICollection<T> ?? items.ToList();
            return new DisposableCollection<T>(collection);
        }

        public static async Task<IEnumerable<T1>> SelectManyAsync<T, T1>(this IEnumerable<T> enumeration, Func<T, Task<IEnumerable<T1>>> func)
        {
            // TODO: throttle requests in batches
            return (await Task.WhenAll(enumeration.Select(func))).SelectMany(s => s);
        }

        public static void SafelyForEach(
            this IEnumerable<RemoteRepo> values,
            Action<RemoteRepo> action,
            int allowedErrorsInARow = 2,
            bool summarizeErrors = false)
        {
            values.SafelyForEach(r => r.Description, action, allowedErrorsInARow, summarizeErrors);
        }

        public static void SafelyForEach(
            this IEnumerable<LocalRepo> values,
            Action<LocalRepo> action,
            int allowedErrorsInARow = 2,
            bool summarizeErrors = false)
        {
            values.SafelyForEach(r => r.Name, action, allowedErrorsInARow, summarizeErrors);
        }

        public static void SafelyForEach<T>(
            this IEnumerable<T> values,
            Func<T, string> getName,
            Action<T> action,
            int allowedErrorsInARow = 2,
            bool summarizeErrors = false)
        {
            var items = values as ICollection<T> ?? values.ToList();

            var errors = new List<(T item, int index, Exception ex)>();

            var errorsInARow = 0;
            items.ForEach((r, i) =>
            {
                Console.WriteLineFormatted(
                    $"{i + 1} of {items.Count} - " + "{0}",
                    new Formatter(getName(r), Colors.BranchColor),
                    Colors.DefaultColor);

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

            });

            if (summarizeErrors && errors.Count > 0)
            {
                Console.Out.WriteLine("");
                Console.WriteLine($"The following {errors.Count} of {items.Count} operations failed",
                    Color.Red);
                Console.Out.WriteLine("");

                errors.ForEach(e => Console.WriteLineFormatted(
                    "{0} of {1}: {2} > {3}",
                    new Formatter(e.index, Colors.DefaultColor),
                    new Formatter(items.Count, Colors.DefaultColor),
                    new Formatter(e.item, Colors.RepoColor),
                    new Formatter(e.ex.Message, Color.Red),
                    Colors.DefaultColor));
            }
        }

        internal static string ToCsv<T>(this IEnumerable<T> values, string separator = ",")
        {
            return string.Join(separator, values);
        }
    }
}