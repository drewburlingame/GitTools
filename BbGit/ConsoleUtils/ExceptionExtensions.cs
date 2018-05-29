using System;
using System.Drawing;
using Console = Colorful.Console;

namespace BbGit.ConsoleUtils
{
    public static class ExceptionExtensions
    {
        public static void Print(this Exception e)
        {
            Console.WriteLine(e, Color.Red);
        }
    }
}