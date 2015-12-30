using System;
using System.IO;

namespace DependencyMapper
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Usage: DependencyMapper.exe <directory-path> [<html-output-file>]");
                return;
            }

            var dirName = args[0];
            if (!Directory.Exists(dirName))
            {
                Console.Error.WriteLine("\"{0}\" is not a directory.", dirName);
                return;
            }

            string outputFile;
            if (args.Length > 1)
                outputFile = args[1];
            else
                outputFile = Path.GetFileName(dirName) + ".html";

            var items = DependencyMapper.GetItems(dirName);
            ReportMaker.Generate(dirName, items, outputFile);
        }
    }
}
