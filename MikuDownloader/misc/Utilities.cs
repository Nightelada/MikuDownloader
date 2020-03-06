using HtmlAgilityPack;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Windows.Media.Imaging;

namespace MikuDownloader.misc
{
    public static class Utilities
    {
        // gets log timestamp for main log
        public static string GetLogTimestamp()
        {
            var currTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            return String.Format("{0}\nAttempting to generate list of matching images... : {1}\n", Constants.VeryLongLine, currTime);
        }

        // gets log filename for main log
        public static string GetLogFileName()
        {
            var currTime = DateTime.Now.ToString("yyyyMMdd");
            return currTime + "_" + Constants.MainLogFileName;
        }

        // gets log filename for not downloaded log
        public static string GetNotDownloadedFilename()
        {
            var currTime = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return currTime + "_" + Constants.NotDownloadedFilename;
        }

        // gets the name of the Directory in which downloaded files go in
        public static string GetMainDownloadDirectory()
        {
            var currTime = DateTime.Now.ToString("yyyyMMdd");
            var downloadDir = Path.Combine(Constants.MainDownloadDirectory, currTime);

            return downloadDir;
        }

        // browses for .txt file and returns full path
        public static string BrowseFile(string filter)
        {
            string filename = string.Empty;

            OpenFileDialog dlg = new OpenFileDialog()
            {
                AddExtension = true,
                Filter = filter
            };

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                filename = dlg.FileName;
            }

            return filename;
        }

        // browses for directory with images and returns full path
        public static string BrowseDirectory(string filter)
        {
            string directory = string.Empty;

            VistaFolderBrowserDialog dlg = new VistaFolderBrowserDialog();

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                directory = dlg.SelectedPath;
            }

