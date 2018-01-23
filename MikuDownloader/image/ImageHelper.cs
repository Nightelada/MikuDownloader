using HtmlAgilityPack;
using Microsoft.Win32;
using MikuDownloader.image;
using MikuDownloader.misc;
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
        public static ImageData ReverseImageSearch(HtmlDocument htmlDoc, string originalImage, out string status)
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
            ImageData imageContainer = new ImageData(originalImage);

            if (matchNodes != null)
            {
                foreach (var match in matchNodes)
                {
                    // get info for each match
                    if (match.OuterHtml.Contains("match"))
                    {
                        var postId = match.SelectSingleNode("tr/td[@class='image']/a").GetAttributeValue("href", null);

                        var tags = match.SelectSingleNode("tr/td[@class='image']/a/img").GetAttributeValue("alt", null);
                        var resolution = match.SelectSingleNode("(tr/td)[3]").InnerText;

                        var similarity = match.SelectSingleNode("(tr/td)[4]").InnerText;
                        var matchType = match.SelectSingleNode("(tr/th)[1]").InnerText;

                        if (postId != null && tags != null && resolution != null)
                        {
                            ImageDetails tempImg = new ImageDetails(postId, tags, resolution, similarity, matchType);
                            imagesList.Add(tempImg);

                            if (!resolutions.Contains(tempImg.Resolution))
                            {
                                resolutions.Add(tempImg.Resolution);
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
                                response += String.Format("{0}: {1} Similarity: {2} Rating: {3}\n", image.MatchType.ToString(), image.PostURL, image.Similarity, image.MatchRating.ToString());
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

            imageContainer.MatchingImages = bestImages;

            status = response;
            return imageContainer;
        }

        // parses sankaku result to get recommendations links
        private static void SaveSankakuRecommendations(string folderPath, string postURL)
        {
            HtmlWeb web = new HtmlWeb();

            HtmlDocument htmlDoc = web.Load(postURL);

            List<string> recommendationList = new List<string>();

            // get all sankaku recommendation nodes
            var recommendationNodes = htmlDoc.DocumentNode.SelectNodes(".//div[@id='recommended']/div[@id='recommendations']/span");

            string logPath = Path.Combine(folderPath, Constants.RecommendationsLogFileName);

            Directory.CreateDirectory(folderPath);


            if (recommendationNodes != null)
            {
                // gets recommendation links
                foreach (var node in recommendationNodes)
                {
                    string tempRecommendation = node.SelectSingleNode("a").GetAttributeValue("href", null);
                    tempRecommendation = String.Format("https://chan.sankakucomplex.com{0}", tempRecommendation);
                    recommendationList.Add(tempRecommendation);
                }

                //downloads recommendation links
                foreach (string s in recommendationList)
                {
                    string imageName = s.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last();

                    //SaveSankakuImage(folderPath, s, imageName);
                }

                recommendationList.Add("==================================================================================");
                File.AppendAllLines(logPath, recommendationList);
            }
            else
            {
                recommendationList.Add("==================================================================================");
                File.AppendAllLines(logPath, recommendationList);
            }
        }

        // downloads best image from a set of sites provided
        public static void DownloadRecommendations(List<ImageDetails> images)
        {
            var currTime = DateTime.Now.ToString("yyyyMMdd");
            var folderPath = Path.Combine(Constants.MainDownloadDirectory, currTime.ToString());

            if (images != null && images.Count > 0)
            {
                foreach (ImageDetails image in images)
                {
                    try
                    {
                        if (image.MatchSource == MatchSource.SankakuChannel)
                        {
                            SaveSankakuRecommendations(folderPath, image.PostURL);
                        }

                        Thread.Sleep(1000);//anti-ban
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException(ex.Message);
                    }
                }
            }
            else
            {
                throw new ArgumentException("No images found in collection! Pages were not parsed correctly! Download picture manually from links!\n");
            }
        }

        // downloads bulk sankaku recommendations from a folder
        public async static Task<string> DownloadBulkRecommendationsFromFolder(List<string> imagesToDownload)
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

                        List<ImageDetails> matchingImages = imageList.MatchingImages;

                        if (matchingImages != null && matchingImages.Count > 0)
                        {
                            DownloadRecommendations(matchingImages);
                            status += "Successfully downoaded image!\n";
                            totalDownloadedImages += Path.GetFileName(file) + "\n";

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

            return "Successful";
        }

        // parses the post to find the url of the image
        private static string GetImageURL(string postURL, MatchSource source, out bool status)
        {
            try
            {
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

            origImage = String.Format("https://danbooru.donmai.us{0}", origImage);

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

        // downloads best image from a set of sites provided
        public static void DownloadBestImage(List<ImageDetails> images, string fileName = "")
        {
            // somehow try to distinguish files with differences! (edge tracing)
            if (images != null && images.Count > 0)
            {
                SavePriorityImages(images, fileName);
            }
            else
            {
                throw new ArgumentException("No images were found in selected sites! Download picture manually from links!\n");
            }
        }

        // save parsed images based on site priority
        private static void SavePriorityImages(List<ImageDetails> images, string fileName)
        {
            var currTime = DateTime.Now.ToString("yyyyMMdd");
            var folderPath = Path.Combine(Constants.MainDownloadDirectory, currTime.ToString());

            bool flagSuccessfullDownload = false;
            int oldPriority = 1;
            bool status = false;

            images.Sort((x, y) => x.Priority.CompareTo(y.Priority));

            if (images.Count == 1 && images[0].MatchSource == MatchSource.TheAnimeGallery)
            {
                throw new ArgumentException(Constants.TheAnimeGalleryErrorMessage);
            }

            foreach (ImageDetails image in images)
            {
                string imageUrl = string.Empty;
                try
                {
                    if (image.Priority > oldPriority)
                    {
                        if (!flagSuccessfullDownload)
                        {
                            oldPriority = image.Priority;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (oldPriority == image.Priority)
                    {
                        if (image.Priority <= 3)
                        {
                            imageUrl = GetImageURL(image.PostURL, image.MatchSource, out status);
                            if (status)
                            {
                                flagSuccessfullDownload = true;
                            }
                        }
                        else
                        {
                            if (!flagSuccessfullDownload)
                            {
                                imageUrl = GetImageURL(image.PostURL, image.MatchSource, out status);
                                if (status)
                                {
                                    flagSuccessfullDownload = true;
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(imageUrl) && status)
                    {
                        SaveImage(folderPath, imageUrl, fileName);
                    }
                    // anti-ban
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(ex.Message);
                }
            }

            if (!flagSuccessfullDownload)
            {
                throw new ArgumentException("Someething went wrong when downloading the image!");
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

        // reads links from file and downloads files 1 by 1
        public async static Task<string> DownloadBulkImages(List<string> URLsToDownload)
        {
            foreach (string imageURL in URLsToDownload)
            {
                string status = string.Empty;

                try
                {
                    var responseTuple = await GetResponseFromURL(imageURL);

                    var imageList = ReverseImageSearch(responseTuple.Item1, responseTuple.Item2, out status);
                    List<ImageDetails> matchingImages = imageList.MatchingImages;

                    DownloadBestImage(matchingImages);
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

        // reads image links from folder and downloads files 1 by 1
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
                        List<ImageDetails> matchingImages = imageList.MatchingImages;

                        if (matchingImages != null && matchingImages.Count > 0)
                        {
                            string fileResolution = GetResolution(file);
                            string matchResolution = matchingImages.First().Resolution;

                            if (CheckIfBetterResolution(fileResolution,matchResolution) || ignoreResolution == true)
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
                                DownloadBestImage(matchingImages, origImageName);
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

        // -||-
        public static string GetDuplicatesLogFileName()
        {
            var currTime = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return currTime + "_" + Constants.DuplicatesLogFileName;
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

        // reads image links from folder and check for duplicates
        public async static Task<string> CheckFolderFull(List<string> imagesToCheck, bool? ignoreResolution)
        {
            List<ImageData> imagesToCheckForDuplicates = new List<ImageData>();

            string log = string.Empty;
            string status = string.Empty;

            foreach (string file in imagesToCheck)
            {

                if (IsImage(file))
                {
                    try
                    {
                        var responseTuple = await GetResponseFromFile(file);

                        var imageData = ReverseImageSearch(responseTuple.Item1, responseTuple.Item2, out status);
                        List<ImageDetails> matchingImages = imageData.MatchingImages;

                        if (matchingImages != null && matchingImages.Count > 0)
                        {
                            string fileResolution = GetResolution(file);
                            string matchResolution = matchingImages.First().Resolution;

                            if (CheckIfBetterResolution(fileResolution, matchResolution))
                            {
                                imageData.HasBetterResolution = true;
                            }

                            imagesToCheckForDuplicates.Add(imageData);
                        }
                    }
                    catch (Exception ex)
                    {
                        log += string.Format("{0}\n{1}\n", status, ex.Message);
                    }
                }
            }

            log = MarkDuplicateImages(imagesToCheckForDuplicates);
            
            return log;
        }

        // Moves duplicate files to different folder
        private static string MarkDuplicateImages(List<ImageData> images)
        {
            string logger = string.Empty;
            List<ImageData> imagesWithBetterResolution = new List<ImageData>();
            List<ImageData> imagesWithSameResolution = new List<ImageData>();

            // Marks duplicate images
            for (int i = 0; i < images.Count; i++)
            {
                List<string> currImage = images[i].GetAllMatchingImages();
                int dupIndex = i;

                if (!images[i].Duplicate)
                {
                    for (int j = i + 1; j < images.Count; j++)
                    {
                        List<string> imageToCheck = images[j].GetAllMatchingImages();

                        if (!images[j].Duplicate)
                        {
                            var isDup = currImage.All(imageToCheck.Contains);
                            if (isDup)
                            {
                                images[j].Duplicate = true;
                                images[j].DuplicateIndex = dupIndex;
                                images[i].Duplicate = true;
                                images[i].DuplicateIndex = dupIndex;
                            }
                        }
                    }
                }
            }

            List<ImageData> finalDuplicates = new List<ImageData>();

            foreach (ImageData image in images)
            {
                if (image.Duplicate)
                {
                    finalDuplicates.Add(image);
                }
                else if (image.HasBetterResolution)
                {
                    imagesWithBetterResolution.Add(image);
                }
                else if (!image.HasBetterResolution)
                {
                    imagesWithSameResolution.Add(image);
                }
            }

            List<ImageData> sortedDuplicates = finalDuplicates.OrderBy(x => x.DuplicateIndex).ToList();

            foreach (ImageData image in sortedDuplicates)
            {
                string originalFile = image.OriginalImage;
                string folderPath = Path.GetDirectoryName(originalFile);

                string resolution = GetResolution(Path.Combine(originalFile));
                logger += string.Format("Image: {0} | Resolution: {1}", originalFile, resolution);

                try
                {
                    string copyFrom = originalFile;
                    string duplicateDirectory = string.Empty;
                    string moveTo = string.Empty;

                    if (image.MatchingImages.First().Resolution.Equals(resolution))
                    {
                        duplicateDirectory = Path.Combine(folderPath, Constants.DuplicatesDirectory, image.DuplicateIndex.ToString());
                        logger += "\n";
                    }
                    else
                    {
                        duplicateDirectory = Path.Combine(folderPath, Constants.DuplicatesDirectory, image.DuplicateIndex.ToString(), Constants.BadDuplicatesDirectory);
                        logger += " - Bad Resolution!\n";
                    }

                    logger += Constants.VeryLongLine + "\n";

                    moveTo = Path.Combine(duplicateDirectory, Path.GetFileName(originalFile));
                    Directory.CreateDirectory(duplicateDirectory);

                    File.Move(copyFrom, moveTo); // Try to move
                }
                catch (IOException ex)
                {
                    logger += string.Format("Failed to move file! {0}\n", ex.Message);
                }
            }

            MarkImagesForDownload(imagesWithBetterResolution);
            MarkImagesWithNoChange(imagesWithSameResolution);

            return logger;
        }

        // creates XML file for downloading images and moves them to different folder
        private static void MarkImagesForDownload(List<ImageData> images)
        {
            string errorLog = string.Empty;

            if (images != null && images.Count > 0)
            {
                foreach (ImageData image in images)
                {
                    try
                    {
                        string folderPath = Path.GetDirectoryName(image.OriginalImage);
                        string forDownloadDirectory = Path.Combine(folderPath, Constants.BetterResolutionDirectory);

                        string moveTo = Path.Combine(forDownloadDirectory, Path.GetFileName(image.OriginalImage));

                        Directory.CreateDirectory(forDownloadDirectory);

                        File.Move(image.OriginalImage, moveTo); // Try to move
                    }
                    catch (IOException ex)
                    {
                        errorLog += string.Format("Failed to move file! {0}\n", ex.Message);
                    }
                }

                string xmlStart = "<?xml version=\"1.0\"?>";

                string serializedImages = string.Format("{0}\n{1}", xmlStart, SerializingHelper.SerializeImageList(images));

                string fileName = string.Format("{0}_{1}", DateTime.Now.ToString("yyyyMMdd_HHmmss"), Constants.BetterResolutionFilename);

                File.WriteAllText(fileName, serializedImages);

                if (!string.IsNullOrEmpty(errorLog))
                {
                    File.AppendAllText("errors.txt", errorLog);
                }
            }
        }

        // moves files with proper resolution to different folder
        private static void MarkImagesWithNoChange(List<ImageData> images)
        {
            string errorLog = string.Empty;

            if (images != null && images.Count > 0)
            {
                foreach (ImageData image in images)
                {
                    try
                    {
                        string folderPath = Path.GetDirectoryName(image.OriginalImage);
                        string goodResolutionDirectory = Path.Combine(folderPath, Constants.GoodResolutionDirectory);

                        string moveTo = Path.Combine(goodResolutionDirectory, Path.GetFileName(image.OriginalImage));

                        Directory.CreateDirectory(goodResolutionDirectory);

                        File.Move(image.OriginalImage, moveTo); // Try to move
                    }
                    catch (IOException ex)
                    {
                        errorLog += string.Format("Failed to move file! {0}\n", ex.Message);
                    }
                }
                
                if (!string.IsNullOrEmpty(errorLog))
                {
                    File.AppendAllText("errors.txt", errorLog);
                }
            }
        }

        public static void DownloadSerializedImages(List<ImageData> imagesToDownload)
        {
            string imagesNotDownloaded = string.Empty;

            foreach (ImageData imageContainer in imagesToDownload)
            {
                string status = string.Empty;

                try
                {
                    DownloadBestImage(imageContainer.MatchingImages);
                    Thread.Sleep(1000); //anti-ban
                }
                catch (Exception ex)
                {
                    status += imageContainer.OriginalImage + "\n";
                    imagesNotDownloaded += imageContainer.OriginalImage + "\n";

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
                    if (!string.IsNullOrEmpty(status))
                    {
                        File.AppendAllText(GetLogFileName(), GetLogTimestamp() + status);
                        File.AppendAllText("images-not-downloaded", imagesNotDownloaded + Constants.VeryLongLine + "\n");
                    }
                }
            }
        }
    }
}