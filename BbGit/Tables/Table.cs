using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
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
        private readonly CancellationToken cancellationToken;
        public TableTheme Theme { get; private set; }

        private Dictionary<string, Column> columnsByProperty;

        public bool HideZeros { get; set; }

        public Table(IConsole console, TableTheme theme = null, bool includeCount = false, CancellationToken? cancellationToken = null)
        {
            this.console = console;
            this.includeCount = includeCount;
            this.cancellationToken = cancellationToken ?? CancellationToken.None;
            Theme = theme ?? TableTheme.ColumnLines;
        }

        public Table OverrideColumn<TSource,TProperty>(
            IEnumerable<TSource> sourceRows, 
            Expression<Func<TSource, TProperty>> property, Column column)
        {
            return OverrideColumn(property.GetPropertyInfo().Name, column);
        }

        public Table OverrideColumn<TSource, TProperty>(
            IEnumerable<TSource> sourceRows,
            Expression<Func<TSource, TProperty>> property, Action<Column> modify)
        {
            var propertyInfo = property.GetPropertyInfo();
            var column = BuildColumn(propertyInfo.Name, propertyInfo.PropertyType);
            modify(column);
            return OverrideColumn(propertyInfo.Name, column);
        }


        private Table OverrideColumn(string name, Column column)
        {
            columnsByProperty ??= new Dictionary<string, Column>();
            columnsByProperty.Add(name, column);
            return this;
        }

        public void Write<T>(IEnumerable<T> rows)
        {
            if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
            {
                throw new Exception("Table.Write<T> does not support primitive types or strings.  Use Write(IEnumerable<IEnumerable<object>> rows)");
            }

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            Column GetOverriden(PropertyInfo property) => 
                columnsByProperty?.GetValueOrDefault(property.Name);

            if (this.Count == 0)
            {
                base.AddRange(properties.Select(p =>
                    GetOverriden(p) ?? BuildColumn(p.Name, p.PropertyType)));
            }
            
            Write(rows.Select(row => properties.Select(p => p.GetValue(row))));
        }

        private Column BuildColumn(string name, Type type)
        {
            // TODO: make bool align center and conversion to text be settings in the theme or table params
            return new Column(name)
            {
                WrapText = type == typeof(string),
                HAlign = type.IsNumeric()
                    ? HAlign.right
                    : type == typeof(bool)
                        ? HAlign.center
                        : HAlign.left,
                DisplayAs = HideZeros && type.IsNumeric()
                    ? o => o is 0 ? null : o.ToString()
                    : type == typeof(bool)
                        ? o => o is true ? "x" : null
                        : null
            };
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
                PrintHeaderRow(context, theme, theme.HeaderRow);
                PrintBorder(console, context, theme.HeaderSeparator);
            }
            
            int rowCounter = 0;
            foreach (var row in context.Rows)
            {
                rowCounter++;
                PrintDataRow(context, theme, theme.Rows, row);

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

        private void PrintHeaderRow(Context context, TableTheme theme, TableTheme.Level level)
        {
            var row = context.Columns
                .Select(c => new { column = c, lines = theme.Multiline 
                    ? c.Column.Name.ChunkLine(c.PrintWidth).ToArray()
                    : new []{c.Column.Name}
                })
                .ToArray();
            var maxLines = row.Max(h => h.lines.Length);

            PrintRow(context, level, row, maxLines, 
                (header, lineIndex) => header.lines.GetLineValue(lineIndex));
        }

        private void PrintDataRow(Context context, TableTheme theme, TableTheme.Level level, Cell[] row)
        {
            int maxLines = 1;
            foreach (var cell in row)
            {
                cell.AdjustWidth(context.Columns[cell.ColumnIndex], theme);
                maxLines = Math.Max(maxLines, cell.MultiLine?.Length ?? 1);
            }

            PrintRow(context, level, row, maxLines, 
                (cell, lineIndex) => cell.GetLineValue(lineIndex));
        }

        private void PrintRow<T>(Context context, TableTheme.Level level,
            T[] row, int maxLines, Func<T, int, string> getLineValue)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // TODO: markdown does not support multi-line cells
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

            var wrappableColumns = context.Columns
                .Where(c => c.Column.WrapText && c.PrintWidth > 6)
                .OrderBy(c => c.PrintWidth);

            var wrappableColumnsCount = wrappableColumns.Count();
            if (wrappableColumnsCount == 0)
            {
                return;
            }

            var availableWidth = wrappableColumns.Sum(c => c.PrintWidth);
            var percentToReduce = (double)overage / (double)availableWidth;
            if (availableWidth < overage)
            {
                wrappableColumns.ForEach(c => c.PrintWidth = Math.Min(10, c.PrintWidth));
                return;
            }


            // TODO: be smarter about how and when to wrap each column
            // TODO: repeat for to free columns that fit within the new width
            wrappableColumns.ForEach(c =>
            {
                var printWidth = percentToReduce * c.PrintWidth;
                c.PrintWidth = (int)Math.Floor(printWidth);
            });
        }

        private void AnalyzeColumnData(Context context)
        {
            foreach (var row in context.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (int i = 0; i < row.Length; i++)
                {
                    var columnInfo = context.Columns[i];
                    var cell = row[i];
                    columnInfo.Type ??= cell?.Value?.GetType();

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
                                           (context.Columns.Length - 1);
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
                    $"{Column.Name} ({Type}) {nameof(Column.MinWidth)}={Column.MinWidth} " +
                    $"{nameof(Column.MaxWidth)}={Column.MaxWidth} {nameof(PrintWidth)}={PrintWidth} " +
                    $"{nameof(MaxLineWidth)}={MaxLineWidth}";
            }
        }
    }
}