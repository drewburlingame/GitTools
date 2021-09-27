using CommandDotNet;

namespace BbGit.ConsoleApp
{
    public class TableFormatModel : IArgumentModel
    {
        public enum TableFormat
        {
            /// <summary>simple</summary>
            s,
            /// <summary>keys only</summary>
            k,
            /// <summary>grid lines</summary>
            g,
            /// <summary>markdown</summary>
            m
        }
 
        [Option(
            ShortName = "t", 
            Description = "  s: no grid lines\n" +
                          "    k: keys only, no grid lines\n" +
                          "    g: grid lines\n" +
                          "    m: markdown")]
        public TableFormat Table { get; set; } = TableFormat.s;
    }
}