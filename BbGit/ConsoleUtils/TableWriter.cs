using System;
using System.Collections.Generic;
using System.Linq;
using BbGit.Framework;
using MoreLinq;

namespace BbGit.ConsoleUtils
{
    public static class TableWriter
    {
        /// <summary></summary>
        public static void WriteTable(
            this IEnumerable<string> rows,
            string[] headers = null,
            TableWriterFormat format = null,
            bool printRowCount = true)
        {
            rows.Select(cell => new[] {cell}).WriteTable(headers, format, printRowCount);
        }

        /// <summary></summary>
        public static void WriteTable(
            this IEnumerable<string[]> rows,
            string[] headers = null,
            TableWriterFormat format = null,
            bool printRowCount = true)
        {
            format = format == null
                ? new TableWriterFormat()
                : format.Clone();

            var safeRows = rows as ICollection<string[]> ?? rows.ToArray();

            var columnCount = GetColumnCount(headers, safeRows);

            EnsureColumnWidthAccomodatesHeadersAndRows(headers, format, columnCount, safeRows);

            if (headers != null)
            {
                var headerLine = BuildRowLine(headers, format);

                Console.WriteLine(headerLine);
                Console.WriteLine("");
            }

            if (safeRows.IsNullOrEmpty())
            {
                return;
            }

            var rowCount = 0;
            foreach (var row in safeRows)
            {
                rowCount++;

                if (row.IsNullOrEmpty())
                {
                    Console.WriteLine();
                    continue;
                }

                var rowLine = BuildRowLine(row, format);
                if (rowCount % 5 == 0)
                {
                    Console.WriteLine(rowLine);
                    Console.WriteLine("");
                }
                else
                {
                    Console.WriteLine(rowLine);
                }
            }

            if (printRowCount)
            {
                Console.WriteLine();
                Console.WriteLine($"{rowCount} rows");
            }
        }

        private static string BuildRowLine(string[] row, TableWriterFormat format)
        {
            var headerLine = row
                .Select(cell => cell ?? "")
                .Select((cell, i) => new {cell, shortage = format.ColumnWidths[i] - cell.Length})
                .Select(cellInfo =>
                    cellInfo.shortage == 0 ? cellInfo.cell : $"{cellInfo.cell}{new string(' ', cellInfo.shortage)}")
                .ToCsv(format.ColumnSpacer);
            return headerLine;
        }

        private static int GetColumnCount(string[] headers, ICollection<string[]> safeRows)
        {
            var columnCount = 0;
            if (headers != null)
            {
                columnCount = headers.Length;
            }

            if (!safeRows.IsNullOrEmpty())
            {
                var rowColumnCount = safeRows.First().Length;
                if (columnCount > 0 && rowColumnCount != columnCount)
                {
                    throw new Exception(
                        $"count of columins in rows ({rowColumnCount}) " +
                        $"does not match count of columns in headers ({columnCount})");
                }

                columnCount = rowColumnCount;
            }

            return columnCount;
        }

        private static void EnsureColumnWidthAccomodatesHeadersAndRows(
            string[] headers,
            TableWriterFormat format,
            int columnCount,
            ICollection<string[]> safeRows)
        {
            if (format.ColumnWidths.IsNullOrEmpty())
            {
                format.ColumnWidths = new int[columnCount];
            }

            if (headers != null)
            {
                EnsureColumnWidthAccomodatesRow(format, headers);
            }

            if (!safeRows.IsNullOrEmpty())
            {
                EnsureColumnWidthAccomodatesRows(format, safeRows);
            }
        }

        private static void EnsureColumnWidthAccomodatesRows(TableWriterFormat format, ICollection<string[]> safeRows)
        {
            safeRows.ForEach(row => EnsureColumnWidthAccomodatesRow(format, row));
        }

        private static void EnsureColumnWidthAccomodatesRow(TableWriterFormat format, string[] row)
        {
            try
            {
                row.ForEach((cell, index) =>
                    format.ColumnWidths[index] =
                        Math.Max(format.ColumnWidths[index],
                            (cell?.Length).GetValueOrDefault() + format.ColumnSpacer.Length));
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"{e.Message} {new {rowCount = row.Length, columnWidthCount = format.ColumnWidths.Length}}", e);
            }
        }
    }
}