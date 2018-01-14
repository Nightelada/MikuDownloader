using MikuDownloader.image;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace MikuDownloader.misc
{
    public class SerializingHelper
    {
        public static string SerializeImageList(List<ImageData> serializableObjectList)
        {
            var doc = new XDocument();

            using (XmlWriter xmlStream = doc.CreateWriter())
            {
                XmlSerializer xSer = new XmlSerializer(typeof(List<ImageData>));

                xSer.Serialize(xmlStream, serializableObjectList);
            }

            return doc.ToString();
        }

        public static List<ImageData> DeserializeImageList(string xmlDoc)
        {
            using (StringReader sr= new StringReader(xmlDoc)) //double check that...
            {
                XmlSerializer xSer = new XmlSerializer(typeof(List<ImageData>));

                var myObject = xSer.Deserialize(sr);

                return (List<ImageData>)myObject;
            }
        }
    }
}
