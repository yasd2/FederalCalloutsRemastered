using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace FederalCallouts.Tools
{
    internal static class Serialization
    {
        //
        //CHANGE THIS VALUE DEPENDING ON YOUR PLUGIN'S NAME
        // 
        public static string sPath = @".\Plugins\LSPDFR\FederalCallouts\Drug\";

        public static List<T> LoadFromXML<T>(string FileName)
        {
            List<T> list = new List<T>();

            XmlSerializer deserializer = new XmlSerializer(typeof(List<T>));
            using (TextReader reader = new StreamReader(sPath + FileName/* + ".xml"*/))
            {
                list = new List<T>();
                list = (List<T>)deserializer.Deserialize(reader);
            }

            return list;
        }

        public static void SaveToXML<T>(List<T> list, string FileName)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<T>));
            using (TextWriter writer = new StreamWriter(sPath + FileName + ".xml"))
            {
                serializer.Serialize(writer, list);
            }
        }

        public static void AppendToXML<T>(T ObjectToAdd, string FileName)
        {
            if (!Directory.Exists(sPath))
            {
                Directory.CreateDirectory(sPath);
            }

            if (File.Exists(sPath + FileName + ".xml"))
            {
                List<T> listOfSpawns = LoadFromXML<T>(FileName);
                listOfSpawns.Add(ObjectToAdd);

                SaveToXML<T>(listOfSpawns, FileName);
            }
            else
            {
                List<T> list = new List<T>();
                list.Add(ObjectToAdd);

                SaveToXML<T>(list, FileName);
            }
        }
    }
}
