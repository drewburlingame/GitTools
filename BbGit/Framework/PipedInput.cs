using System;
using System.Collections.Generic;

namespace BbGit.Framework
{
    public class PipedInput
    {
        public bool HasValues { get; }
        public IEnumerable<string> Values { get; }

        /// <inheritdoc />
        private PipedInput(string[] lines)
        {
            this.Values = lines;
            this.HasValues = !lines.IsNullOrEmpty();
        }

        public static PipedInput GetPipedInput()
        {
            if (Console.IsInputRedirected)
            {
                var input = Console.In.ReadToEnd()
                    .Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
                return new PipedInput(input);
            }

            return new PipedInput(null);
        }
    }
}