using System;
using BbGit.Framework;
using Spectre.Console;

namespace BbGit.Tables
{
    public class Column
    {
        public string Name { get; }
        public HAlign HAlign { get; set; }
        public bool WrapText { get; set; }
        public Func<object?,string?>? DisplayAs { get; set; } 

        public Column(string name)
        {
            Name = name;
        }

        public TableColumn ToTableColumn()
        {
            return new TableColumn(Name)
            {
                Alignment = HAlign.ToString().ToEnum<Justify>(),
                NoWrap = !WrapText
            };
        }
    }
}