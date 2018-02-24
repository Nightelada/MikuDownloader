namespace MikuDownloader
{
    public static class Constants
    {
        public static int Win32MaxPath = 250;

        // imgur data
        public static string UploadImageEndpoint = "https://api.imgur.com/3/image";
        public static string AuthorizationHeader = "Bearer 70d0f77b441d2352620cfe6af9251048d7e67b61";
        public static string ClientIDHeader = "Client-ID dcd596649fca845";

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

        public static string SauceNAOMain = "http://saucenao.com/";
        public static string TinEyeMain = "http://tineye.com/";
        public static string GoogleMain = "https://images.google.com/";

        public static string FacebookTempURL = "fbcdn.net"; //scontent.fsof3-1.fna.fbcdn.net

        public static string TheAnimeGalleryErrorMessage = "The Anime Gallery images must be downloaded manually! Check links above!";

        public static string MainDownloadDirectory = "MikuDownloaderImages";
        public static string MainLogFileName = "downloaded-images-log.txt";
        public static string SecondaryLogFileName = "folder-downloaded-images-log.txt";
        public static string RecommendationsLogFileName = "recommendations-log.txt";
        public static string DuplicatesLogFileName = "duplicates-log.txt";
        public static string DuplicatesDirectory = "duplicates";
        public static string NotDownloadedFilename = "not-downloaded-log.txt";
        public static string BetterResolutionFilename = "for-download-images.xml";
        public static string BetterResolutionDirectory = "for-download";
        public static string GoodResolutionDirectory = "good-resolution";

        public static string VeryLongLine = "------------------------------------------------------------------------------------";

        public static string FromFileHelpText = "Downloading image from file:\n\nSelect an image file (.jpg, .png, .gif) from the opened explorer menu" +
            "\nYou can choose to keep the original filename (to replace it easier after download)" +
            "\nand you can also choose to download image even if a duplicate resolution found." +
            "\nImage will be automatically downloaded if found" +
            "\nAfter image is downloaded you can view it in the download directory";

        public static string FromURLHelpText = "Downloading image from URL:\n\nPaste an existing image link into the text box" +
           "\nClick From URL button" +
           "\nImage will be automatically downloaded if found" +
           "\nAfter image is downloaded you can view it in the download directory";

        public static string FromListHelpText = "Downloading image from list:\n\nCreate a .txt file with links to images from the Internet" +
           "\nEach image link must be on a new line in the file" +
           "\nClick Download from list button" +
           "\nImages will be automatically downloaded if found" +
           "\nAfter images are downloaded you can view them in the download directory";

        public static string CheckFolderHelpText = "Checking folder for duplicates and images with better resolution:\n" +
            "\nSelect a folder containing image files (.jpg, .png, .gif) from the opened explorer menu" +
            "\nThe folder will be searched for duplicated images and sorted into folders based on them" +
            "\nIt will also make a list of all files for which better resolution has been found as an XML file" +
            "\nwhich you can then download with the Download from XML button" +
            "\nAfter images are downloaded you can view them in the download directory";

        public static string FromXMLHelpText = "Downloading images from XML list:\n" +
            "\nSelect an XML containing found images with better resolutions (from checking folder)" +
            "\nAll images contained in the list will be downloaded normally" +
            "\nAfter images are downloaded you can view them in the download directory";
    }
}
