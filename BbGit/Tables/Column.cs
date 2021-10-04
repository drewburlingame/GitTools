using System;
using BbGit.Framework;

namespace BbGit.Tables
{
    public class Column
    {
        public string Name { get; }
        public HAlign HAlign { get; set; }
        public bool WrapText { get; set; }
        public int? MinWidth { get; set; }
        public int? MaxWidth { get; set; }
        public Func<object?,string?>? DisplayAs { get; set; } 

        public Column(string name)
        {
            Name = name;
        }
    }
}