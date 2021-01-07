namespace MikuDownloader
{
    public static class Constants
    {
        public static int Win32MaxPath = 250;
        public static int MaximumRecursions = 4;

        // header needed cause some sites wont accept connections if they dont recognise you as a browser
        public static string UserAgentHeader = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";

        public static string ImagesFilter = "Image files (*.jpg, *.jpeg, *.gif, *.png) | *.jpg; *.jpeg; *.gif; *.png";

        public static string TextFilter = "Text file |*.txt";

        public static string XmlFilter = "XML file |*.xml";

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
        public static string TheAnimeGalleryErrorMessage = "The Anime Gallery images must be downloaded manually! Check image post links above!";
        public static string ZerochanErrorMessage = "Zerochan images require registration and must be downloaded manually! Check image post links above!";

        public static string MainDirectory = "MikuDownloaderImages";
        public static string MainLogDirectory = "MikuLogs";
        public static string LoadedDirectory = "Loaded";
        public static string NotLoadedDirectory = "NotLoaded";
        public static string FailLoadedDirectory = "FailLoaded";

        public static string MainLogFileName = "miku-loader-log.txt";
        public static string XmlFileName = "image-list.xml";

        public static string VeryLongLine = "------------------------------------------------------------------------------------";
    }
}
