using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace MikuDownloader.misc
{
    public class SerializingHelper
    {

        public static void Serialize(ImageDetails serializableObject, string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Create))
            {
                XmlSerializer xSer = new XmlSerializer(typeof(ImageDetails));

                xSer.Serialize(fs, serializableObject);
            }
        }

        public static void SerializeList(List<ImageDetails> serializableObjectList, string filename)
        {
            var doc = new XDocument();

            using (XmlWriter xmlStream = doc.CreateWriter())
            {
                XmlSerializer xSer = new XmlSerializer(typeof(List<ImageDetails>));

                xSer.Serialize(xmlStream, serializableObjectList);
            }
        }

        public static ImageDetails DeSerialize(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open)) //double check that...
            {
                XmlSerializer _xSer = new XmlSerializer(typeof(ImageDetails));

                var myObject = _xSer.Deserialize(fs);

                return (ImageDetails)myObject;
            }
        }

        public static List<ImageDetails> DeSerializeList(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open)) //double check that...
            {
                XmlSerializer _xSer = new XmlSerializer(typeof(List<ImageDetails>));

                var myObject = _xSer.Deserialize(fs);

                return (List<ImageDetails>)myObject;
            }
        }


        public static string SerializeTest(List<ImageDetails> serializableObjectList)
        {
            var doc = new XDocument();

            using (XmlWriter xmlStream = doc.CreateWriter())
            {
                XmlSerializer xSer = new XmlSerializer(typeof(List<ImageDetails>));

                xSer.Serialize(xmlStream, serializableObjectList);
            }

            return doc.ToString();
        }


        public static string SerializeDoubleList(List<List<ImageDetails>> serializableObjectList)
        {
            var doc = new XDocument();

            using (XmlWriter xmlStream = doc.CreateWriter())
            {
                XmlSerializer xSer = new XmlSerializer(typeof(List<List<ImageDetails>>));

                xSer.Serialize(xmlStream, serializableObjectList);
            }

            return doc.ToString();
        }

        public static List<List<ImageDetails>> DeSerializeTest(string xmlDoc)
        {
            using (StringReader sr= new StringReader(xmlDoc)) //double check that...
            {
                XmlSerializer _xSer = new XmlSerializer(typeof(List<List<ImageDetails>>));

                var myObject = _xSer.Deserialize(sr);

                return (List<List<ImageDetails>>)myObject;
            }
        }
    }
}
