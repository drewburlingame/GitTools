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
            /// <summary>column lines</summary>
            c,
            /// <summary>row lines</summary>
            r,
            /// <summary>grid lines</summary>
            g,
            /// <summary>markdown</summary>
            m
        }
 
        [Option(
            ShortName = "t", 
            Description = "  c: column lines\n" +
                          "    4: row lines\n" +
                          "    g: grid lines\n" +
                          "    s: header separator\n" +
                          "    m: markdown")]
        public TableFormat Table { get; set; } = TableFormat.c;

        public TableTheme GetTheme()
        {
            return Table switch
            {
                TableFormat.c => TableTheme.ColumnLines,
                TableFormat.r => TableTheme.RowLines,
                TableFormat.g => TableTheme.Grid,
                TableFormat.s => TableTheme.Borderless,
                TableFormat.m => TableTheme.Markdown,
                _ => throw new ArgumentOutOfRangeException($"unknown TableFormat: {Table}")
            };
        }
    }
}