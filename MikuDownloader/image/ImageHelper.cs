﻿using HtmlAgilityPack;
using MikuDownloader.image;
using MikuDownloader.misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MikuDownloader
{
    public static class ImageHelper
    {
        // Searches for image from file
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

        // Searches for image from URL
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

        // Makes HTML request for image reverse search using a local image file
        public async static Task<Tuple<HtmlDocument, string>> GetResponseFromFile(string imagePath)
        {
            var httpResponse = await ReverseSearchFileHTTP(imagePath);
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(httpResponse);

            Tuple<HtmlDocument, string> tempTuple = new Tuple<HtmlDocument, string>(htmlDoc, imagePath);

            return tempTuple;
        }

        // Makes HTML request for image reverse search using an existing image URL
        public async static Task<Tuple<HtmlDocument, string>> GetResponseFromURL(string imageURL)
        {
            var httpResponse = await ReverseSearchURLHTTP(imageURL);
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(httpResponse);

            Tuple<HtmlDocument, string> tempTuple = new Tuple<HtmlDocument, string>(htmlDoc, imageURL);

            return tempTuple;
        }

        // MAIN FUNC
        // This function parses an HTML after reverse-searching from IQDB
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
                            response += String.Format("No Relevant images found!\nCheck below URLs:\n{0}\n{1}\n{2}\n{3}\n", Constants.SauceNAOMain, Constants.TinEyeMain, Constants.GoogleMain, Constants.Ascii2dMain);

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
                                var ascii2d = String.Format("{0}{1}", Constants.Ascii2d, originalImage);

                                response += String.Format("No Relevant images found!\nCheck below URLs:\n{0}\n{1}\n{2}\n{3}\n", sauceNao, tinEye, google, ascii2d);
                            }
                            else
                            {
                                response += String.Format("No Relevant images found!\nCheck below URLs:\n{0}\n{1}\n{2}\n{3}\n", Constants.SauceNAOMain, Constants.TinEyeMain, Constants.GoogleMain, Constants.Ascii2dMain);
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

        // Downloads best image from a set of sites provided
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

        // Saves parsed images based on site priority
        private static void SavePriorityImages(List<ImageDetails> images, string fileName)
        {
            var folderPath = Utilities.GetLoadedDirectory();

            bool flagSuccessfulDownload = false;
            int oldPriority = 1;
            bool status = false;
            string errorMessages = string.Empty;

            images.Sort((x, y) => x.Priority.CompareTo(y.Priority));

            if (images.Count == 1)
            {
                if (images[0].MatchSource == MatchSource.TheAnimeGallery)
                {
                    throw new ArgumentException(Constants.TheAnimeGalleryErrorMessage);
                }
                else if (images[0].MatchSource == MatchSource.Zerochan)
                {
                    throw new ArgumentException(Constants.ZerochanErrorMessage);
                }
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
                        if (image.Priority <= 3 || !flagSuccessfulDownload)
                        {
                            imageUrl = Utilities.GetImageURL(image.PostURL, image.MatchSource, out status);
                            if (status)
                            {
                                flagSuccessfulDownload = true;
                            }
                            else
                            {
                                errorMessages += imageUrl;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(imageUrl) && status)
                    {
                        Utilities.SaveImage(folderPath, imageUrl, fileName);
                    }
                    // anti-ban
                    Task.Delay(500);
                }
                catch (Exception ex)
                {
                    errorMessages += ex.Message + "\n";
                }
            }

            if (!flagSuccessfulDownload)
            {
                throw new ArgumentException($"Something went wrong when downloading the image!\n{errorMessages}");
            }
        }

        // Checks a list of images for duplicates and marks it
        public static void MarkDuplicateImages(List<ImageData> images)
        {
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
        }
    }
}