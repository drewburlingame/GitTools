namespace BbGit.ConsoleUtils
{
    public class TableWriterFormat
    {
        public string Indent { get; set; }
        public string ColumnSpacer { get; set; }

        /// <summary>
        ///     The widths for each column.
        ///     Any value less than 0 will result in scanning all rows to find the maximum width.
        /// </summary>
        public int[] ColumnWidths { get; set; }

        public TableWriterFormat()
        {
            this.Indent = " ";
            this.ColumnSpacer = "  ";
            this.ColumnWidths = new int[0];
        }

        public TableWriterFormat Clone()
        {
            return new TableWriterFormat
            {
                Indent = this.Indent,
                ColumnSpacer = this.ColumnSpacer,
                ColumnWidths = this.ColumnWidths
            };
        }
    }
}