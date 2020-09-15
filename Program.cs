using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BPIndex
{
    class Program
    {
        private static string bpPath;
        private static Dictionary<string, Blueprint> fileDict = new Dictionary<string, Blueprint>();
        static void Main(string[] args)
        {
            bpPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            BuildToRefer();
            BuildFromRefer();
            BuildHTML();
            Console.WriteLine("Complete.");
            Console.ReadLine();
        }

        private static void BuildFromRefer()
        {
            
            Console.WriteLine("Building refer from list...");
            foreach (KeyValuePair<string, Blueprint> kvp in fileDict)
            {
                foreach(KeyValuePair<string, Refer> referKvp in kvp.Value.ReferTo)
                {
                    if(fileDict.ContainsKey(referKvp.Key))
                    {
                        if(!fileDict[referKvp.Key].ReferFrom.ContainsKey(kvp.Key))
                        {
                            fileDict[referKvp.Key].ReferFrom.Add(kvp.Key, new Refer(kvp.Value.Path, kvp.Value.Name));
                        }
                    }
                }
            }
        }

        private static void BuildHTML()
        {
            string target = bpPath + "index.txt";
            string currentPath = "";
            string oldPath = "";
            string file;

            if (File.Exists(target))
                File.Delete(target);
            using (TextWriter tw = File.CreateText(target))
            {
                foreach (KeyValuePair<string, Blueprint> kvp in fileDict)
                {
                    currentPath = kvp.Value.Path.Substring(0, kvp.Value.Path.LastIndexOf('\\') + 1);
                    if (!currentPath.Equals(oldPath))
                        tw.WriteLine("{0}", currentPath);
                    oldPath = currentPath;
                    file = kvp.Value.Path.Substring(kvp.Value.Path.LastIndexOf('\\') + 1);
                    if (kvp.Value.ReferTo.Count > 0 || kvp.Value.ReferFrom.Count > 0)
                    {
                        tw.WriteLine("\tBlueprint: {0}",
                        file);

                        if (kvp.Value.ReferTo.Count > 0)
                            tw.WriteLine("\t\tRefers To:");
                        foreach (KeyValuePair<string, Refer> rto in kvp.Value.ReferTo)
                        {
                            tw.WriteLine("\t\t\tGuid: {0,-40} Name: {1,-40} Type: {2}", rto.Key, rto.Value.Name, rto.Value.Type);
                        }

                        if (kvp.Value.ReferFrom.Count > 0)
                            tw.WriteLine("\t\tRefered From:");
                        foreach (KeyValuePair<string, Refer> rfrom in kvp.Value.ReferFrom)
                        {
                            tw.WriteLine("\t\t\tGuid: {0,-40} Name: {1,-40}", rfrom.Key, rfrom.Value.Name);
                        }
                    }
                }
            }
        }
        private static void BuildToRefer()
        {
            Console.WriteLine("Building File List...");
            string[] files = Directory.GetFiles(bpPath, "*.json", SearchOption.AllDirectories);
            Console.WriteLine("{0} files found...", files.Length);
            Console.WriteLine("Building refer to list...");
            foreach (string file in files)
            {
                string guid = GetGuidFromPath(file);
                if (!fileDict.ContainsKey(guid))
                    fileDict.Add(guid, new Blueprint(file));
                else
                    Console.WriteLine("Warning:  {0} guid already exsits", guid);

            }
        }

        private static string GetGuidFromPath(string target)
        {
            string[] tests = target.Substring(target.LastIndexOf('\\') + 1).Split('.');
            Guid result;
            foreach(string test in tests)
            {
                if(Guid.TryParse(test, out result))
                {
                    return (test);
                }
            }
            return "";
        }
    }

    internal class Refer
    {
        public string Type { get; set; }
        public string Name { get; set; }

        public Refer(string type, string name)
        {
            this.Type = type;
            this.Name = name;
        }
    }

    internal class Blueprint
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public Dictionary<string, Refer> ReferTo = new Dictionary<string, Refer>();
        public Dictionary<string, Refer> ReferFrom = new Dictionary<string, Refer>();

        public Blueprint(string target)
        {
            this.Path = target;
            this.Name = target.Substring(target.LastIndexOf('\\') + 1).Split('.')[0];

            string line;
            string line2 ="";
            string previous = "";
            Regex pattern = new Regex("[,\\[\":]");
            using (TextReader tr = File.OpenText(target))
            {
                while((line = tr.ReadLine()) != null)
                {
                    if (line.Contains("Blueprint:"))
                    {
                        string[] splits = line.Split(':');
                        if (splits[0].Trim(' ').Equals("\"Blueprint"))
                        {
                            line2 = line;
                            do
                            {
                                splits = line2.Split(':');
                                if (splits.Length > 2)
                                {
                                    if (!ReferTo.ContainsKey(splits[1]))
                                        ReferTo.Add(splits[1], new Refer(pattern.Replace(previous.Trim(' '), ""), pattern.Replace(splits[2], "")));
                                }
                                line2 = tr.ReadLine();
                            } while (line2.Contains("Blueprint:") || pattern.Replace(line2.Trim(), "").Equals("null"));
                        }
                        else
                        {
                            if (!ReferTo.ContainsKey(splits[2]))
                                ReferTo.Add(splits[2], new Refer(pattern.Replace(splits[0].Trim(' '), ""), pattern.Replace(splits[3], "")));
                        }
                    }
                    previous = line;
                }
            }
        }
    }
}