using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NIDParser
{
    public class Program
    {
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
            FunctionImport,
            FunctionExport,
            VariableImport,
            VariableExport
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("NIDParser by hgoel0974\nUsage: NIDParser [VitaDefiler output filename]");
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
                        if (line.Contains("Module: "))
                        {
                            currentModuleName = line.Split(',')[0].Split(' ').Last();
                        }
                        else
                        {
                            currentModuleName = line.Split(' ').Last(); //The module name is the last part of the line
                        }

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
                    else if (line.Contains("Function Export Num: "))
                    {
                        currentMode = Mode.FunctionExport;
                    }
                    else if (line.Contains("Function Import Num: "))
                    {
                        currentMode = Mode.FunctionImport;
                    }
                    else if (line.Contains("Variable Export Num: "))
                    {
                        currentMode = Mode.VariableExport;
                    }
                    else if (line.Contains("Variable Import Num: "))
                    {
                        currentMode = Mode.VariableImport;
                    }
                    else if ((currentMode == Mode.FunctionExport || currentMode == Mode.VariableExport) && line.StartsWith("[Vita] resolve.c:111 "))
                    {
                        line = line.Replace("[Vita] resolve.c:111 ", "");

                        uint nid = Convert.ToUInt32(line.Split(',')[0].Replace("NID: ", "").Trim(), 16);
                        NIDType typeCode = (NIDType)Convert.ToInt32(line.Split(',')[1].Replace("type: ", "").Trim()); //Parse the typeCode

                        var nidBlock = new NID()
                        {
                            Type = typeCode,
                            Value = nid
                        };

                        // if (!modules[currentModuleName].Exports.Contains(nidBlock))
                        modules[currentModuleName].Exports.Add(nidBlock);

                    }
                    else if ((currentMode == Mode.FunctionImport || currentMode == Mode.VariableImport) && line.StartsWith("[Vita] resolve.c:247"))
                    {
                        line = line.Replace("[Vita] resolve.c:247 ", "").Trim();

                        uint nid = Convert.ToUInt32(line.Split(',')[0].Replace("NID: ", "").Trim(), 16);
                        NIDType typeCode = (NIDType)Convert.ToInt32(line.Split(',')[1].Replace("type: ", "").Trim()); //Parse the typeCode

                        var nidBlock = new NID()
                        {
                            Type = typeCode,
                            Value = nid
                        };
                        // if (!modules[currentModuleName].Imports.Contains(nidBlock))
                        modules[currentModuleName].Imports.Add(nidBlock);
                    }

                }
            }

            XmlSerializer serializer = new XmlSerializer(modules.Values.ToArray().GetType(), new XmlRootAttribute("Modules"));
            using (TextWriter writer = new StreamWriter("NIDTable.xml"))
            {
                serializer.Serialize(writer, modules.Values.ToArray());
            }
        }
    }
}
