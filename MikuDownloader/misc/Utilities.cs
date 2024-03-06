using FileTypeChecker.Abstracts;
using FileTypeChecker.Extensions;
using FileTypeChecker;
using HtmlAgilityPack;
using MikuDownloader.image;
using MikuDownloader.misc.rest;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace MikuDownloader.misc
{
    public static class Utilities
    {
        // Logs data in the searching function
        public static void LogSearch(string logText)
        {
            if (!string.IsNullOrEmpty(logText))
            {
                string fullLog = $"{Constants.VeryLongLine}\nLogSearch {DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}\n{Constants.GeneralLogMessage}\n{logText}";
                File.AppendAllText(GetLogFileName(), fullLog);
            }
        }

        // Logs onlye errors in the searching function
        public static void LogSearchErrors(string logText)
        {
            if (!string.IsNullOrEmpty(logText))
            {
                string fullLog = $"{Constants.VeryLongLine}\nLogSearchErrors {DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}\n{logText}";
                File.AppendAllText(GetErrorLogFileName(), fullLog);
            }
        }

        // Logs data in the downloading function
        public static void LogDownload(string logText)
        {
            if (!string.IsNullOrEmpty(logText))
            {
                string fullLog = $"{Constants.VeryLongLine}\nLogDownload {DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}\n{logText}";
                File.AppendAllText(GetLogFileName(), fullLog);
            }
        }

        // Logs data in the sorting function
        public static void LogSort(string logText)
        {
            string fullLog = $"{Constants.VeryLongLine}\nLogSort {DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}\n{logText}";
            File.AppendAllText(GetLogFileName(), fullLog);
        }

        // Logs fail loaded images
        public static void LogFailLoaded(string logText)
        {
            if (!string.IsNullOrEmpty(logText))
            {
                string fullLog = $"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")} : {logText}\n";
                File.AppendAllText(GetFailLoadedFileName(), fullLog);
            }
        }

        // Logs not loaded images
        public static void LogNotLoaded(string logText)
        {
            if (!string.IsNullOrEmpty(logText))
            {
                string fullLog = $"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")} : {logText}\n";
                File.AppendAllText(GetNotLoadedFileName(), fullLog);
            }
        }

        // Logs not checked images
        public static void LogNotChecked(string logText)
        {
            if (!string.IsNullOrEmpty(logText))
            {
                string fullLog = $"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")} : {logText}\n";
                File.AppendAllText(GetNotCheckedFileName(), fullLog);
            }
        }

        // Gets log filename for main log
        public static string GetLogFileName()
        {
            var currTime = DateTime.Now.ToString("yyyyMMdd");
            var fileName = currTime + "_" + Constants.MainLogFileName;
            Directory.CreateDirectory(Constants.MainLogDirectory);
            return Path.Combine(Constants.MainLogDirectory, fileName);
        }

        // Gets log filename for error only log
        public static string GetErrorLogFileName()
        {
            var currTime = DateTime.Now.ToString("yyyyMMdd");
            var fileName = currTime + "_" + Constants.MainErrorLogFileName;
            Directory.CreateDirectory(Constants.MainLogDirectory);
            return Path.Combine(Constants.MainLogDirectory, fileName);
        }

        // Gets log filename for fail loaded images
        public static string GetFailLoadedFileName()
        {
            var currTime = DateTime.Now.ToString("yyyyMMdd");
            var fileName = currTime + "_" + Constants.FailLoadedFileName;
            Directory.CreateDirectory(Constants.MainLogDirectory);
            return Path.Combine(Constants.MainLogDirectory, fileName);
        }

        // Gets log filename for not loaded images
        public static string GetNotLoadedFileName()
        {
            var currTime = DateTime.Now.ToString("yyyyMMdd");
            var fileName = currTime + "_" + Constants.NotLoadedFileName;
            Directory.CreateDirectory(Constants.MainLogDirectory);
            return Path.Combine(Constants.MainLogDirectory, fileName);
        }

        // Gets log filename for not loaded images
        public static string GetNotCheckedFileName()
        {
            var currTime = DateTime.Now.ToString("yyyyMMdd");
            var fileName = currTime + "_" + Constants.NotCheckedFileName;
            Directory.CreateDirectory(Constants.MainLogDirectory);
            return Path.Combine(Constants.MainLogDirectory, fileName);
        }

        // Gets filename for serialized image list
        public static string GetXmlFilename()
        {
            var currTime = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = currTime + "_" + Constants.XmlFileName;
            Directory.CreateDirectory(Constants.MainLogDirectory);
            return Path.Combine(Constants.MainLogDirectory, fileName);
        }

        // Gets the name of the Directory in which downloaded files go in
        public static string GetLoadedDirectory()
        {
            return Path.Combine(Constants.MainDirectory, Constants.LoadedDirectory);
        }

        // Gets the name of the Directory in which downloaded files go in
        public static string GetNotLoadedDirectory()
        {
            return Path.Combine(Constants.MainDirectory, Constants.NotLoadedDirectory);
        }

        // Gets the name of the Directory in which downloaded files go in
        public static string GetFailLoadedDirectory()
        {
            return Path.Combine(Constants.MainDirectory, Constants.FailLoadedDirectory);
        }

        // Gets the name of the Directory in which downloaded files go in
        public static string GetWebpConvertedDirectory()
        {
            return Path.Combine(Constants.MainDirectory, Constants.WebpConvertedDirectory);
        }

        // Parses a file full of URLs to a list
        public static List<string> ParseURLs(string filepath)
        {
            List<string> finalURLs = new List<string>();
            List<string> tempList = new List<string>();

            if (!string.IsNullOrEmpty(filepath))
            {
                tempList = File.ReadAllLines(filepath).Where(x => !String.IsNullOrEmpty(x)).ToList();
            }

            foreach (string url in tempList)
            {
                if (CheckIfRealUrl(url))
                {
                    finalURLs.Add(url);
                }
            }
            return finalURLs;
        }

        // Returns the size of a file in bytes
        public static long GetFileSize(string filename)
        {
            return new FileInfo(filename).Length;
        }

        // Returns a human readable file size
        public static string GetSizeString(long length)
        {
            long B = 0, KB = 1024, MB = KB * 1024, GB = MB * 1024, TB = GB * 1024;
            double size = length;
            string suffix = nameof(B);

            if (length >= TB)
            {
                size = Math.Round((double)length / TB, 2);
                suffix = nameof(TB);
            }
            else if (length >= GB)
            {
                size = Math.Round((double)length / GB, 2);
                suffix = nameof(GB);
            }
            else if (length >= MB)
            {
                size = Math.Round((double)length / MB, 2);
                suffix = nameof(MB);
            }
            else if (length >= KB)
            {
                size = Math.Round((double)length / KB, 2);
                suffix = nameof(KB);
            }

            return $"{size} {suffix}";
        }

        // Checks if the file is image
        public static bool IsImage(string filename, out IFileType fileType)
        {
            using (var fileStream = File.OpenRead(filename))
            {
                var isRecognizableType = FileTypeValidator.IsTypeRecognizable(fileStream);

                if (!isRecognizableType)
                {
                    fileType = null;
                    return false;
                }

                fileType = FileTypeValidator.GetFileType(fileStream);
                return fileStream.IsImage();
            }
        }

        // Returns file type based on byte header information
        public static IFileType GetFileType(string filename)
        {
            using (var fileStream = File.OpenRead(filename))
            {
                var isRecognizableType = FileTypeValidator.IsTypeRecognizable(fileStream);

                if (!isRecognizableType)
                {
                    return null;
                }

                IFileType fileType = FileTypeValidator.GetFileType(fileStream);
                return fileType;
            }
        }

        // Returns the resolution of an image from the file system
        public static string GetResolution(string filename)
        {
            string resolution = string.Empty;

            using (var imageStream = File.OpenRead(filename))
            {
                var decoder = BitmapDecoder.Create(imageStream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.Default);
                var height = decoder.Frames[0].PixelHeight;
                var width = decoder.Frames[0].PixelWidth;
                resolution = string.Format("{0}{1}{2}", width, '×', height);
            }
            return resolution;
        }

        // Compares the resolution values of two images
        private static string CompareResolutions(string resA, string resB, out long numRes)
        {
            string[] strAparams = resA.Split(new char[] { '×', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] strBparams = resB.Split(new char[] { '×', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (strAparams.Length >= 2 && strBparams.Length >= 2)
            {
                int aX = int.Parse(strAparams[0]);
                int aY = int.Parse(strAparams[1]);
                int bX = int.Parse(strBparams[0]);
                int bY = int.Parse(strBparams[1]);

                if (aX * aY >= bX * bY)
                {
                    numRes = aX * aY;
                    return resA;
                }
                else
                {
                    numRes = bX * bY;
                    return resB;
                }
            }
            else
            {
                numRes = 0;
                return "Error! Couldn't determine best resolution!";
            }
        }

        // Checks if a found matching image has better resolution than the original image
        public static bool CheckIfBetterResolution(string originalRes, string matchRes)
        {
            string[] strOrigParams = originalRes.Split(new char[] { '×', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] strMatchParams = matchRes.Split(new char[] { '×', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (strOrigParams.Length >= 2 && strMatchParams.Length >= 2)
            {
                int aX = int.Parse(strOrigParams[0]);
                int aY = int.Parse(strOrigParams[1]);

                int bX = int.Parse(strMatchParams[0]);
                int bY = int.Parse(strMatchParams[1]);

                if (aX * aY < bX * bY)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        // Determines maximum resolution from a list of matching images resolutions
        public static string DetermineBestResolution(List<string> stringsToCheck)
        {
            string bestString = string.Empty;
            long bestNum = 0;

            if (stringsToCheck.Count > 0)
            {
                if (stringsToCheck.Count == 1)
                {
                    bestString = stringsToCheck[0];
                }
                else
                {
                    for (int i = 0; i < stringsToCheck.Count - 1; i++)
                    {
                        long currNum;
                        string currString = CompareResolutions(stringsToCheck[i], stringsToCheck[i + 1], out currNum);
                        if (currString.Contains("Error"))
                        {
                            throw new ArgumentException("Bad resolutions!");
                        }
                        else
                        {
                            if (currNum >= bestNum)
                            {
                                bestNum = currNum;
                                bestString = currString;
                            }
                        }
                    }
                }
                return bestString;
            }
            else
            {
                throw new ArgumentException("No resolutions to check!");
            }
        }

        // call REST api for different websites
        public static dynamic GetRestImageUrl(string baseUrl, MatchSource source)
        {
            var client = new RestClient(baseUrl);
            var request = new RestRequest();
            request.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };
            var queryResult = client.Execute(request);

            dynamic finalUrl;
            switch (source)
            {
                case MatchSource.Danbooru:
                    finalUrl = JsonConvert.DeserializeObject<DanbooruRest.Root>(queryResult.Content);
                    break;
                case MatchSource.Gelbooru:
                    finalUrl = JsonConvert.DeserializeObject<GelbooruRest.Root>(queryResult.Content);
                    break;
                case MatchSource.Yandere:
                    finalUrl = JsonConvert.DeserializeObject<List<YandereRest.Root>>(queryResult.Content).FirstOrDefault();
                    break;
                case MatchSource.Konachan:
                    finalUrl = JsonConvert.DeserializeObject<List<KonachanRest.Root>>(queryResult.Content).FirstOrDefault();
                    break;
                case MatchSource.Zerochan:
                    finalUrl = JsonConvert.DeserializeObject<ZerochanRest.Root>(queryResult.Content);
                    break;
                case MatchSource.SankakuChannel:
                    finalUrl = JsonConvert.DeserializeObject<List<SankakuChannelRest.Root>>(queryResult.Content).FirstOrDefault();
                    break;
                default:
                    finalUrl = null;
                    break;
            }
            return finalUrl;
        }

        // Parses the post to find the url of the image
        public static string GetImageURL(string postURL, MatchSource source, out bool status)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string imageUrl = string.Empty;
                status = true;

                switch (source)
                {
                    case MatchSource.Danbooru:
                        string danbooruApiUrl = postURL + Constants.JsonReqParameterDanbooru;
                        DanbooruRest.Root danbooruRestObject = GetRestImageUrl(danbooruApiUrl, source);
                        imageUrl = danbooruRestObject.media_asset.variants.Find(o => o.type == "original").url;
                        break;
                    case MatchSource.Gelbooru:
                        string gelbooruApiUrl = postURL.Replace("page=post&s=view", "page=dapi&s=post&q=index") + Constants.JsonReqParameterGelbooru;
                        GelbooruRest.Root gelbooruRestObject = GetRestImageUrl(gelbooruApiUrl, source);
                        imageUrl = gelbooruRestObject.post[0].file_url;
                        break;
                    case MatchSource.Yandere:
                        string yandereApiUrl = postURL.Replace("post/show/", "post.json?tags=id:");
                        YandereRest.Root yandereRestObject = GetRestImageUrl(yandereApiUrl, source);
                        imageUrl = yandereRestObject.file_url;
                        break;
                    case MatchSource.Konachan:
                        string konachanApiUrl = postURL.Replace("post/show/", "post.json?tags=id:");
                        KonachanRest.Root konachanRestObject = GetRestImageUrl(konachanApiUrl, source);
                        imageUrl = konachanRestObject.file_url;
                        break;
                    case MatchSource.Zerochan:
                        string zerochanApiUrl = postURL + Constants.JsonReqParameterZerochan;
                        ZerochanRest.Root zerochanRestObject = GetRestImageUrl(zerochanApiUrl, source);
                        imageUrl = zerochanRestObject.full;
                        break;
                    case MatchSource.SankakuChannel:
                        string sankakuChannelApiUrl = Constants.JsonReqParameterSankakuChannel + new Uri(postURL).Segments.LastOrDefault();
                        SankakuChannelRest.Root sankakuChannelRestObject = GetRestImageUrl(sankakuChannelApiUrl, source);
                        imageUrl = sankakuChannelRestObject.file_url;
                        break;
                    case MatchSource.Eshuushuu:
                        HtmlWeb essWeb = new HtmlWeb();
                        HtmlDocument essHtmlDoc = essWeb.Load(postURL);
                        imageUrl = GetEshuushuuImage(essHtmlDoc);
                        break;
                    case MatchSource.AnimePictures:
                        HtmlWeb epWeb = new HtmlWeb();
                        HtmlDocument epHtmlDoc = epWeb.Load(postURL);
                        imageUrl = GetAnimePicturesImage(epHtmlDoc);
                        break;
                    default:
                        status = false;
                        imageUrl = "Unavailable";
                        break;
                }
                
                return imageUrl;
            }
            catch (Exception ex)
            {
                status = false;
                return ex.Message;
            }
        }

        // Parses e-shuushuu result to get best resolution link
        private static string GetEshuushuuImage(HtmlDocument htmlDoc)
        {
            string origImage = string.Empty;

            var nodes = htmlDoc.DocumentNode.SelectNodes(".//div[@class='thumb']/a[@class='thumb_image']");

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    origImage = node.GetAttributeValue("href", null);
                }
                if (string.IsNullOrEmpty(origImage))
                {
                    throw new ArgumentException("Failed to parse e-shuushuu url!");
                }
            }
            else
            {
                throw new ArgumentException("Error when parsing e-shuushuu url! Post was deleted/removed!");
            }
            origImage = string.Format("http://e-shuushuu.net/{0}", origImage);

            return origImage;
        }

        // Parses Anime-Pictures result to get best resolution link
        private static string GetAnimePicturesImage(HtmlDocument htmlDoc)
        {
            string origImage = string.Empty;

            var nodes = htmlDoc.DocumentNode.SelectNodes(".//div[@id='big_preview_cont']/a");

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    origImage = node.GetAttributeValue("href", null);
                }
                if (string.IsNullOrEmpty(origImage))
                {
                    throw new ArgumentException("Failed to parse anime-pictures image!");
                }
            }
            else
            {
                throw new ArgumentException("Error when parsing anime-pictures url! Post was deleted/removed!");
            }
            origImage = string.Format("https://anime-pictures.net{0}", origImage);

            return origImage;
        }

        // Extracts image name from url for saving on disk
        private static string GetImageNameFromURL(string directory, string URL)
        {
            string name;

            string currFolder = AppDomain.CurrentDomain.BaseDirectory;

            string[] tempStringArray = URL.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            name = tempStringArray.Last();

            tempStringArray = name.Split(new string[] { "?_", "?e=", "?if=" }, StringSplitOptions.RemoveEmptyEntries);
            name = tempStringArray.First();

            string ext = Path.GetExtension(name);
            name = name.Substring(0, name.LastIndexOf(ext));

            int allowedSymbols = Constants.Win32MaxPath - directory.Length - currFolder.Length - ext.Length - 4;

            if (name.Length > allowedSymbols)
            {
                name = name.Substring(0, allowedSymbols);
            }

            return name.Replace("%20", " ") + ext;
        }

        // Saves the image from the given url, to the selected path, with the given filename (extension is generated dynamically)
        public static void SaveImage(string directory, string imageURL, string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
            {
                imageName = GetImageNameFromURL(directory, imageURL);
            }
            else
            {
                throw new InvalidDataException("Image name is empty!");
            }

            using (WebClient webClient = new WebClient())
            {
                webClient.Headers.Add("user-agent", Constants.UserAgentHeader);
                byte[] data = null;

                try
                {
                    data = webClient.DownloadData(imageURL);
                }
                catch (WebException we)
                {
                    throw new Exception("Could not download image! " + we.Message);
                }

                try
                {
                    string imagePath = Path.Combine(directory, imageName);
                    Directory.CreateDirectory(directory);

                    File.WriteAllBytes(imagePath, data);
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not save image! " + ex.Message);
                }
            }
        }

        // Checks if the provided string is a valid URL
        public static bool CheckIfRealUrl(string urlToCheck)
        {
            return Uri.TryCreate(urlToCheck, UriKind.Absolute, out Uri uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        // Parses an image link directly copied from a browser
        public static string GetUrlFromClipboardImage(string htmlText)
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlText);
            string url;

            try
            {
                url = htmlDoc.DocumentNode.SelectSingleNode(".//body/img").GetAttributeValue("src", null);
            }
            catch (Exception ex)
            {
                url = null;
            }

            return url;
        }

        // Returns a list of all images in the folder, including subdirectories
        public static List<string> GetAllImagesFromFolder(string directory)
        {
            List<string> finalImages = new List<string>();

            List<string> tempImageList = Directory.GetFiles(directory, "*", SearchOption.AllDirectories).ToList();

            foreach (string image in tempImageList)
            {
                bool isImage = IsImage(image, out IFileType fileType);

                if (isImage)
                {
                    finalImages.Add(image);
                }
                else
                {
                    string errors = string.Empty;
                    errors += string.Format("Original image: {0}\n", image);
                    long fileSize = GetFileSize(image);
                    errors += string.Format("File size: {0}\n", GetSizeString(fileSize));
                    if (fileType != null)
                    {
                        errors += string.Format("File type: ({1}) {0}\n", fileType.Name, fileType.Extension);
                    }
                    errors += "Not an image!\n";
                    LogSearchErrors(errors);
                    LogNotChecked(image);
                }
            }

            return finalImages;
        }

        // Returns a list of all resources inside the resource folder
        public static List<string> GetResourcesUnder(string folder)
        {
            folder = folder.ToLower() + "/";

            var assembly = Assembly.GetCallingAssembly();
            var resourcesName = assembly.GetName().Name + ".g.resources";
            var stream = assembly.GetManifestResourceStream(resourcesName);
            var resourceReader = new ResourceReader(stream);

            var resources =
                from p in resourceReader.OfType<DictionaryEntry>()
                let theme = (string)p.Key
                where theme.StartsWith(folder)
                select theme.Substring(folder.Length);

            return resources.ToList();
        }

        // Serializes ImageData objects to XML
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

        // Deserializes ImageData object from XML
        public static List<ImageData> DeserializeImageList(string xmlDoc)
        {
            using (StringReader sr = new StringReader(xmlDoc))
            {
                XmlSerializer xSer = new XmlSerializer(typeof(List<ImageData>));

                var myObject = xSer.Deserialize(sr);

                return (List<ImageData>)myObject;
            }
        }

        // Saves a serialized list of images
        public static void SaveSerializedList(string xmlImageList)
        {
            File.WriteAllText(GetXmlFilename(), xmlImageList);
        }

        // Returns the current version of the application
        public static string GetAppVersion()
        {
            return $"MikuDownloader {Assembly.GetExecutingAssembly().GetName().Version}";
        }

        // Converts webp images to other formats
        public static string ConverWebpToJpg(string webpFilePath)
        {
            string dirPath = GetWebpConvertedDirectory();
            Directory.CreateDirectory(dirPath);
            string jpgFileName;

            // Load the webp file in an instance of Image
            using (var image = Aspose.Imaging.Image.Load(webpFilePath))
            {
                // Create an instance of JpegOptions
                string origExt = Path.GetExtension(webpFilePath);

                if (origExt.Contains(".jpg") || origExt.Contains(".jpeg"))
                {
                    var exportOptions = new Aspose.Imaging.ImageOptions.JpegOptions();
                    Directory.CreateDirectory(dirPath);
                    jpgFileName = Path.Combine(dirPath, Path.GetFileNameWithoutExtension(webpFilePath) + ".jpg");
                    image.Save(jpgFileName, exportOptions);
                }
                else if (origExt.Contains(".gif"))
                {
                    var exportOptions = new Aspose.Imaging.ImageOptions.GifOptions();
                    Directory.CreateDirectory(dirPath);
                    jpgFileName = Path.Combine(dirPath, Path.GetFileNameWithoutExtension(webpFilePath) + ".gif");
                    image.Save(jpgFileName, exportOptions);
                }
                else if (origExt.Contains(".png") || origExt.Contains(".webp"))
                {
                    var exportOptions = new Aspose.Imaging.ImageOptions.PngOptions();
                    Directory.CreateDirectory(dirPath);
                    jpgFileName = Path.Combine(dirPath, Path.GetFileNameWithoutExtension(webpFilePath) + ".png");
                    image.Save(jpgFileName, exportOptions);
                }
                else
                {
                    return null;
                }
            }

            return jpgFileName;
        }

        // Get the free remaining space of the current drive
        public static long GetFreeSpace()
        {
            string currDrive = Path.GetPathRoot(Assembly.GetEntryAssembly().Location);
            DriveInfo di = new DriveInfo(currDrive);
            return di.AvailableFreeSpace;
        }
    }
}
