using System;
using System.Collections.Generic;
using System.Text;

namespace BbGit.Tables
{
    public static class StringExtensions
    {
        public static string GetLineValue(this string[] lines, int index) =>
            lines.Length > index ? lines[index] : null;

        public static IEnumerable<string> ChunkLine(this string stringToSplit, int maxLineLength)
        {
            https://stackoverflow.com/a/22370087/169336

            // TODO: remove empty entries?
            string[] words = stringToSplit.Split(' ');
            StringBuilder line = new StringBuilder();
            foreach (string word in words)
            {
                if (word.Length + line.Length <= maxLineLength)
                {
                    line.Append(word + " ");
                }
                else
                {
                    if (line.Length > 0)
                    {
                        yield return line.ToString().Trim();
                        line.Clear();
                    }
                    string overflow = word;
                    while (overflow.Length > maxLineLength)
                    {
                        // TODO: look for punctuation to break on
                        yield return overflow.Substring(0, maxLineLength);
                        overflow = overflow.Substring(maxLineLength);
                    }
                    line.Append(overflow + " ");
                }
            }
            yield return line.ToString().Trim();
        }

        public static string Justify(this string value, HAlign align, int width)
        {
            value ??= "";
            switch (align)
            {
                case HAlign.left:
                    value = value.PadRight(width);
                    break;
                case HAlign.right:
                    value = value.PadLeft(width);
                    break;
                case HAlign.center:
                    var half = width / 2;
                    value = value.PadLeft(half / 2 + half);
                    value = value.PadRight(width);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return value;
        }
    }
}