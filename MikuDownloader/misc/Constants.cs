using System.Collections.Generic;

namespace MikuDownloader
{
    public static class Constants
    {
        public static int Win32MaxPath = 250;
        public static int MaximumRecursions = 4;

        // header needed cause some sites wont accept connections if they dont recognise you as a browser
        public static string UserAgentHeader = "MikuDownloader - https://github.com/Nightelada/MikuDownloader";

        public static string ImagesFilter = "Image files (*.jpg, *.jpeg, *.gif, *.png) | *.jpg; *.jpeg; *.gif; *.png";

        public static string TextFilter = "Text file |*.txt";

        public static string XmlFilter = "XML file |*.xml";

        public static long MaxFileSize = 8388608;
        public static List<string> AcceptedFileExtensions = new List<string>() {"png", "jpg", "gif" };
        //
        public static string IQDB = "http://iqdb.org/";
        public static string SauceNAO = "http://saucenao.com/search.php?db=999&dbmaski=32768&url=";
        public static string TinEye = "http://tineye.com/search?url=";
        public static string Google = "http://www.google.com/searchbyimage?image_url=";
        public static string Ascii2d = "https://ascii2d.net/search/url/";


        public static string SauceNAOMain = "http://saucenao.com/";
        public static string TinEyeMain = "http://tineye.com/";
        public static string GoogleMain = "https://images.google.com/";
        public static string Ascii2dMain = "https://ascii2d.net/";

        public static string FacebookTempURL = "fbcdn.net"; //scontent.fsof3-1.fna.fbcdn.net

        public static string GeneralLogMessage = "Attempting to generate list of matching images... : ";

        public static string MainDirectory = "MikuDownloaderImages";
        public static string MainLogDirectory = "MikuLogs";
        public static string LoadedDirectory = "Loaded";
        public static string NotLoadedDirectory = "NotLoaded";
        public static string FailLoadedDirectory = "FailLoaded";
        public static string WebpConvertedDirectory = "WebpConverted";


        public static string MainLogFileName = "miku-loader-log.txt";
        public static string MainErrorLogFileName = "miku-loader-error-log.txt";
        public static string XmlFileName = "image-list.xml";
        public static string NotLoadedFileName = "not-loaded-images.txt";
        public static string FailLoadedFileName = "fail-downloaded-images.txt";
        public static string NotCheckedFileName = "not-checked-images.txt";


        public static string VeryLongLine = "------------------------------------------------------------------------------------";

        // rest calls
        public static string JsonReqParameterDanbooru = ".json";
        public static string JsonReqParameterGelbooru = "&json=1";
        public static string JsonReqParameterZerochan = "?json";
        public static string JsonReqParameterSankakuChannel = "https://capi-v2.sankakucomplex.com/posts?tags=id_range:";


    }
}
