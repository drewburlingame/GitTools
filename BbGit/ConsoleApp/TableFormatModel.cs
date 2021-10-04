using System;
using BbGit.Tables;
using CommandDotNet;

namespace BbGit.ConsoleApp
{
    public class TableFormatModel : IArgumentModel
    {
        public enum TableFormat
        {
            /// <summary>simple</summary>
            s,
            /// <summary>column borders</summary>
            c,
            /// <summary>grid lines</summary>
            g,
            /// <summary>markdown</summary>
            m
        }
 
        [Option(
            ShortName = "t", 
            Description = "  c: column borders\n" +
                          "    g: grid lines\n" +
                          "    s: header separator\n" +
                          "    m: markdown")]
        public TableFormat Table { get; set; } = TableFormat.c;

        public TableTheme GetTheme()
        {
            return Table switch
            {
                TableFormat.c => TableTheme.ColumnBorders,
                TableFormat.g => TableTheme.Grid,
                TableFormat.s => TableTheme.Borderless,
                TableFormat.m => TableTheme.Markdown,
                _ => throw new ArgumentOutOfRangeException($"unknown TableFormat: {Table}")
            };
        }
    }
}