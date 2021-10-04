namespace BbGit.Tables
{
    public class TableTheme
    {
        #region Themes

        public static TableTheme ColumnLines => new(" ", 
            new Level('┌', '┬', '─',  '┐'),
            new Level('│', '│', ' ', '│'),
            new Level('├', '┼', '─',  '┤'),
            new Level('│', '│', ' ', '│'),
            null,
            new Level('└', '┴', '─',  '┘'))
        {
            Multiline = true
        };

        public static TableTheme RowLines => new(" ",
            new Level('┌', '─', '─', '┐'),
            new Level('│', ' ', ' ', '│'),
            new Level('├', '─', '─', '┤'),
            new Level('│', ' ', ' ', '│'),
            new Level('├', '─', '─', '┤'),
            new Level('└', '─', '─', '┘'))
        {
            Multiline = true
        };

        public static TableTheme Grid => new(" ",
            new Level('┌', '┬', '─',  '┐'),
            new Level('│', '│', ' ', '│'),
            new Level('├', '┼', '─',  '┤'),
            new Level('│', '│', ' ', '│'),
            new Level('├', '┼', '─',  '┤'),
            new Level('└', '┴', '─',  '┘'))
        {
            Multiline = true
        };

        public static TableTheme Markdown => new(" ",
            null,
            new Level('|', '|', ' ', '|'),
            new Level('|', '|', '-', '|'),
            new Level('|', '|', ' ', '|'),
            null,
            null);

        public static TableTheme Borderless => new(" ",
            null,
            new Level(null, null, ' ', null),
            new Level('─', null, '─', '─'),
            new Level(null, null, ' ', null),
            null,
            null)
        {
            Multiline = true
        };

        public static TableTheme DataOnly => new(" ",
            null,
            null,
            null,
            new Level(null, null, ' ', null),
            null,
            null);

        // TODO: CSV that will escape quotes in cells. Add a func to inspect and modify data
        public static TableTheme CSV => new(",",
            null,
            null,
            null,
            new Level(null, null, ' ', null),
            null,
            null);


        #endregion

        public class Level
        {
            public char? Left { get; }
            public char? Delimiter { get; }
            public char Filler { get; }
            public char? Right { get; }

            public Level(char? left, char? delimiter, char filler, char? right)
            {
                Left = left;
                Delimiter = delimiter;
                Filler = filler;
                Right = right;
            }
        }

        public bool PrintHeader => HeaderRow is not null && HeaderSeparator is not null;
        public string Padding { get; }
        public Level Top { get; }
        public Level HeaderRow { get; }
        public Level HeaderSeparator { get; }
        public Level Rows { get; }
        public Level RowSeparator { get; }
        public Level Bottom { get; }

        public bool Multiline { get; set; }

        public TableTheme WithoutHeader => new(Padding, Top, null, null, Rows, RowSeparator, Bottom);

        public int Width(int columnCount) => 3 + (Padding.Length * 2 * columnCount);

        // for serializer
        public TableTheme()
        {
        }

        public TableTheme(string padding, 
            Level top, Level headerRow, Level headerSeparator, 
            Level rows, Level rowSeparator, Level bottom)
        {
            Padding = padding;
            Top = top;
            HeaderRow = headerRow;
            HeaderSeparator = headerSeparator;
            Rows = rows;
            RowSeparator = rowSeparator;
            Bottom = bottom;
        }
    }
}