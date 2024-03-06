using System.Collections.Generic;

namespace MikuDownloader.misc.rest
{
    internal class ZerochanRest
    {// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Root
        {
            public int? id { get; set; }
            public string small { get; set; }
            public string medium { get; set; }
            public string large { get; set; }
            public string full { get; set; }
            public int? width { get; set; }
            public int? height { get; set; }
            public int? size { get; set; }
            public string hash { get; set; }
            public string source { get; set; }
            public string primary { get; set; }
            public List<string> tags { get; set; }
        }


    }
}
