using System;
using CommandDotNet.Diagnostics;

namespace BbGit.ConsoleUtils
{
    public static class ExceptionExtensions
    {
        public static void Print(this Exception e)
        {
            Console.WriteLine(e.Print(includeData: true, includeProperties: true, includeStackTrace: true).ColorError());
        }
    }
}