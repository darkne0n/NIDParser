using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NIDParser
{
    #region Data structures

    [Serializable]
    public struct Library
    {
        public string Name;
        public Module[] Modules;
        [XmlText]
        public string NID;
    }

    [Serializable]
    public struct Module
    {
        [XmlAttribute]
        public string Name;

        public NIDTable Import;
        public NIDTable Export;

        [XmlText]
        public string NID;
    }

    [Serializable]
    public struct NIDTable
    {
        [XmlAttribute]
        public int Count { get { return NID.Count; } set { } }

        [XmlElement]
        public List<NID> NID;
    }

    [Serializable]
    public struct NID
    {
        [XmlAttribute]
        public NIDType Type;

        [XmlText]
        public string Value;
    }

    public enum NIDType
    {
        Unknown, Function, SysCall, Variable, Relative, Unresolved
    }

    public enum Mode
    {
        None,
        Export,
        Import
    }
    #endregion

    public class Program
    {
        static void Main(string[] args)
        {
            args = new string[] { "output.txt", "NIDTable.xml", "ND" };

            if (args.Length < 2 || args.Length > 3 || (args.Length == 3 && args[2] != "ND"))
            {
                Console.WriteLine("NIDParser by hgoel0974\nUsage: NIDParser [VitaDefiler output filename] [XML NID List output file] [options]\nOption:\n\tND : No Duplicates");
                return;
            }

            bool noDuplicates = (args.Length == 3 && args[2] == "ND");

            Dictionary<string, Library> libraries = new Dictionary<string, Library>();
            Dictionary<string, Module> modules = new Dictionary<string, Module>();
            Mode currentMode = Mode.None;

            using (StreamReader reader = new StreamReader(args[0]))
            {
                string currentLibraryName = "", currentModuleName = "";

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    if (line.Contains("[Vita]"))
                    {
                        if (line.Contains("Module:"))
                        {
                            if (currentLibraryName != "")
                            {
                                var tmp = libraries[currentLibraryName];
                                tmp.Modules = modules.Values.ToArray();
                                libraries[currentLibraryName] = tmp;
                                modules = new Dictionary<string, Module>();
                            }

                            currentLibraryName = line.Split(',')[0].Split(' ').Last().Trim();

                            uint nid = 0;
                            if (line.Split(',').Length > 1) nid = Convert.ToUInt32(line.Split(',')[1].Split(' ').Last(), 16);

                            if (currentLibraryName != "" && !libraries.ContainsKey(currentLibraryName))
                            {
                                libraries[currentLibraryName] = new Library()
                                {
                                    Name = currentLibraryName,
                                    NID = nid.ToString(),
                                    Modules = null
                                };
                            }
                        }
                        else if (line.Contains("Export Num"))
                        {
                            currentMode = Mode.Export;

                            currentModuleName = line.Split(',')[1].Split(' ').Last();
                            uint nid = Convert.ToUInt32(line.Split(',')[2].Split(' ').Last(), 16);

                            if (!modules.ContainsKey(currentModuleName))
                            {
                                modules[currentModuleName] = new Module()
                                {
                                    NID = nid.ToString(),
                                    Name = currentModuleName,
                                    Export = new NIDTable() { NID = new List<NID>() },
                                    Import = new NIDTable() { NID = new List<NID>() }
                                };
                            }

                        }
                        else if (line.Contains("Import Num"))
                        {
                            currentMode = Mode.Import;

                            currentModuleName = line.Split(',')[1].Split(' ').Last();
                            uint nid = Convert.ToUInt32(line.Split(',')[2].Split(' ').Last(), 16);

                            if (!modules.ContainsKey(currentModuleName))
                            {
                                modules[currentModuleName] = new Module()
                                {
                                    NID = nid.ToString(),
                                    Name = currentModuleName,
                                    Export = new NIDTable() { NID = new List<NID>() },
                                    Import = new NIDTable() { NID = new List<NID>() }
                                };
                            }
                        }
                        else if (line.Contains("type:"))
                        {
                            uint nid = Convert.ToUInt32(line.Split(',')[0].Split(' ').Last(), 16);
                            NIDType type = (NIDType)Convert.ToInt32(line.Split(',')[1].Split(' ').Last());

                            NID curNID = new NID() { Type = type, Value = nid.ToString() };

                            if (currentMode == Mode.Import)
                            {
                                if (noDuplicates && !modules[currentModuleName].Import.NID.Contains(curNID)) modules[currentModuleName].Import.NID.Add(curNID);
                                else if (!noDuplicates) modules[currentModuleName].Import.NID.Add(curNID);
                            }
                            if (currentMode == Mode.Export)
                            {
                                if (noDuplicates && !modules[currentModuleName].Export.NID.Contains(curNID)) modules[currentModuleName].Export.NID.Add(curNID);
                                else if (!noDuplicates) modules[currentModuleName].Export.NID.Add(curNID);
                            }
                        }
                    }
                }

                XmlSerializer serializer = new XmlSerializer(libraries.Values.ToArray().GetType(), new XmlRootAttribute("Modules"));
                using (TextWriter writer = new StreamWriter(args[1]))
                {
                    serializer.Serialize(writer, libraries.Values.ToArray());
                }
            }
        }
    }
}