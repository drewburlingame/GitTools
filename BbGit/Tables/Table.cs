using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BbGit.Framework;
using CommandDotNet;
using CommandDotNet.Rendering;
using MoreLinq;

namespace BbGit.Tables
{
    public class Table : List<Column>
    {
        private readonly IConsole console;
        private readonly bool includeCount;
        public TableTheme Theme { get; }

        private Dictionary<string, Column> columnsByProperty;

        public Table(IConsole console, TableTheme theme = null, bool includeCount = false)
        {
            this.console = console;
            this.includeCount = includeCount;
            Theme = theme ?? TableTheme.ColumnBorders;
        }

        public Table OverrideColumn<TSource,TProperty>(
            IEnumerable<TSource> sourceRows, 
            Expression<Func<TSource, TProperty>> property, Column column)
        {
            columnsByProperty ??= new Dictionary<string, Column>();
            columnsByProperty.Add(property.GetPropertyInfo().Name, column);
            return this;
        }
        
        public void Write<T>(IEnumerable<T> rows)
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            Column GetOverriden(PropertyInfo property) => 
                columnsByProperty?.GetValueOrDefault(property.Name);

            if (this.Count == 0)
            {
                base.AddRange(properties.Select(p =>
                {
                    // TODO: make bool align center and conversion to text be settings in the theme or table params
                    return GetOverriden(p)
                           ?? new Column(p.Name)
                           {
                               WrapText = p.PropertyType == typeof(string),
                               HAlign = p.PropertyType.IsNumeric()
                                   ? HAlign.right
                                   : p.PropertyType == typeof(bool)
                                       ? HAlign.center
                                       : HAlign.left,
                               DisplayAs = p.PropertyType == typeof(bool)
                                   ? o => o is true ? "x" : null
                                   : null
                           };
                }));
            }
            
            Write(rows.Select(row => properties.Select(p => p.GetValue(row))));
        }

        public void Write(IEnumerable<IEnumerable<object>> rows)
        {
            var context = new Context(this, rows);
         
            AnalyzeColumnData(context);
            AdjustColumnWidths(context);
            
            var theme = context.Table.Theme;

            PrintBorder(console, context, theme.Top);
            if (theme.PrintHeader && context.HasHeaders)
            {
                PrintHeaderRow(console, context, theme, theme.HeaderRow);
                PrintBorder(console, context, theme.HeaderSeparator);
            }
            
            int rowCounter = 0;
            foreach (var row in context.Rows)
            {
                rowCounter++;
                PrintDataRow(console, context, theme, theme.Rows, row);

                if (rowCounter != context.Rows.Count)
                {
                    // todo: option to print separator every N rows
                    PrintBorder(console, context, theme.RowSeparator);
                }
            }

            PrintBorder(console, context, theme.Bottom);

            if (includeCount)
            {
                console.WriteLine();
                console.WriteLine($"count: {rowCounter}");
            }
        }

        private static void PrintHeaderRow(IConsole console, Context context, TableTheme theme, TableTheme.Level level)
        {
            var row = context.Columns
                .Select(c => new { column = c, lines = theme.Multiline 
                    ? c.Column.Name.ChunkLine(c.PrintWidth).ToArray()
                    : new []{c.Column.Name}
                })
                .ToArray();
            var maxLines = row.Max(h => h.lines.Length);

            PrintRow(console, context, level, row, maxLines, 
                (header, lineIndex) => header.lines.GetLineValue(lineIndex));
        }

        private static void PrintDataRow(IConsole console, Context context, 
            TableTheme theme, TableTheme.Level level, Cell[] row)
        {
            int maxLines = 1;
            foreach (var cell in row)
            {
                cell.AdjustWidth(context.Columns[cell.ColumnIndex], theme);
                maxLines = Math.Max(maxLines, cell.MultiLine?.Length ?? 1);
            }

            PrintRow(console, context, level, row, maxLines, 
                (cell, lineIndex) => cell.GetLineValue(lineIndex));
        }

        private static void PrintRow<T>(
            IConsole console, Context context, TableTheme.Level level,
            T[] row, int maxLines, Func<T, int, string> getLineValue)
        {
            // TODO: markdown doesnt support multi-line cells
            for (int l = 0; l < maxLines; l++)
            {
                console.Write(level.Left);
                console.Write(level.Filler);

                for (int c = 0; c < row.Length; c++)
                {
                    var cell = row[c];
                    var column = context.Columns[c];

                    var lineValue = getLineValue(cell, l);
                    console.Write(lineValue.Justify(column.Column.HAlign, column.PrintWidth));

                    if (!row.IsLast(c))
                    {
                        console.Write(level.Filler);
                        console.Write(level.Delimiter);
                        console.Write(level.Filler);
                    }
                }

                console.Write(level.Filler);
                console.WriteLine(level.Right);
            }
        }


        private static void PrintBorder(IConsole console, Context context, TableTheme.Level level)
        {
            if (level is null) return;

            // TODO: verify null can be written
            console.Write(level.Left);
            console.Write(level.Filler);
            for (int i = 0; i < context.Columns.Length; i++)
            {
                var column = context.Columns[i];
                console.Write(new string(level.Filler, column.PrintWidth));
                if (!context.Columns.IsLast(i))
                {
                    console.Write(level.Filler);
                    console.Write(level.Delimiter);
                    console.Write(level.Filler);
                }
            }

            console.Write(level.Filler);
            console.WriteLine(level.Right);
        }

