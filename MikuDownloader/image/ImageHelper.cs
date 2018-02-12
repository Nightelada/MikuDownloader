using HtmlAgilityPack;
using MikuDownloader.image;
using MikuDownloader.misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MikuDownloader
{
    public static class ImageHelper
    {
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
                    string bestResoltuion = Utilities.DetermineBestResolution(resolutions);
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

        // downloads best image from a set of sites provided
        public static string DownloadBestImage(List<ImageDetails> images, string fileName = "")
        {
            // somehow try to distinguish files with differences! (edge tracing)
            if (images != null && images.Count > 0)
            {
                try
                {
                    SavePriorityImages(images, fileName);
                    return string.Empty;
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
            else
            {
                throw new ArgumentException("No images were found in selected sites! Download picture manually from links!\n");
            }
        }

        // save parsed images based on site priority
        private static void SavePriorityImages(List<ImageDetails> images, string fileName)
        {
            var folderPath = Utilities.GetMainDownloadDirectory();

            bool flagSuccessfulDownload = false;
            int oldPriority = 1;
            bool status = false;
            string errorMessages = string.Empty;

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
                        if (!flagSuccessfulDownload)
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
                            imageUrl = Utilities.GetImageURL(image.PostURL, image.MatchSource, out status);
                            if (status)
                            {
                                flagSuccessfulDownload = true;
                            }
                        }
                        else
                        {
                            if (!flagSuccessfulDownload)
                            {
                                imageUrl = Utilities.GetImageURL(image.PostURL, image.MatchSource, out status);
                                if (status)
                                {
                                    flagSuccessfulDownload = true;
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(imageUrl) && status)
                    {
                        Utilities.SaveImage(folderPath, imageUrl, fileName);
                    }
                    // anti-ban
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    errorMessages += ex.Message + "\n";
                }
            }

            if (!flagSuccessfulDownload)
            {
                throw new ArgumentException($"Something went wrong when downloading the image! {errorMessages}");
            }
        }

        // reads image links from folder and check for duplicates and find better resolutions
        public async static Task<string> CheckFolderFull(List<string> imagesToCheck, bool? ignoreResolution)
        {
            List<ImageData> imagesToCheckForDuplicates = new List<ImageData>();

            string log = string.Empty;
            string status = string.Empty;

            foreach (string file in imagesToCheck)
            {

                if (Utilities.IsImage(file))
                {
                    try
                    {
                        var responseTuple = await GetResponseFromFile(file);

                        var imageData = ReverseImageSearch(responseTuple.Item1, responseTuple.Item2, out status);
                        List<ImageDetails> matchingImages = imageData.MatchingImages;

                        if (matchingImages != null && matchingImages.Count > 0)
                        {
                            string fileResolution = Utilities.GetResolution(file);
                            string matchResolution = matchingImages.First().Resolution;

                            if (Utilities.CheckIfBetterResolution(fileResolution, matchResolution))
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

        // sorts duplicate files to different folder
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

                string resolution = Utilities.GetResolution(Path.Combine(originalFile));
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

        // downloads images from serialized XML files
        public static void DownloadSerializedImages(List<ImageData> imagesToDownload)
        {
            string listOfNotDownloadedImages = string.Empty;
            string status = string.Empty;

            foreach (ImageData imageContainer in imagesToDownload)
            {
                try
                {
                    DownloadBestImage(imageContainer.MatchingImages);
                    Thread.Sleep(1000); //anti-ban
                }
                catch (Exception ex)
                {
                    status += imageContainer.OriginalImage + "\n";
                    listOfNotDownloadedImages += imageContainer.OriginalImage + "\n";

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
                    if (string.IsNullOrEmpty(status))
                    {
                        status += Constants.VeryLongLine + "\n";
                        File.AppendAllText(Utilities.GetLogFileName(), Utilities.GetLogTimestamp() + status);
                    }
                }
            }

            if (!string.IsNullOrEmpty(status))
            {
                File.AppendAllText("images-not-downloaded.txt", "Images not downloaded:\n" + listOfNotDownloadedImages + Constants.VeryLongLine + status + Constants.VeryLongLine + "\n");
            }
        }
    }
}