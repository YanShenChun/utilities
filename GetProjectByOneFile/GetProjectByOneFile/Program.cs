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
        // static Regex includeReg = new Regex(@"#include\s+(?<sep>[<""])(?<relv_path>[0-9a-zA-Z_/\\]+)[>""]");
        static Regex _includeReg = new Regex(@"#include\s+(?<sep>[<""])(?<relv_path>[^""<>]+)[>""]");
        static string _rootPath = Environment.CurrentDirectory;
        static string _targetFile = string.Empty;
        static string _output = Environment.CurrentDirectory;

        static void Main(string[] args)
        {
            Initialize(args);
#if DEBUG
            //GetDistinctReferences_UT();
            GetRelvDir_UT();
#endif
            Work();
        }

        static void Initialize(string[] args)
        {
#if DEBUG
            args = new string[3];
            args[0] = @"D:\work\chromium\src";
            args[1] = @"D:\work\chromium\src\base\logging_win.h";
            args[2] = @"tmp";
#endif
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: GetProjectByOneFile chromium_project_root cpp_file output");
                Environment.Exit(1);
            }

            try
            {
                _rootPath = Path.GetFullPath(args[0]);
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: specified root path is invalid: {0}", args[0]);
                Console.WriteLine("Details: {0}", e.Message);
                Console.WriteLine("Usage: GetProjectByOneFile chromium_project_root cpp_file output");
                Environment.Exit(1);
            }

            if (!Directory.Exists(_rootPath))
            {
                Console.WriteLine("Specified rootPath: {0} doesn't exist!", _rootPath);
                Console.WriteLine("Usage: GetProjectByOneFile chromium_project_root cpp_file output");
                Environment.Exit(1);
            }

            try
            {
                _targetFile = Path.GetFullPath(args[1]);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: specified target file is invalid: {0}", args[1]);
                Console.WriteLine("Details: {0}", e.Message);
                Console.WriteLine("Usage: GetProjectByOneFile chromium_project_root cpp_file output");
                Environment.Exit(1);
            }

            if (!File.Exists(_targetFile))
            {
                Console.WriteLine("Specified rootPath: {0} doesn't exist!", _targetFile);
                Console.WriteLine("Usage: GetProjectByOneFile chromium_project_root cpp_file output");
                Environment.Exit(1);
            }

            _output = Path.GetFullPath(args[2]);
            Console.WriteLine(_output);
           
            if (!Directory.Exists(_output))
            {
                Directory.CreateDirectory(_output);
            }
        }

        static void Work()
        {
#if DEBUG
            string workPath = GetDistinctTmpFolderBytime();
            workPath = _output;
#else
            string workPath = _output;
#endif
            string targetFileRelvDir = GetRelvDir(workPath, _targetFile);
            CopyFile(_targetFile, targetFileRelvDir);

            List<string> result = GetDistinctReferences(_targetFile);
            int pos = 0;


            while (pos < result.Count)
            {
                string cppFilePath = CombinePath(_rootPath, result[pos]);

                if (File.Exists(cppFilePath))
                {
                    var includedPaths = GetDistinctReferences(cppFilePath);
                    AddRangeToList(result, includedPaths);

                    var corrCppPaths = GenCorrespondingCpp(includedPaths);
                    AddRangeToList(result, corrCppPaths);
                }
                else
                {
                    Console.WriteLine("Error: {0} doesn't exist!", cppFilePath);
                }

                pos++;
            }

            // post copy
            foreach (var item in result)
            {
                string targetDir = GetTargetDir(workPath, item);
                string cppFilePath = CombinePath(_rootPath, item);
                CopyFile(cppFilePath, targetDir);
            }
        }

        static List<string> GenCorrespondingCpp(List<string> headFiles)
        {
            List<string> result = new List<string>();

            foreach (var item in headFiles)
            {
                result.Add(item.Substring(0, item.Length - 2) + ".cc");
            }

            return result;
        }

        static string GetTargetDir(string workPath, string originalInclude)
        {
            string tmp = string.Empty;
            int slashIndex = originalInclude.LastIndexOf('/');

            if (slashIndex >= 0)
            {
                tmp = originalInclude.Substring(0, slashIndex);
            }

            string targetPath = CombinePath(workPath, tmp);
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            return targetPath;
        }

        static string GetRelvDir(string workPath, string originFullPath)
        {
            int originRootLen = _rootPath.Length;

            int slashIndex = originFullPath.LastIndexOf('\\');

            if (slashIndex < 0)
            {
                slashIndex = originFullPath.Length;
            }

            string tmp = originFullPath.Substring(originRootLen + 1, slashIndex - originRootLen);
            tmp = Path.Combine(workPath, tmp);

            if (!Directory.Exists(tmp))
            {
                Directory.CreateDirectory(tmp);
            }

            return tmp;
        }

        static void GetRelvDir_UT()
        {
            string tmpFull = @"D:\work\chromium\src\base\logging_win.h";
            string workRoot = @"D:\tmp";
            string result = GetRelvDir(workRoot, tmpFull);
        }

        static string CombinePath(string root, string part)
        {
            string tmp = Path.Combine(root, part);
            return tmp.Replace('/', '\\');
        }

        static void CopyFile(string file, string desPath)
        {
            if (!File.Exists(file))
            {
                Console.WriteLine("Ignore copy file:{0}", file);
                return;
            }

            string desFilePath = CombinePath(desPath, Path.GetFileName(file));

            if (File.Exists(desFilePath))
            {
                MakeFileWriteable(desFilePath);
            }

            File.Copy(file, desFilePath, true);
        }

        static void MakeFileWriteable(string file)
        {
            var attr = File.GetAttributes(file);
            attr = attr & ~FileAttributes.ReadOnly;
            File.SetAttributes(file, attr);
        }

        static string GetDistinctTmpFolderBytime()
        {
            string folderName = string.Format("{0:yyyyMMdd_HH-mm-ss}", DateTime.Now);
            string fullPath = CombinePath(Environment.CurrentDirectory, folderName);
            Directory.CreateDirectory(fullPath);
            return fullPath;
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

                    Match match = _includeReg.Match(line);
                    if (match.Success)
                    {
                        string sep = match.Groups["sep"].Value;
                        string relvPath = match.Groups["relv_path"].Value;

                        if (sep == "\"")
                        {
                            result.Add(relvPath);
                        }

                        //if (sep == "\"" && !_tracker.ContainsKey(relvPath))
                        //{
                        //    result.Add(relvPath);
                        //    _tracker[relvPath] = 1;
                        //}
                    }
                    else
                    {
                        Console.WriteLine("Error: parse line {0} in file {1}, it's invalid", line, cppFilePath);
                    }
                }
            }

            return result.Distinct().ToList();
        }

        static void AddRangeToList(List<string> result, List<string> includePaths)
        {
            Dictionary<string, byte> tracker = new Dictionary<string, byte>();
            
            foreach (var item in result)
            {
                if (!tracker.ContainsKey(item))
                {
                    tracker[item] = 1;
                }
            }
            
            foreach (var item in includePaths)
            {
                if (!tracker.ContainsKey(item))
                {
                    result.Add(item);
                }
            }
        }

        static void GetDistinctReferences_UT()
        {
            List<string> result_h = GetDistinctReferences(CombinePath(_rootPath, @"base\logging.h"));
            List<string> result_c = GetDistinctReferences(CombinePath(_rootPath, @"base\logging.cc"));
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