        private static void AdjustColumnWidths(Context context)
        {
            var overage = context.RequiredConsoleWidth - context.ConsoleWidth;
            if (overage < 1)
            {
                return;
            }

            var wrappableColumns = context.Columns.Where(c => c.Column.WrapText);
            var wrappableColumnsCount = wrappableColumns.Count();
            if (wrappableColumnsCount == 0)
            {
                return;
            }

            // TODO: be smarter about how and when to wrap each column
            // TODO: repeat for to free columns that fit within the new width
            var wrappableWidth = overage / wrappableColumnsCount;

            var free = wrappableColumns
                .Where(c => c.PrintWidth <= wrappableWidth)
                .Sum(c => wrappableWidth - c.PrintWidth);

            wrappableColumns = wrappableColumns
                .Where(c => c.PrintWidth > wrappableWidth);
            wrappableColumnsCount = wrappableColumns.Count();
            wrappableWidth = wrappableWidth + (free / wrappableColumnsCount);

            wrappableColumns.ForEach(c => c.PrintWidth = wrappableWidth);
        }

        private static void AnalyzeColumnData(Context context)
        {
            foreach (var row in context.Rows)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    var columnInfo = context.Columns[i];
                    var cell = row[i];
                    columnInfo.Type ??= cell?.GetType();

                    if (columnInfo.Column.DisplayAs is not null)
                    {
                        cell.Value = columnInfo.Column.DisplayAs(cell.Value);
                    }

                    columnInfo.MaxLineWidth = Math.Max(columnInfo.MaxLineWidth, cell.Width);
                }
            }

            foreach (var column in context.Columns)
            {
                var maxAllowedWidth = column.Column.MaxWidth ?? int.MaxValue;
                column.PrintWidth = Math.Min(maxAllowedWidth,
                    Math.Max(column.Column.Name.Length, column.MaxLineWidth));
            }

            context.RequiredConsoleWidth = context.Columns.Sum(c => c.PrintWidth) + 
                                            context.Table.Theme.Width(context.Columns.Length) +
                                            (context.Columns.Length-1);
        }

        private class Context
        {
            public Table Table { get; }
            public ICollection<Cell[]> Rows { get; }

            public ColumnInfo[] Columns { get; }
            public int ConsoleWidth { get; } = Console.LargestWindowWidth;
            public int RequiredConsoleWidth { get; set; }

            public bool HasHeaders => Columns.Any(c => !c.Column.Name.IsNullOrEmpty());

            public Context(Table table, IEnumerable<IEnumerable<object>> rows)
            {
                Table = table;
                Columns = table.Select(c => new ColumnInfo(c)).ToArray();
                Rows = rows
                    .Select(row => row.Select((cell, i) => new Cell(cell, i)).ToArray())
                    .ToCollection();
            }
        }

        private class Cell
        {
            private static readonly char[] NewLines = { '\n', '\r' };

            private object value;

            public object Value
            {
                get => value;
                set
                {
                    this.value = value;
                    if (this.value is string text)
                    {
                        MultiLine = text.Split(NewLines, StringSplitOptions.RemoveEmptyEntries);
                        Width = MultiLine.Any() ? MultiLine.Max(l => l.Length) : 0;
                    }
                    else
                    {
                        MultiLine = null;
                        Width = this.value?.ToString()?.Length ?? 0;
                    }
                }
            }
            
            public string[] MultiLine { get; private set; }
            public int Width { get; private set; }
            public int ColumnIndex { get; }

            public Cell(object value, int columnIndex)
            {
                Value = value;
                ColumnIndex = columnIndex;
            }

            public string GetLineValue(int lineIndex) => MultiLine?.GetLineValue(lineIndex) 
                                                     ?? (lineIndex == 0 ? Value?.ToString() : null);

            public void AdjustWidth(ColumnInfo columnInfo, TableTheme theme)
            {
                var printWidth = Math.Min(columnInfo.PrintWidth, columnInfo.Column.MaxWidth ?? int.MaxValue);

                if (Width <= printWidth) return;

                if (theme.Multiline)
                {
                    MultiLine = MultiLine is null
                        ? Value.ToString().ChunkLine(printWidth).ToArray()
                        : MultiLine.SelectMany(l => l.ChunkLine(printWidth)).ToArray();
                }
            }

            public override string ToString()
            {
                return $"Cell: {ColumnIndex} {Value}";
            }
        }

        private class ColumnInfo
        {
            public Column Column { get; }
            public int MaxLineWidth { get; set; }
            public int PrintWidth { get; set; }
            public Type Type { get; set; }

            public ColumnInfo(Column column)
            {
                Column = column;
            }

            public override string ToString()
            {
                return
                    $"{Column.Name} {nameof(Column.MaxWidth)}={Type} {nameof(Column.MaxWidth)}={Column.MaxWidth} {nameof(PrintWidth)}={PrintWidth} {nameof(MaxLineWidth)}={MaxLineWidth}";
            }
        }
    }
}