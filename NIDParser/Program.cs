#define NO_DUPLICATES

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
    public struct Module
    {
        [XmlAttribute]
        public string Name;

        [XmlElement(Order = 1)]
        public int ImportCount { get { return Imports.Count; } set { } }

        [XmlArray(Order = 2)]
        public List<NID> Imports;

        [XmlElement(Order = 3)]
        public int ExportCount { get { return Exports.Count; } set { } }

        [XmlArray(Order = 4)]
        public List<NID> Exports;
    }

    [Serializable]
    public struct NID
    {
        [XmlAttribute]
        public NIDType Type;

        [XmlText]
        public uint Value;
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
            if (args.Length < 2)
            {
                Console.WriteLine("NIDParser by hgoel0974\nUsage: NIDParser [VitaDefiler output filename] [XML NID List output file]");
                return;
            }

            Dictionary<string, Module> modules = new Dictionary<string, Module>();
            Mode currentMode = Mode.None;

            using (StreamReader reader = new StreamReader(args[0]))
            {
                string currentModuleName = "";
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    if (line.Contains("Module: "))
                    {
                        currentModuleName = line.Split(',')[0].Split(' ').Last();

                        if (!modules.ContainsKey(currentModuleName) && currentModuleName != "")  //If this is a new module initialize all the objects inside
                        {
                            modules[currentModuleName] = new Module()
                            {
                                Name = currentModuleName,
                                Exports = new List<NID>(),
                                Imports = new List<NID>()
                            };
                        }

                    }
                    else if (line.Contains("Export Num: "))
                    {
                        currentMode = Mode.Export;
                    }
                    else if (line.Contains("Import Num: "))
                    {
                        currentMode = Mode.Import;
                    }
                    else if (currentMode != Mode.None && line.Contains("NID:") && line.Contains("type:"))
                    {
                        line = line.Replace("[Vita] resolve.c:111 ", "");
                        line = line.Replace("[Vita] resolve.c:247 ", "").Trim();

                        uint nid = Convert.ToUInt32(line.Split(',')[0].Replace("NID: ", "").Trim(), 16);
                        NIDType typeCode = (NIDType)Convert.ToInt32(line.Split(',')[1].Replace("type: ", "").Trim()); //Parse the typeCode

                        var nidBlock = new NID()
                        {
                            Type = typeCode,
                            Value = nid
                        };

                        if (currentMode == Mode.Export)
                        {
#if NO_DUPLICATES
                        if (!modules[currentModuleName].Exports.Contains(nidBlock))
#endif
                            modules[currentModuleName].Exports.Add(nidBlock);
                        }
                        else if (currentMode == Mode.Import)
                        {
#if NO_DUPLICATES
                        if (!modules[currentModuleName].Imports.Contains(nidBlock))
#endif
                            modules[currentModuleName].Imports.Add(nidBlock);
                        }

                    }
                }

                XmlSerializer serializer = new XmlSerializer(modules.Values.ToArray().GetType(), new XmlRootAttribute("Modules"));
                using (TextWriter writer = new StreamWriter(args[1]))
                {
                    serializer.Serialize(writer, modules.Values.ToArray());
                }
            }
        }
    }
}