            return directory;
        }

        // parses a file full of URLs to a list
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
                bool result = Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                if (result)
                {
                    finalURLs.Add(url);
                }
            }
            return finalURLs;
        }

        // checks if the file is image
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

        // return the resolution of an image from the file system
        public static string GetResolution(string filename)
        {
            string resolution = string.Empty;

            using (var imageStream = File.OpenRead(filename))
            {
                var decoder = BitmapDecoder.Create(imageStream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.Default);
                var height = decoder.Frames[0].PixelHeight;
                var width = decoder.Frames[0].PixelWidth;
                resolution = String.Format("{0}{1}{2}", width, '×', height);
            }
            return resolution;
        }

        // compares 2 different resolutions
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

        // check if match has better resolution than original image
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

        // determines maximum resolution from a list of resolutions
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

        // parses the post to find the url of the image
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
                        imageUrl = SaveDanbooruImage(htmlDoc);
                        break;
                    case MatchSource.SankakuChannel:
                        imageUrl = SaveSankakuImage(htmlDoc);
                        break;
                    case MatchSource.Gelbooru:
                        imageUrl = SaveGelbooruImage(htmlDoc);
                        break;
                    case MatchSource.Yandere:
                        imageUrl = SaveYandereImage(htmlDoc);
                        break;
                    case MatchSource.Konachan:
                        imageUrl = SaveKonachanImage(htmlDoc);
                        break;
                    case MatchSource.Zerochan:
                        imageUrl = SaveZerochanImage(htmlDoc);
                        break;
                    case MatchSource.Eshuushuu:
                        imageUrl = SaveEshuushuuImage(htmlDoc);
                        break;
                    case MatchSource.AnimePictures:
                        imageUrl = SaveAnimePicturesImage(htmlDoc);
                        break;
                    case MatchSource.TheAnimeGallery: // TODO: save session cookie before trying to download
                        imageUrl = SaveTheAnimeGalleryImage(htmlDoc);
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

        // parses danbooru result to get best res link without searching many times
        private static string SaveDanbooruImage(HtmlDocument htmlDoc)
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
                            throw new ArgumentException("Error when parsing danbooru url! Image was probably deleted or removed!\n");
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

        // parses sankaku result to get best res link without searching many times
        private static string SaveSankakuImage(HtmlDocument htmlDoc)
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
            origImage = String.Format("https:{0}", origImage);
            origImage = origImage.Replace("&amp;", "&");

            return origImage;
        }

        // parses danbooru result to get best res link without searching many times
        private static string SaveGelbooruImage(HtmlDocument htmlDoc)
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

        // parses yande.re result to get best res link without searching many times
        private static string SaveYandereImage(HtmlDocument htmlDoc)
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

        // parses yande.re result to get best res link without searching many times
        private static string SaveKonachanImage(HtmlDocument htmlDoc)
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

        // parses zerochan result to get best res link without searching many times
        private static string SaveZerochanImage(HtmlDocument htmlDoc)
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

        // parses e-shuushuu result to get best res link without searching many times
        private static string SaveEshuushuuImage(HtmlDocument htmlDoc)
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
            origImage = String.Format("http://e-shuushuu.net/{0}", origImage);

            return origImage;
        }

        // parses Anime-Pictures result to get best res link without searching many times
        private static string SaveAnimePicturesImage(HtmlDocument htmlDoc)
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
            origImage = String.Format("https://anime-pictures.net{0}", origImage);

            return origImage;
        }

        // parses The Anime Gallery result to get best res link without searching many times
        private static string SaveTheAnimeGalleryImage(HtmlDocument htmlDoc)
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
            origImage = String.Format("www.theanimegallery.com{0}", origImage);

            return origImage;
        }

        // extracts image name from url for saving on disk
        private static string GetImageNameFromURL(string directory, string URL)
        {
            string name = string.Empty;

            string currFolder = AppDomain.CurrentDomain.BaseDirectory;

            int allowedSymbols = Constants.Win32MaxPath - directory.Length - currFolder.Length - 4;

            string[] tempStringArray = URL.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            name = tempStringArray.Last();

            if (name.Contains(".jpg"))
            {
                name = name.Remove(name.IndexOf(".jpg"));
            }
            else if (name.Contains(".png"))
            {
                name = name.Remove(name.IndexOf(".png"));
            }
            else if (name.Contains(".gif"))
            {
                name = name.Remove(name.IndexOf(".gif"));
            }
            else if (name.Contains(".jpeg"))
            {
                name = name.Remove(name.IndexOf(".jpeg"));
            }

            if (name.Length > allowedSymbols)
            {
                return name.Substring(0, allowedSymbols);
            }
            else
            {
                return name;
            }
        }

        // saves the image from the given url, to the selected path, with the given filename (extension is generated dynamically)
        public static string SaveImage(string directory, string imageURL, string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
            {
                imageName = GetImageNameFromURL(directory, imageURL);
                imageName = imageName.Replace("%20", " ");
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
                    throw new Exception("Could not download image!" + we.Message);
                }

                using (MemoryStream mem = new MemoryStream(data))
                {
                    using (var yourImage = System.Drawing.Image.FromStream(mem))
                    {
                        ImageFormat iff = null;
                        string filename = string.Empty;

                        if (ImageFormat.Jpeg.Equals(yourImage.RawFormat))
                        {
                            iff = ImageFormat.Jpeg;
                            filename = imageName + ".jpg";
                        }
                        else if (ImageFormat.Png.Equals(yourImage.RawFormat))
                        {
                            iff = ImageFormat.Png;
                            filename = imageName + ".png";
                        }
                        else if (ImageFormat.Gif.Equals(yourImage.RawFormat))
                        {
                            iff = ImageFormat.Gif;
                            filename = imageName + ".gif";
                        }
                        else
                        {
                            throw new ArgumentException("Invalid image hash detected! Make sure the image to download is in format of png, jpg or gif!");
                        }

                        string imagePath = Path.Combine(directory, filename);

                        Directory.CreateDirectory(directory);

                        yourImage.Save(imagePath, iff);

                        return imagePath;
                    }
                }
            }
        }

        public static string GetUrlFromClipboardImage(string htmlText)
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlText);

            string url = htmlDoc.DocumentNode.SelectSingleNode(".//body/img").GetAttributeValue("src",null);

            return url;
        }

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

        public static string[] GetResourcesUnder(string folder)
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

            return resources.ToArray();
        }
    }
}
