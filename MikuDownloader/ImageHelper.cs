using HtmlAgilityPack;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MikuDownloader
{
    public static class ImageHelper
    {
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

        // determines maximum resolution from a list of resolutions
        private static string DetermineBestResolution(List<string> stringsToCheck)
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

        // makes HTML request for image reverse search using an image file from the computer
        public async static Task<Tuple<HtmlDocument, string>> GetResponseFromFile(string imagePath)
        {
            var httpResponse = await ReverseSearchFileHTTP(imagePath);
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(httpResponse);

            Tuple<HtmlDocument, string> tempTuple = new Tuple<HtmlDocument, string>(htmlDoc, imagePath);

            return tempTuple;
        }

        // makes HTML request for image reverse search using an existing image URL
        public async static Task<Tuple<HtmlDocument, string>> GetResponseFromURL(string imageURL)
        {
            var httpResponse = await ReverseSearchURLHTTP(imageURL);
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(httpResponse);

            Tuple<HtmlDocument, string> tempTuple = new Tuple<HtmlDocument, string>(htmlDoc, imageURL);

            return tempTuple;
        }

        // MAIN FUNC
        // this function parses an HTML after reverse-searching from IQDB
        public static List<ImageDetails> ReverseImageSearch(HtmlDocument htmlDoc, string originalImage, out string status)
        {
            string response = string.Empty;

            response = String.Format("Original image: {0}\n", originalImage);

            // check if error when loading image
            var err = htmlDoc.DocumentNode.SelectSingleNode(".//div[@class='err']");
            if (err != null)
            {
                response += String.Format("Image search failed!\n{0}\nSupported file types are JPEG, PNG and GIF\nMaximum file size: 8192 KB\nMaximum image dimensions: 7500x7500\n",
                    err.InnerText);
                status = response;
                return null;
            }

            // get all nodes containing matches info
            var nodes = htmlDoc.DocumentNode.SelectNodes(".//div/table[tr/th]/tr/th");

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    if (node.InnerText.Contains("Best match"))
                    {
                        response += "Best image found!\n";
                        break;
                    }
                    // if no best match is found - return alternative links
                    else if (node.InnerText.Contains("No relevant"))
                    {
                        if (File.Exists(originalImage))
                        {
                            response += String.Format("No Relevant images found!\nCheck below URLs:\n{0}\n{1}\n{2}\n", Constants.SauceNAOMain, Constants.TinEyeMain, Constants.GoogleMain);

                            status = response;
                            return null;
                        }
                        else
                        {
                            if (!originalImage.Contains(Constants.FacebookTempURL))
                            {
                                var sauceNao = String.Format("{0}{1}", Constants.SauceNAO, originalImage);
                                var tinEye = String.Format("{0}{1}", Constants.TinEye, originalImage);
                                var google = String.Format("{0}{1}&safe=off", Constants.Google, originalImage);
                                response += String.Format("No Relevant images found!\nCheck below URLs:\n{0}\n{1}\n{2}\n", sauceNao, tinEye, google);
                            }
                            else
                            {
                                response += String.Format("No Relevant images found!\nCheck below URLs:\n{0}\n{1}\n{2}\n", Constants.SauceNAOMain, Constants.TinEyeMain, Constants.GoogleMain);
                            }
                            status = response;
                            return null;
                        }
                    }
                }
            }
            else
            {
                response += "Image search failed! Unable to parse document for matches!\n";
                status = response;
                return null;
            }

            // get all nodes containing matches
            var matchNodes = htmlDoc.DocumentNode.SelectNodes(".//div/table[tr/th]");

            List<string> resolutions = new List<string>();
            List<ImageDetails> imagesList = new List<ImageDetails>();
            List<ImageDetails> bestImages = new List<ImageDetails>();

            if (matchNodes != null)
            {
                foreach (var match in matchNodes)
                {
                    // get info for each match
                    if (match.OuterHtml.Contains("match"))
                    {
                        var postId = match.SelectSingleNode("tr/td[@class='image']/a").GetAttributeValue("href", null);
                        var imageLink = match.SelectSingleNode("tr/td[@class='image']/a/img").GetAttributeValue("src", null);

                        var tags = match.SelectSingleNode("tr/td[@class='image']/a/img").GetAttributeValue("alt", null);
                        var resolution = match.SelectSingleNode("(tr/td)[3]").InnerText;

                        var similarity = match.SelectSingleNode("(tr/td)[4]").InnerText;
                        var matchType = match.SelectSingleNode("(tr/th)[1]").InnerText;

                        if (imageLink != null && postId != null && tags != null && resolution != null)
                        {
                            ImageDetails tempImg = new ImageDetails(postId, imageLink, tags, resolution, similarity, matchType);
                            tempImg.OriginalURL = originalImage;
                            imagesList.Add(tempImg);

                            if (!tempImg.Source.Equals("Unavailable"))
                            {
                                if (!resolutions.Contains(tempImg.Resolution))
                                {
                                    resolutions.Add(tempImg.Resolution);
                                }
                            }
                            else
                            {
                                response += "Image search failed! The matches found could not be parsed!\n";
                                status = response;
                                return null;
                            }
                        }
                    }
                }

                try
                {
                    string bestResoltuion = DetermineBestResolution(resolutions);
                    response += String.Format("Best resolution found is: {0}\n", bestResoltuion);

                    if (imagesList.Count > 0)
                    {
                        foreach (ImageDetails image in imagesList)
                        {
                            if (image.Resolution.Equals(bestResoltuion))
                            {
                                bestImages.Add(image);
                                response += String.Format("{0}: {1} Similarity: {2}\n", image.MatchType.ToString(), image.PostURL, image.Similarity);
                            }
                            if (image.Resolution.Equals("Unavailable"))
                            {
                                response += String.Format("Weird resolution! Check: {0}\n", image.PostURL);
                            }
                        }
                    }
                    else
                    {
                        response += "Image search failed! You should not see this code!\n";
                        status = response;
                        return null;
                    }
                }
                catch (ArgumentException ae)
                {
                    response += String.Format("Could not determine best resolution! {0}\n", ae.Message);
                }
            }
            else
            {
                response += "Failed to parse documents after finding matches!\n";

                status = response;
                return null;
            }

            status = response;
            return bestImages;
        }

        // parses sankaku result to get best res link without searching many times
        private static void SaveSankakuImage(string folderPath, string postURL, string imageName)
        {
            HtmlWeb web = new HtmlWeb();

            HtmlDocument htmlDoc = web.Load(postURL);

            string origImage = string.Empty;

            var nodes = htmlDoc.DocumentNode.SelectNodes(".//div[@id='post-content']");

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    origImage = node.SelectSingleNode("a/img").GetAttributeValue("src", null);
                }
                if (string.IsNullOrEmpty(origImage))
                {
                    throw new ArgumentException("Failed to parse sankaku image!");
                }
            }
            else
            {
                throw new ArgumentException("Error when parsing sankaku image! Post was deleted/removed!");
            }
            origImage = String.Format("https:{0}", origImage);
            origImage = origImage.Replace("&amp;","&");

            SaveImage(folderPath, origImage, imageName);
        }

        // parses danbooru result to get best res link without searching many times
        private static void SaveDanbooruImage(string folderPath, string postURL, string imageName)
        {
            HtmlWeb web = new HtmlWeb();

            HtmlDocument htmlDoc = web.Load(postURL);

            string origImage = string.Empty;

            var nodes = htmlDoc.DocumentNode.SelectNodes(".//section[@id='post-information']/ul/li");

            foreach (var node in nodes)
            {
                if (node.InnerHtml.Contains("Size"))
                {
                    origImage = node.SelectSingleNode("a").GetAttributeValue("href", null);
                    break;
                }
            }
            if (string.IsNullOrEmpty(origImage))
            {
                throw new ArgumentException("Failed to parse Danbooru image!");
            }
            origImage = String.Format("https://danbooru.donmai.us{0}", origImage);

            SaveImage(folderPath, origImage, imageName);
        }

        // parses danbooru result to get best res link without searching many times
        private static void SaveGelbooruImage(string folderPath, string postURL, string imageName)
        {
            HtmlWeb web = new HtmlWeb();

            HtmlDocument htmlDoc = web.Load(postURL);

            string origImage = string.Empty;

            var nodes = htmlDoc.DocumentNode.SelectNodes(".//div/li/a");

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
                throw new ArgumentException("Failed to parse Gelbooru image!");
            }

            SaveImage(folderPath, origImage, imageName);
        }

        // parses yande.re result to get best res link without searching many times
        private static void SaveYandereImage(string folderPath, string postURL, string imageName)
        {
            HtmlWeb web = new HtmlWeb();

            HtmlDocument htmlDoc = web.Load(postURL);

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
                    throw new ArgumentException("Failed to parse yande.re image!");
                }
            }
            else
            {
                throw new ArgumentException("Error when parsing yande.re image! Image was probably deleted");
            }

            SaveImage(folderPath, origImage, imageName);
        }

        // parses zerochan result to get best res link without searching many times
        private static void SaveZerochanImage(string folderPath, string postURL, string imageName)
        {
            HtmlWeb web = new HtmlWeb();

            HtmlDocument htmlDoc = web.Load(postURL);

            string origImage = string.Empty;

            var nodes = htmlDoc.DocumentNode.SelectNodes(".//div[@id='content']/div/img");

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    origImage = node.GetAttributeValue("src", null);
                }
                if (string.IsNullOrEmpty(origImage))
                {
                    throw new ArgumentException("Failed to parse zerochan image!");
                }
            }
            else
            {
                throw new ArgumentException("Error when parsing zerochan image! Image was probably deleted");
            }

            SaveImage(folderPath, origImage, imageName);
        }

        // parses e-shuushuu result to get best res link without searching many times
        private static void SaveEshuushuuImage(string folderPath, string postURL, string imageName)
        {
            HtmlWeb web = new HtmlWeb();

            HtmlDocument htmlDoc = web.Load(postURL);

            string origImage = string.Empty;

            var nodes = htmlDoc.DocumentNode.SelectNodes(".//div[@class='thumb']/a[@class='thumb_image']");

            foreach (var node in nodes)
            {
                origImage = node.GetAttributeValue("href", null);
            }
            if (string.IsNullOrEmpty(origImage))
            {
                throw new ArgumentException("Failed to parse E-shuushuu image!");
            }
            origImage = String.Format("http://e-shuushuu.net/{0}", origImage);

            SaveImage(folderPath, origImage, imageName);
        }

        // parses Anime-Pictures result to get best res link without searching many times
        private static void SaveAnimePicturesImage(string folderPath, string postURL, string imageName)
        {
            HtmlWeb web = new HtmlWeb();

            HtmlDocument htmlDoc = web.Load(postURL);

            string origImage = string.Empty;

            var nodes = htmlDoc.DocumentNode.SelectNodes(".//div[@id='content']/div/div[@id='big_preview_cont']/a");

            foreach (var node in nodes)
            {
                origImage = node.GetAttributeValue("href", null);
            }
            if (string.IsNullOrEmpty(origImage))
            {
                throw new ArgumentException("Failed to parse Anime-Pictures image!");
            }
            origImage = String.Format("https://anime-pictures.net{0}", origImage);

            SaveImage(folderPath, origImage, imageName);
        }

        // parses The Anime Gallery result to get best res link without searching many times
        private static void SaveTheAnimeGalleryImage(string folderPath, string postURL, string imageName)
        {
            HtmlWeb web = new HtmlWeb();

            HtmlDocument htmlDoc = web.Load(postURL);

            string origImage = string.Empty;

            var nodes = htmlDoc.DocumentNode.SelectNodes(".//div[@class='download']/a");

            foreach (var node in nodes)
            {
                origImage = node.GetAttributeValue("href", null);
            }
            if (string.IsNullOrEmpty(origImage))
            {
                throw new ArgumentException("Failed to parse The Anime Gallery image!");
            }
            origImage = String.Format("www.theanimegallery.com{0}", origImage);

            SaveImage(folderPath, origImage, imageName);
        }

        // downloads best image from a set of sites provided
        public static void DownloadBestImage(List<ImageDetails> images, string fileName = "")
        {
            var currTime = DateTime.Now.ToString("yyyyMMdd");
            var folderPath = Path.Combine(Constants.MainDownloadDirectory, currTime.ToString());

            // somehow try to distinguish files with differences! (edge tracing)
            if (images != null && images.Count > 0)
            {
                bool sankakuFlag = false;
                bool danbooruFlag = false;
                bool gelbooruFlag = false;
                bool yandereFlag = false;
                bool eshuushuuFlag = false;


                if (images.Exists(item => item.MatchSource == MatchSource.SankakuChannel))
                {
                    sankakuFlag = true;
                }
                if (images.Exists(item => item.MatchSource == MatchSource.Danbooru))
                {
                    danbooruFlag = true;
                }
                if (images.Exists(item => item.MatchSource == MatchSource.Gelbooru))
                {
                    gelbooruFlag = true;
                }
                if (images.Exists(item => item.MatchSource == MatchSource.Yandere))
                {
                    yandereFlag = true;
                }
                if (images.Exists(item => item.MatchSource == MatchSource.Eshuushuu))
                {
                    eshuushuuFlag = true;
                }
                
                // goes through all images in collection - the source is danbooru,sankaku or gelbooru - it downloads all available images, considering priority sites
                // if the source is another one it just downloads the first in the list (if there are more than one)
                foreach (ImageDetails image in images)
                {
                    try
                    {
                        string imageName;
                        if (String.IsNullOrEmpty(fileName))
                        {
                            imageName = image.ImageName;
                        }
                        else
                        {
                            imageName = fileName;
                        }

                        if (image.MatchSource == MatchSource.Danbooru)
                        {
                            SaveDanbooruImage(folderPath, image.PostURL, imageName);
                        }
                        else if (image.MatchSource == MatchSource.SankakuChannel && !danbooruFlag)
                        {
                            SaveSankakuImage(folderPath, image.PostURL, imageName);
                        }
                        else if (image.MatchSource == MatchSource.Gelbooru && !sankakuFlag && !danbooruFlag)
                        {
                            SaveGelbooruImage(folderPath, image.PostURL, imageName);
                        }
                        else if (image.MatchSource == MatchSource.Yandere && !gelbooruFlag && !danbooruFlag && !sankakuFlag)
                        {
                            SaveYandereImage(folderPath, image.PostURL, imageName);
                            break;
                        }
                        else if (image.MatchSource == MatchSource.Eshuushuu && !yandereFlag && !gelbooruFlag && !danbooruFlag && !sankakuFlag)
                        {
                            SaveEshuushuuImage(folderPath, image.PostURL, imageName);
                            break;
                        }
                        else if (!eshuushuuFlag && !yandereFlag && !gelbooruFlag && !danbooruFlag && !sankakuFlag)
                        {
                            if (image.MatchSource == MatchSource.Zerochan)
                            {
                                SaveZerochanImage(folderPath, image.PostURL, imageName);
                            }
                            //probbaly works
                            else if (image.MatchSource == MatchSource.AnimePictures)
                            {
                                SaveAnimePicturesImage(folderPath, image.PostURL, imageName);
                            }
                            // doesn't work
                            else if (image.MatchSource == MatchSource.TheAnimeGallery)
                            {
                                //SaveTheAnimeGalleryImage(folderPath, image.PostURL, imageName);
                                throw new ArgumentException(Constants.TheAnimeGalleryErrorMessage);
                            }
                            else
                            {
                                throw new ArgumentException("Uknown parse source site!\n");
                            }
                            break;
                        }
                        Thread.Sleep(1000);//anti-ban
                    }
                    catch (Exception ex)
                    {
                        if (!ex.Message.Contains("Error when parsing sankaku image! Post was deleted/removed!"))
                        {
                            throw new ArgumentException(ex.Message);
                        }
                    }
                }
            }
            else
            {
                throw new ArgumentException("No images found in collection! Pages were not parsed correctly! Download picture manually from links!\n");
            }
        }
        
        // saves the image from the given url, to the selected path, with the given filename (extension is generated dynamically)
        public static string SaveImage(string directory, string imageURL, string imageName)
        {
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

        // reads links from file, uploads them to imgur, and downloads files 1 by 1
        public async static Task<string> DownloadBulkImages(List<string> URLsToDownload)
        {
            foreach (string imageURL in URLsToDownload)
            {
                string status = string.Empty;

                try
                {
                    var responseTuple = await GetResponseFromURL(imageURL);

                    var imageList = ReverseImageSearch(responseTuple.Item1, responseTuple.Item2, out status);

                    DownloadBestImage(imageList);
                    status += "Successfully downoaded image!\n";

                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        status += String.Format("Failed to download image!\n{0}\n{1}\n", ex.Message, ex.InnerException.Message);
                    }
                    else
                    {
                        status += String.Format("Failed to download image!\n{0}\n", ex.Message);
                    }
                }
                finally
                {
                    File.AppendAllText(GetLogFileName(), GetLogTimestamp() + status);
                }
            }

            return "Successfull!";
        }

        // reads links from file, uploads them to imgur, and downloads files 1 by 1
        public async static Task<string> DownloadBulkImagesFromFolder(List<string> imagesToDownload, bool? keepFilenames = true, bool? ignoreResolution = false)
        {
            string secondaryLog = String.Format("Begin checking of files for folder: {0}\n", Path.GetDirectoryName(imagesToDownload.First()));
            string totalDownloadedImages = string.Empty;

            foreach (string file in imagesToDownload)
            {
                string status = string.Empty;
                secondaryLog += String.Format("Checking image for: {0}\n", Path.GetFileName(file));

                if (IsImage(file))
                {
                    try
                    {
                        var responseTuple = await GetResponseFromFile(file);

                        var imageList = ReverseImageSearch(responseTuple.Item1, responseTuple.Item2, out status);

                        if (imageList != null && imageList.Count > 0)
                        {
                            string fileResolution = GetResolution(file);
                            string matchResolution = imageList.First().Resolution;

                            if (!matchResolution.Equals(fileResolution) || ignoreResolution == true)
                            {
                                string origImageName;
                                if (keepFilenames == true)
                                {
                                    origImageName = Path.GetFileNameWithoutExtension(file);
                                }
                                else
                                {
                                    origImageName = String.Empty;
                                }
                                DownloadBestImage(imageList, origImageName);
                                status += "Successfully downoaded image!\n";
                                secondaryLog += String.Format("Image with better resolution was found or resolution is being ignored!\nOriginal res: {0} - new res: {1}\n", fileResolution, matchResolution);
                                totalDownloadedImages += Path.GetFileName(file) + "\n";
                            }
                            else
                            {
                                status += "Image in folder has same resolution! Image was not downloaded!\n";
                                secondaryLog += "Image in folder has same resolution!\n";
                            }
                            Thread.Sleep(1000); //anti-ban
                        }
                        else
                        {
                            secondaryLog += "No matches were found for the image!\n";
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.InnerException != null)
                        {
                            status += String.Format("Failed to download image!\n{0}\n{1}\n", ex.Message, ex.InnerException.Message);
                        }
                        else
                        {
                            status += String.Format("Failed to download image!\n{0}\n", ex.Message);
                        }
                        secondaryLog += "Something went wrong when downloading image!\n";
                    }
                    finally
                    {
                        File.AppendAllText(GetLogFileName(), GetLogTimestamp() + status);
                    }
                }
                else
                {
                    secondaryLog += String.Format("File: {0} is not an image file and was not checked!\n", Path.GetFileName(file));
                }
                secondaryLog += Constants.VeryLongLine + "\n";
            }
            File.AppendAllText(GetSecondaryLogFileName(), "All downloaded images:\n" + totalDownloadedImages + Constants.VeryLongLine + "\n" + secondaryLog);

            return "Successfull!";
        }

        // loggin purposes
        public static string GetLogTimestamp()
        {
            var currTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            return String.Format("{0}\nAttempting to generate list of matching images... : {1}\n", Constants.VeryLongLine, currTime);
        }

        // -||-
        public static string GetLogFileName()
        {
            var currTime = DateTime.Now.ToString("yyyyMMdd");
            return currTime + "_" + Constants.MainLogFileName;
        }

        // -||-
        public static string GetSecondaryLogFileName()
        {
            var currTime = DateTime.Now.ToString("yyyyMMdd");
            return currTime + "_" + Constants.SecondaryLogFileName;
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

            OpenFileDialog dlg = new OpenFileDialog()
            {
                AddExtension = true,
                Filter = filter
            };

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                directory = Path.GetDirectoryName(dlg.FileName);
            }

            return directory;
        }

        // parses a file full of URLs to a list
        public static List<string> ParseURLs(string filepath)
        {
            List<string> URLs = new List<string>();

            if (!string.IsNullOrEmpty(filepath))
            {
                URLs = File.ReadAllLines(filepath).ToList();
            }

            return URLs;
        }

        // searches for image from file
        public static async Task<string> ReverseSearchFileHTTP(string filePath)
        {
            FileStream fs = new FileStream(filePath, FileMode.Open);

            HttpContent stringContent = new StringContent("8388608");
            HttpContent fileStreamContent = new StreamContent(fs);

            using (var client = new HttpClient())
            {
                using (var formData = new MultipartFormDataContent())
                {
                    formData.Add(stringContent, "MAX_FILE_SIZE");
                    for (int i = 1; i <= 13; i++)
                    {
                        /*
                         * 1 - danbooru
                         * 2 - konachan
                         * 3 - yande.re
                         * 4 - gelbooru
                         * 5 - sankakucomplex
                         * 6 - e-shuushuu
                         * 10 - the anime gallery
                         * 11 - zerochan
                         * 12 - anime-pictures
                         */
                        if (new[] { 7, 8, 9, 12 }.Contains(i))
                        {
                            continue;
                        }
                        formData.Add(new StringContent(i.ToString()), "service[]");
                    }

                    formData.Add(fileStreamContent, "file", "image.jpg");

                    var response = await client.PostAsync(Constants.IQDB, formData);
                    var contents = await response.Content.ReadAsStringAsync();

                    return contents;
                }
            }
        }

        // searches for image from URL
        public static async Task<string> ReverseSearchURLHTTP(string fileUrl)
        {
            HttpContent stringContent = new StringContent(fileUrl);

            using (var client = new HttpClient())
            {
                using (var formData = new MultipartFormDataContent())
                {
                    for (int i = 1; i <= 13; i++)
                    {
                        /*
                         * 1 - danbooru
                         * 2 - konachan
                         * 3 - yande.re
                         * 4 - gelbooru
                         * 5 - sankakucomplex
                         * 6 - e-shuushuu
                         * 10 - the anime gallery
                         * 11 - zerochan
                         * 12 - anime-pictures
                         */
                        if (new[] { 7, 8, 9, 12 }.Contains(i))
                        {
                            continue;
                        }

                        formData.Add(new StringContent(i.ToString()), "service[]");
                    }
                    formData.Add(stringContent, "url");
                    // possible checks for error codes and shit
                    var response = await client.PostAsync(Constants.IQDB, formData);
                    var contents = await response.Content.ReadAsStringAsync();

                    return contents;
                }
            }
        }

        // checks if the file is image
        private static bool IsImage(string filename)
        {
            if (filename.Contains(".txt") || filename.Contains(".mp3") || filename.Contains(".avi") || filename.Contains(".mp4") || filename.Contains(".mkv"))
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

        // return the resolution of an image file from PC
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
    }
}