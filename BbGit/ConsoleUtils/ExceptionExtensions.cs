using System;
using System.Drawing;
using CommandDotNet.Diagnostics;
using Console = Colorful.Console;

namespace BbGit.ConsoleUtils
{
    public static class ExceptionExtensions
    {
        public static void Print(this Exception e)
        {
            Console.WriteLine(e.Print(includeData: true, includeProperties: true, includeStackTrace: true), Color.Red);
        }
    }
}