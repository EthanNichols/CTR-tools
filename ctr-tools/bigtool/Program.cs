﻿using CTRFramework;
using CTRFramework.Shared;
using System;
using System.IO;

namespace bigtool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(
                "{0}\r\n{1}\r\n\r\n{2}\r\n",
                "CTR-tools: BigTool",
                "Builds and extracts Crash Team Racing BIG files",
                Meta.GetVersion());

            if (args.Length > 0)
            {
                Console.WriteLine("Current path: " + Environment.CurrentDirectory);

                string ext = Path.GetExtension(args[0]).ToLower();

                try
                {
                    BigFile big = BigFile.FromFile(args[0]);

                    if (big.Entries.Count > 0)
                    {
                        switch (ext)
                        {
                            case ".big": big.Export(".\\bigfile"); break;
                            case ".txt": big.Build(".\\bigfile.big"); break;
                            default: Console.WriteLine("{0}: {1}", "Unsupported file", ext); break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }

                return;
            }

            Console.WriteLine(
               "{0}:\r\n\t{1}: {2}\r\n\t{3}: {4}",
               "Usage",
               "Extract example", "bigtool C:\\BIGFILE.BIG",
               "Build example", "bigtool C:\\BIGFILE.TXT"
            );
        }
    }
}
