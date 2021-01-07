using HtmlAgilityPack;
using MikuDownloader.image;
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
            string fullLog = $"{Constants.VeryLongLine}\nLogSearch {DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}\n{Constants.GeneralLogMessage}\n{logText}";
            File.AppendAllText(GetLogFileName(), fullLog);
        }

        // Logs data in the downloading function
        public static void LogDownload(string logText)
        {
            string fullLog = $"{Constants.VeryLongLine}\nLogDownload {DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}\n{logText}";
            File.AppendAllText(GetLogFileName(), fullLog);
        }

        // Logs data in the sorting function
        public static void LogSort(string logText)
        {
            string fullLog = $"{Constants.VeryLongLine}\nLogSort {DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}\n{logText}";
            File.AppendAllText(GetLogFileName(), fullLog);
        }

        // Gets log filename for main log
        public static string GetLogFileName()
        {
            var currTime = DateTime.Now.ToString("yyyyMMdd");
            var fileName = currTime + "_" + Constants.MainLogFileName;
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

        // Checks if the file is image
        public static bool IsImage(string filename)
        {
            if (filename.EndsWith(".txt") || filename.EndsWith(".mp3") || filename.EndsWith(".avi")
                || filename.EndsWith(".mp4") || filename.EndsWith(".mkv") || filename.EndsWith(".webm"))
            {
                return false;
            }

            using (FileStream stream = new FileStream(filename, FileMode.Open))
            {
                stream.Seek(0, SeekOrigin.Begin);

                List<string> jpg = new List<string> { "FF", "D8" };
                List<string> gif = new List<string> { "47", "49", "46" };
                List<string> png = new List<string> { "89", "50", "4E", "47", "0D", "0A", "1A", "0A" };

                List<List<string>> imgTypes = new List<List<string>> { jpg, gif, png };

                List<string> bytesIterated = new List<string>();

                for (int i = 0; i < 8; i++)
                {
                    string bit = stream.ReadByte().ToString("X2");
                    bytesIterated.Add(bit);

                    bool isImage = imgTypes.Any(img => !img.Except(bytesIterated).Any());
                    if (isImage)
                    {
                        return true;
                    }
                }
            }

            return false;
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

        // Parses the post to find the url of the image
        public static string GetImageURL(string postURL, MatchSource source, out bool status)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                HtmlWeb web = new HtmlWeb();
                HtmlDocument htmlDoc = web.Load(postURL);
                string imageUrl = string.Empty;
                status = true;

                switch (source)
                {
                    case MatchSource.Danbooru:
                        imageUrl = GetDanbooruImage(htmlDoc);
                        break;
                    case MatchSource.SankakuChannel:
                        imageUrl = GetSankakuImage(htmlDoc);
                        break;
                    case MatchSource.Gelbooru:
                        imageUrl = GetGelbooruImage(htmlDoc);
                        break;
                    case MatchSource.Yandere:
                        imageUrl = GetYandereImage(htmlDoc);
                        break;
                    case MatchSource.Konachan:
                        imageUrl = GetKonachanImage(htmlDoc);
                        break;
                    case MatchSource.Zerochan:
                        imageUrl = GetZerochanImage(htmlDoc);
                        break;
                    case MatchSource.Eshuushuu:
                        imageUrl = GetEshuushuuImage(htmlDoc);
                        break;
                    case MatchSource.AnimePictures:
                        imageUrl = GetAnimePicturesImage(htmlDoc);
                        break;
                    case MatchSource.TheAnimeGallery: // TODO: save session cookie before trying to download
                        imageUrl = GetTheAnimeGalleryImage(htmlDoc);
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

        // Parses danbooru result to get best resolution link
        private static string GetDanbooruImage(HtmlDocument htmlDoc)
        {
            string origImage = string.Empty;

            var nodes = htmlDoc.DocumentNode.SelectNodes(".//section[@id='post-information']/ul/li");
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    if (node.InnerHtml.Contains("Size"))
                    {
                        try
                        {
                            origImage = node.SelectSingleNode("a").GetAttributeValue("href", null);
                            break;
                        }
                        catch (Exception ex)
                        {
                            throw new ArgumentException(string.Format("Error when parsing danbooru url! Image was probably deleted or removed!\n{0}\n", ex.Message));
                        }
                    }
                }
                if (string.IsNullOrEmpty(origImage))
                {
                    throw new ArgumentException("Failed to parse danbooru url!");
                }
            }
            else
            {
                throw new ArgumentException("Error when parsing danbooru url! Image was probably deleted or removed!");
            }

            return origImage;
        }

        // Parses sankaku result to get best resolution link
        private static string GetSankakuImage(HtmlDocument htmlDoc)
        {
            string origImage = string.Empty;

            var deleted = htmlDoc.DocumentNode.SelectNodes(".//div[@class='status-notice deleted']");
            var nodes = htmlDoc.DocumentNode.SelectNodes(".//div[@id='stats']/ul/li");

            if (nodes != null && deleted == null)
            {
                foreach (var node in nodes)
                {
                    if (node.InnerHtml.Contains("Original:"))
                    {
                        origImage = node.SelectSingleNode("a").GetAttributeValue("href", null);
                    }
                }
                if (string.IsNullOrEmpty(origImage))
                {
                    throw new ArgumentException("Failed to parse sankaku url!");
                }
            }
            else
            {
                throw new ArgumentException("Error when parsing sankaku url! Post was deleted/removed!");
            }
            origImage = string.Format("https:{0}", origImage);
            origImage = origImage.Replace("&amp;", "&");

            return origImage;
        }

        // Parses danbooru result to get best resolution link
        private static string GetGelbooruImage(HtmlDocument htmlDoc)
        {
            string origImage = string.Empty;

            var nodes = htmlDoc.DocumentNode.SelectNodes(".//div/li/a");

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    if (node.InnerHtml.Equals("Original image"))
                    {
                        origImage = node.GetAttributeValue("href", null);
                        break;
                    }
                }
                if (string.IsNullOrEmpty(origImage))
                {
                    throw new ArgumentException("Failed to parse gelbooru url!");
                }
            }
            else
            {
                throw new ArgumentException("Error when parsing gelbooru url! Post was deleted/removed!");
            }

            return origImage;
        }

        // Parses yande.re result to get best resolution link
        private static string GetYandereImage(HtmlDocument htmlDoc)
        {
            string origImage = string.Empty;

            var nodes = htmlDoc.DocumentNode.SelectNodes(".//div/ul/li/a[@class='original-file-unchanged' or @class='original-file-changed']");

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    origImage = node.GetAttributeValue("href", null);
                }
                if (string.IsNullOrEmpty(origImage))
                {
                    throw new ArgumentException("Failed to parse yande.re url!");
                }
            }
            else
            {
                throw new ArgumentException("Error when parsing yande.re url! Image was probably deleted");
            }

            return origImage;
        }

        // Parses konachan result to get best resolution link
        private static string GetKonachanImage(HtmlDocument htmlDoc)
        {
            string origImage = string.Empty;

            var nodes = htmlDoc.DocumentNode.SelectNodes(".//div/ul/li/a[@class='original-file-unchanged' or @class='original-file-changed']");

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    origImage = node.GetAttributeValue("href", null);
                }
                if (string.IsNullOrEmpty(origImage))
                {
                    throw new ArgumentException("Failed to parse konachan url!");
                }
            }
            else
            {
                throw new ArgumentException("Error when parsing konachan url! Image was probably deleted");
            }

            return origImage;
        }

        // Parses zerochan result to get best resolution link
        private static string GetZerochanImage(HtmlDocument htmlDoc)
        {
            string origImage = string.Empty;

            var nodes = htmlDoc.DocumentNode.SelectNodes(".//div[@id='content']/div/a");

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    origImage = node.GetAttributeValue("href", null);
                }
                if (string.IsNullOrEmpty(origImage))
                {
                    throw new ArgumentException("Failed to parse zerochan url!");
                }
            }
            else
            {
                throw new ArgumentException("Error when parsing zerochan url! Image was probably deleted");
            }

            return origImage;
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

        // Parses The Anime Gallery result to get best resolution link
        private static string GetTheAnimeGalleryImage(HtmlDocument htmlDoc)
        {
            string origImage = string.Empty;

            var nodes = htmlDoc.DocumentNode.SelectNodes(".//div[@class='download']/a");

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    origImage = node.GetAttributeValue("href", null);
                }
                if (string.IsNullOrEmpty(origImage))
                {
                    throw new ArgumentException("Failed to parse the anime gallery url!");
                }
            }
            else
            {
                throw new ArgumentException("Error when parsing the anime gallery url! Post was deleted/removed!");

            }
            origImage = string.Format("www.theanimegallery.com{0}", origImage);

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
                if (IsImage(image))
                {
                    finalImages.Add(image);
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
    }
}
