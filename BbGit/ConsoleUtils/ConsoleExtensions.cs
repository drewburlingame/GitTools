using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BbGit.ConsoleUtils
{
    public static class ConsoleExtensions
    {
        public static void WriteLines<T>(this TextWriter console, IEnumerable<T> items)
        {
            foreach (var item in items.Where(i => i != null))
            {
                console.WriteLine(item);
            }
        }

        public static async Task WriteLinesAsync<T>(this TextWriter console, IEnumerable<T> items)
        {
            foreach (var item in items.Where(i => i != null))
            {
                await console.WriteLineAsync(item.ToString());
            }
        }
    }
}