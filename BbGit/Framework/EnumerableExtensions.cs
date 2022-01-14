using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BbGit.ConsoleUtils;
using BbGit.Git;
using MoreLinq;

namespace BbGit.Framework
{
    internal static class EnumerableExtensions
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T>? enumerable)
        {
            return enumerable is null || !enumerable.Any();
        }

        public static bool IsLast<T>(this ICollection<T> collection, int index) => index == collection.Count - 1;

        public static ICollection<T> ToCollection<T>(this IEnumerable<T> enumerable)
        {
            return enumerable is ICollection<T> coll ? coll : enumerable.ToList();
        }

        public static bool IsIn<T>(this T item, params T[] collection) => collection.Contains(item);
        public static bool IsIn<T>(this T item, IEnumerable<T> collection) => collection.Contains(item);

        public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> items, bool condition, Func<T, bool> filter) =>
            condition ? items.Where(filter) : items;

        public static IEnumerable<T> WhereMatches<T>(this IEnumerable<T> items, Func<T, string?> getValue, 
            string? regexPattern, 
            RegexOptions regexOptions = RegexOptions.Compiled)
        {
            return string.IsNullOrEmpty(regexPattern)
                ? items
                : items.WhereMatches(getValue, new Regex(regexPattern, regexOptions));
        }

        public static IEnumerable<T> WhereMatches<T>(this IEnumerable<T> items, Func<T, string?> getValue, Regex? regex)
            => items.Where(t =>
            {
                if (regex is null) return true;

                var value = getValue(t);
                return value is not null && regex.IsMatch(value);
            });

        public static DisposableCollection<T> ToDisposableCollection<T>(this IEnumerable<T> items) where T : IDisposable 
            => new(items.ToCollection());

        public static async Task<IEnumerable<T1>> SelectManyAsync<T, T1>(this IEnumerable<T> enumeration, Func<T, Task<IEnumerable<T1>>> func)
        {
            // TODO: throttle requests in batches
            return (await Task.WhenAll(enumeration.Select(func))).SelectMany(s => s);
        }

        public static void SafelyForEach(
            this IEnumerable<RemoteRepo> values,
            Action<RemoteRepo> action, CancellationToken cancellationToken,
            int allowedErrorsInARow = 2,
            bool summarizeErrors = false)
        {
            values.SafelyForEach(r => r.Description, action, cancellationToken, allowedErrorsInARow, summarizeErrors);
        }

        public static void SafelyForEach(
            this IEnumerable<LocalRepo> values,
            Action<LocalRepo> action, CancellationToken cancellationToken,
            int allowedErrorsInARow = 2,
            bool summarizeErrors = false)
        {
            values.SafelyForEach(r => r.Name, action, cancellationToken, allowedErrorsInARow, summarizeErrors);
        }

        public static void SafelyForEach<T>(
            this IEnumerable<T> values,
            Func<T, string> getName,
            Action<T> action, CancellationToken cancellationToken,
            int allowedErrorsInARow = 2,
            bool summarizeErrors = false)
        {
            var items = values as ICollection<T> ?? values.ToList();

            var errors = new List<(T repoName, int index, Exception ex)>();

            var errorsInARow = 0;
            items.ForEach((repoName, index) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                Console.WriteLine($"{index + 1} of {items.Count} - {getName(repoName).ColorBranch()}".ColorDefault());

                try
                {
                    action(repoName);
                    errorsInARow = 0;
                }

                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    errorsInARow++;

                    if (errorsInARow > allowedErrorsInARow)
                    {
                        throw;
                    }

                    e.Print();

                    errors.Add((repoName, index + 1, e));
                }

            });

            if (summarizeErrors && errors.Count > 0)
            {
                Console.Out.WriteLine("");
                Console.WriteLine($"The following {errors.Count} of {items.Count} operations failed".ColorError());
                Console.Out.WriteLine("");

                errors.ForEach(e => Console.WriteLine(
                    $"{e.index} of {items.Count}: {e.repoName!.ColorRepo()} > {e.ex.Message.ColorError()}".ColorDefault()));
            }
        }

        internal static string ToCsv<T>(this IEnumerable<T> values, string separator = ",")
        {
            return string.Join(separator, values);
        }
    }
}