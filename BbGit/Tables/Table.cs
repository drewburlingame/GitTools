using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Spectre.Console;
using SpectreTable=Spectre.Console.Table;

namespace BbGit.Tables
{
    public class Table : List<Column>
    {
        private readonly IAnsiConsole _console;
        private readonly bool _includeCount;
        private readonly TableBorder _tableBorder;

        private Dictionary<string, Column>? _columnsByProperty;

        public bool HideZeros { get; set; }

        public Table(IAnsiConsole console, TableBorder? theme = null, bool includeCount = false)
        {
            _console = console;
            _includeCount = includeCount;
            _tableBorder = theme ?? TableBorder.Rounded;
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
            _columnsByProperty ??= new Dictionary<string, Column>();
            _columnsByProperty.Add(name, column);
            return this;
        }

        public void Write<T>(IEnumerable<T> rows)
        {
            if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
            {
                throw new Exception("Table.Write<T> does not support primitive types or strings.  Use Write(IEnumerable<IEnumerable<object>> rows)");
            }

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            Column? GetOverriden(PropertyInfo property) => 
                _columnsByProperty?.GetValueOrDefault(property.Name);

            if (Count == 0)
            {
                AddRange(properties.Select(p =>
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
                    ? o => o is 0 ? null : o?.ToString()
                    : type == typeof(bool)
                        ? o => o is true ? "x" : null
                        : null
            };
        }

        public void Write(IEnumerable<IEnumerable<object?>> rows)
        {
            var table = new SpectreTable
            {
                Border = _tableBorder
            };
            foreach (var column in this)
            {
                table.AddColumn(column.ToTableColumn());
            }

            string Format(object? obj, int columnIndex)
            {
                var displayAs = this[columnIndex].DisplayAs;
                return (displayAs is null 
                    ? obj?.ToString()
                    : displayAs(obj)) ?? "";
            }
            
            foreach (var row in rows)
            {
                table.AddRow(row.Select(Format).ToArray());
            }

            AnsiConsole.Write(table);

            if (_includeCount)
            {
                _console.WriteLine();
                _console.WriteLine($"count: {table.Rows.Count}");
            }
        }
    }
}