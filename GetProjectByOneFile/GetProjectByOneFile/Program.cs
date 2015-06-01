using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GetProjectByOneFile
{
    class Program
    {
        static Regex includeReg = new Regex(@"#include\s+(?<sep>[<""])(?<relv_path>[0-9a-zA-Z_\\]+)[>""]");
        static string rootPath = Environment.CurrentDirectory;
        static void Main(string[] args)
        {
            GetDistinctReferences_UT();
        }

        static List<string> GetDistinctReferences(string cppFilePath)
        {
            List<string> result = new List<string>();
 
            using (StreamReader reader = new StreamReader(cppFilePath))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine().Trim();
                    if (!line.StartsWith("#include "))
                        continue;

                    if (line.StartsWith("namespace "))
                        break;

                    Match match = includeReg.Match(line);
                    if (match.Success)
                    {
                        string sep = match.Groups["sep"].Value;
                        string relvPath = match.Groups["relv_path"].Value;

                        if (sep == "\"")
                        {
                            result.Add(relvPath);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error: parse line {0} in file {1}, it's invalid", line, cppFilePath);
                    }
                }
            }

            return result.Distinct().ToList();
        }

        static void GetDistinctReferences_UT()
        {
            List<string> result_h = GetDistinctReferences(@"E:\chromium\42.0.2311.135\src\base\logging.h");
            List<string> result_c = GetDistinctReferences(@"E:\chromium\42.0.2311.135\src\base\logging.cc");
        }

        static bool IsSkippiedLine(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return true;
            }

            //if (line.StartsWith("") || line.StartsWith(""))
            //{
            //    return true;
            //}
            
            return false;
        }
    }
}
