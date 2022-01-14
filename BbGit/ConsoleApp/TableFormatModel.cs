using System;
using CommandDotNet;
using Spectre.Console;

namespace BbGit.ConsoleApp
{
    public class TableFormatModel : IArgumentModel
    {
        public enum TableFormat { c, b, h, m }
 
        [Option('t', 
            Description = "  b: borders\n" +
                          "    c: columns\n" +
                          "    h: header separator\n" +
                          "    m: markdown")]
        public TableFormat Table { get; set; } = TableFormat.b;

        public TableBorder GetTheme()
        {
            return Table switch
            {
                TableFormat.b => TableBorder.Rounded,
                TableFormat.c => TableBorder.Minimal,
                TableFormat.h => TableBorder.Simple,
                TableFormat.m => TableBorder.Markdown,
                _ => throw new ArgumentOutOfRangeException($"unknown TableFormat: {Table}")
            };
        }
    }
}