using MikuDownloader.image;
using MikuDownloader.misc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using XamlAnimatedGif;

namespace MikuDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Button> buttonsList;

        public MainWindow()
        {
            InitializeComponent();
            buttonsList = new List<Button>()
            {
                btnDownloadFromFolder,
                btnDownloadFromList,
                btnDownloadFromFile,
                btnDownloadFromURL,
                btnCheckFolder,
                btnDeserializeFolder
            };
        }

        // checks image from URL inputted in text field
        private async void btnDownloadFromURL_Click(object sender, RoutedEventArgs e)
        {
            BlockAllButtons();

            string imageURL = txtBoxURL.Text;
            string status = string.Empty;

            if (!string.IsNullOrEmpty(imageURL))
            {
                Uri uriResult;
                bool result = Uri.TryCreate(imageURL, UriKind.Absolute, out uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                if (result)
                {
                    try
                    {
                        txtBlockData.Text = "Checking for matching images...\n";

                        var responseTuple = await ImageHelper.GetResponseFromURL(imageURL);

                        var imageData = ImageHelper.ReverseImageSearch(responseTuple.Item1, responseTuple.Item2, out status);
                        if (imageData != null)
                        {
                            List<ImageDetails> matchesList = imageData.MatchingImages;
                            string errorText = string.Empty;

                            if (matchesList != null && matchesList.Count > 0)
                            {
                                txtBlockData.Text += "Image found! Attempting to download...\n";
                                await Task.Run(() => errorText = ImageHelper.DownloadBestImage(matchesList));
                            }

                            if (string.IsNullOrEmpty(errorText))
                            {
                                status += "Successfully downloaded image!\n";
                                txtBlockData.Text += $"Successfully downloaded image!\nCheck folder: {Utilities.GetMainDownloadDirectory()}";
                            }
                            else
                            {
                                status += errorText;
                                txtBlockData.Text += $"{errorText} Check logs for more info!";
                            }
                        }
                        else
                        {
                            txtBlockData.Text += "Failed to download image! Check log for more info!";
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

                        txtBlockData.Text += "Failed to download image! Check log for more info!";
                    }
                    finally
                    {
                        File.AppendAllText(Utilities.GetLogFileName(), Utilities.GetLogTimestamp() + status);
                    }
                }
                else
                {
                    MessageBox.Show("Not a valid URL! Check if it starts with http:// or https://", "Error");
                }
            }
            else
            {
                MessageBox.Show("No URL was inputted!", "Error");
            }

            ReleaseAllButtons();
        }

        // selects an image from local machine and checks it
        private async void btnDownLoadFromFile_Click(object sender, RoutedEventArgs e)
        {
            BlockAllButtons();

            string filename = Utilities.BrowseFile(Constants.ImagesFilter);
            string status = string.Empty;

            if (!string.IsNullOrEmpty(filename))
            {
                try
                {
                    txtBlockData.Text = "Checking for matching images...\n";

                    var responseTuple = await ImageHelper.GetResponseFromFile(filename);

                    var imageData = ImageHelper.ReverseImageSearch(responseTuple.Item1, responseTuple.Item2, out status);

                    if (imageData != null)
                    {
                        List<ImageDetails> matchesList = imageData.MatchingImages;

                        if (matchesList != null && matchesList.Count > 0)
                        {
                            var res = Utilities.GetResolution(filename);
                            bool? ignoreResolution = chkBoxIgnoreResolution.IsChecked;
                            bool? keepFilenames = chkBoxKeepFilenames.IsChecked;
                            string resToCheck = matchesList.First().Resolution;

                            if (!Utilities.CheckIfBetterResolution(res, resToCheck) && ignoreResolution != true)
                            {
                                status += "Image checked had equal or better resolution! Image was not downloaded!\n";
                                txtBlockData.Text += "Image checked had equal or better resolution! Image was not downloaded!\nIf you want to download it anyway check the logs for links!\n";
                            }
                            else
                            {
                                string oldFilename = String.Empty;
                                if (keepFilenames == true)
                                {
                                    oldFilename = Path.GetFileNameWithoutExtension(filename);
                                }
                                txtBlockData.Text += "Image found! Attempting to download...\n";
                                string errorText = string.Empty;

                                await Task.Run(() => errorText = ImageHelper.DownloadBestImage(matchesList, oldFilename));


                                if (string.IsNullOrEmpty(errorText))
                                {
                                    status += "Successfully downloaded image!\n";
                                    txtBlockData.Text += $"Successfully downloaded image!\nCheck folder: {Utilities.GetMainDownloadDirectory()}";
                                }
                                else
                                {
                                    status += errorText;
                                    txtBlockData.Text += $"{errorText} Check logs for more info!";
                                }
                            }
                        }
                        else
                        {
                            txtBlockData.Text += "Failed to download image! Check log for more info!";
                        }
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

                    txtBlockData.Text += "Failed to download image! Check log for more info!";
                }
                finally
                {
                    File.AppendAllText(Utilities.GetLogFileName(), Utilities.GetLogTimestamp() + status);
                }
            }
            else
            {
                MessageBox.Show("No file was selected!", "Error");
            }

            ReleaseAllButtons();
        }

        // reads image links from a text files and checks them
        private async void btnDownloadFromList_Click(object sender, RoutedEventArgs e)
        {
            BlockAllButtons();
            string filepath = Utilities.BrowseFile(Constants.TextFilter);

            if (!string.IsNullOrEmpty(filepath))
            {
                List<string> urls = Utilities.ParseURLs(filepath);
                List<string> finalUrls = new List<string>();

                if (urls != null && urls.Count > 0)
                {
                    foreach (string url in urls)
                    {
                        bool result = Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                        if (result)
                        {
                            finalUrls.Add(url);
                        }
                    }
                    if (finalUrls.Count > 0)
                    {
                        await DownloadFromList(finalUrls);
                    }
                    else
                    {
                        MessageBox.Show("No valid links found in text file!", "Error");
                    }
                }
                else
                {
                    MessageBox.Show("No images found in text file!", "Error");
                }
            }
            else
            {
                MessageBox.Show("No file was selected!", "Error");
            }

            ReleaseAllButtons();
        }

        // checks a list of Image URLs
        public async Task DownloadFromList(List<string> finalUrls)
        {
            string currStatus = string.Empty;
            int currPic = 0;
            int lastPic = finalUrls.Count;
            int downloadedCount = 0;
            int failedCount = 0;
            int notFoundCount = 0;
            string noMatchesImages = "Pictures with no relative matches found:\n";
            string failedToDownloadImages = "Pictures that failed to download:\n";

            foreach (string imageURL in finalUrls)
            {
                currStatus = $"Successful downloads: {downloadedCount}\nFailed downloads: {failedCount}\nNo matching images found: {notFoundCount}";
                currPic++;
                string status = string.Empty;
                txtBlockData.Text = $"{currStatus}\nChecking image {currPic} of {lastPic}...\n";

                try
                {
                    var responseTuple = await ImageHelper.GetResponseFromURL(imageURL);

                    var imageList = ImageHelper.ReverseImageSearch(responseTuple.Item1, responseTuple.Item2, out status);

                    if (imageList != null && imageList.MatchingImages.Count > 0)
                    {
                        List<ImageDetails> matchesList = imageList.MatchingImages;

                        string errorText = string.Empty;

                        txtBlockData.Text += "Image found! Attempting to download...\n";

                        await Task.Run(() => errorText = ImageHelper.DownloadBestImage(matchesList));

                        if (string.IsNullOrEmpty(errorText))
                        {
                            status += "Successfully downloaded image!\n";
                            txtBlockData.Text += $"Successfully downloaded image!\n";
                            downloadedCount++;
                        }
                        else
                        {
                            status += errorText + "\n";
                            txtBlockData.Text += $"{errorText} Check logs for more info!";
                            failedCount++;
                            failedToDownloadImages += imageURL + "\n";
                        }
                    }
                    else
                    {
                        txtBlockData.Text += $"No matches found for {Path.GetFileName(imageURL)}!";
                        notFoundCount++;
                        noMatchesImages += imageURL + "\n";
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    failedToDownloadImages += imageURL + "\n";
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
                    File.AppendAllText(Utilities.GetLogFileName(), Utilities.GetLogTimestamp() + status);
                }
            }
            currStatus = $"Successful downloads: {downloadedCount}\nFailed downloads: {failedCount}\nNo matching images found: {notFoundCount}";
            txtBlockData.Text = $"Finished checking image list! Check log for more info!\n{currStatus}";
            if (failedCount > 0 || notFoundCount > 0)
            {
                string failsString = string.Empty;
                if (failedCount > 0)
                {
                    failsString += failedToDownloadImages;
                }
                if (notFoundCount > 0)
                {
                    failsString += noMatchesImages;
                }
                File.AppendAllText(Utilities.GetNotDownloadedFilename(), failsString);
            }
        }

        // reads a folder for images and checks them
        private async void btnDownloadFromFolder_Click(object sender, RoutedEventArgs e)
        {
            BlockAllButtons();

            string folderPath = Utilities.BrowseDirectory(Constants.ImagesFilter);

            if (!string.IsNullOrEmpty(folderPath))
            {
                List<string> images = Directory.GetFiles(folderPath).ToList();

                if (images != null && images.Count > 0)
                {
                    bool? keepFilenames = chkBoxKeepFilenames.IsChecked;
                    bool? ignoreResolution = chkBoxIgnoreResolution.IsChecked;

                    await DownloadFromFolder(images, keepFilenames, ignoreResolution);
                }
                else
                {
                    MessageBox.Show("No images found in text file!", "Error");
                }
            }
            else
            {
                MessageBox.Show("No file was selected!", "Error");
            }

            ReleaseAllButtons();
        }

        // checks a list of files
        public async Task DownloadFromFolder(List<string> imagesToDownload, bool? keepFilenames = true, bool? ignoreResolution = false)
        {
            string secondaryLog = String.Format("Begin checking of files for folder: {0}\n", Path.GetDirectoryName(imagesToDownload.First()));

            string currStatus = string.Empty;
            int currFile = 0;
            int lastFile = imagesToDownload.Count;
            int downloadedCount = 0;
            int failedCount = 0;
            int notFoundCount = 0;
            int isNotImageCount = 0;
            int noBetterResFoundCount = 0;
            string noMatchesImages = "Pictures with no relative matches found:\n";
            string failedToDownloadImages = "Pictures that failed to download:\n";
            
            foreach (string file in imagesToDownload)
            {
                currStatus = $"Successful downloads: {downloadedCount}\nFailed downloads: {failedCount}\nNo matching images found: {notFoundCount}\nNot image files: {isNotImageCount}\nImages with good resolution: {noBetterResFoundCount}";
                currFile++;

                string status = string.Empty;
                secondaryLog += String.Format("Checking image for: {0}\n", Path.GetFileName(file));
                txtBlockData.Text = $"{currStatus}\nChecking file {currFile} of {lastFile}...\n";

                if (Utilities.IsImage(file))
                {
                    try
                    {
                        var responseTuple = await ImageHelper.GetResponseFromFile(file);

                        var imageList = ImageHelper.ReverseImageSearch(responseTuple.Item1, responseTuple.Item2, out status);
                        List<ImageDetails> matchingImages = imageList.MatchingImages;

                        if (matchingImages != null && matchingImages.Count > 0)
                        {
                            string fileResolution = Utilities.GetResolution(file);
                            string matchResolution = matchingImages.First().Resolution;

                            if (Utilities.CheckIfBetterResolution(fileResolution, matchResolution) || ignoreResolution == true)
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

                                string errorText = string.Empty;
                                txtBlockData.Text += "Image found! Attempting to download...\n";

                                await Task.Run(() => errorText = ImageHelper.DownloadBestImage(matchingImages, origImageName));


                                if (string.IsNullOrEmpty(errorText))
                                {
                                    status += "Successfully downloaded image!\n";
                                    secondaryLog += String.Format("Image with better resolution was found or resolution is being ignored!\nOriginal res: {0} - new res: {1}\n", fileResolution, matchResolution);
                                    txtBlockData.Text += $"Successfully downloaded image!\n";
                                    downloadedCount++;
                                }
                                else
                                {
                                    status += errorText + "\n";
                                    secondaryLog += errorText + "\n";
                                    txtBlockData.Text += $"{errorText} Check logs for more info!";
                                    failedCount++;
                                    failedToDownloadImages += file + "\n";
                                }
                            }
                            else
                            {
                                noBetterResFoundCount++;
                                status += "Image in folder has same resolution! Image was not downloaded!\n";
                                secondaryLog += "Image in folder has same resolution!\n";
                            }
                        }
                        else
                        {
                            notFoundCount++;
                            noMatchesImages += file + "\n";
                            secondaryLog += "No matches were found for the image!\n";
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        failedToDownloadImages += file + "\n";

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
                        File.AppendAllText(Utilities.GetLogFileName(), Utilities.GetLogTimestamp() + status);
                    }
                }
                else
                {
                    isNotImageCount++;
                    secondaryLog += String.Format("File: {0} is not an image file and was not checked!\n", Path.GetFileName(file));
                }
                secondaryLog += Constants.VeryLongLine + "\n";
            }
            currStatus = $"Successful downloads: {downloadedCount}\nFailed downloads: {failedCount}\nNo matching images found: {notFoundCount}\nNot image files: {isNotImageCount}\nImages with good resolution: {noBetterResFoundCount}";
            txtBlockData.Text = $"Finished checking folder! Check log for more info!\n{currStatus}";
            if (failedCount > 0 || notFoundCount > 0)
            {
                string failsString = string.Empty;
                if (failedCount > 0)
                {
                    failsString += failedToDownloadImages;
                }
                if (notFoundCount > 0)
                {
                    failsString += noMatchesImages;
                }
                File.AppendAllText(Utilities.GetNotDownloadedFilename(), failsString);
            }
            File.AppendAllText(Utilities.GetSecondaryLogFileName(), secondaryLog + "\n" + currStatus +"\n" + Constants.VeryLongLine);
        }

        // checks folder for images that have duplicates or better resolution
        private async void btnCheckFolder_Click(object sender, RoutedEventArgs e)
        {
            BlockAllButtons();

            string folderPath = Utilities.BrowseDirectory(Constants.ImagesFilter);
            string logger = string.Empty;

            if (!string.IsNullOrEmpty(folderPath))
            {
                List<string> images = Directory.GetFiles(folderPath).ToList();

                if (images != null && images.Count > 0)
                {
                    logger += string.Format("Attempting to generate list of duplicate images for folder : {0}\n", folderPath);

                    var watch = Stopwatch.StartNew();

                    logger += await CheckFolderFull(images);

                    watch.Stop();
                    var elapsedMs = watch.ElapsedMilliseconds;

                    logger += string.Format("Total time to write logfile and move images: {0} seconds\n", elapsedMs / 1000.0);

                    File.AppendAllText(Utilities.GetDuplicatesLogFileName(), logger);
                }
                else
                {
                    MessageBox.Show("No images found in folder!", "Error");
                }
            }
            else
            {
                MessageBox.Show("No file was selected!", "Error");
            }

            ReleaseAllButtons();
        }

        // checks a folder for duplicates and find better resolutions
        public async Task<string> CheckFolderFull(List<string> imagesToCheck)
        {
            List<ImageData> imagesToCheckForDuplicates = new List<ImageData>();

            string log = string.Empty;
            string status = string.Empty;

            string currStatus = string.Empty;
            int currFile = 0;
            int lastFile = imagesToCheck.Count;
            int foundCount = 0;
            int notFoundCount = 0;
            int isNotImageCount = 0;
            int noBetterResFoundCount = 0;

            foreach (string file in imagesToCheck)
            {
                currStatus = $"Better resolution found: {foundCount}\nNo better resolution: {noBetterResFoundCount}\nNo matches found: {notFoundCount}\nNot images: {isNotImageCount}";
                currFile++;

                txtBlockData.Text = $"{currStatus}\nChecking file {currFile} of {lastFile}...\n";

                if (Utilities.IsImage(file))
                {
                    try
                    {
                        var responseTuple = await ImageHelper.GetResponseFromFile(file);

                        var imageData = ImageHelper.ReverseImageSearch(responseTuple.Item1, responseTuple.Item2, out status);
                        List<ImageDetails> matchingImages = imageData.MatchingImages;

                        if (matchingImages != null && matchingImages.Count > 0)
                        {
                            string fileResolution = Utilities.GetResolution(file);
                            string matchResolution = matchingImages.First().Resolution;

                            if (Utilities.CheckIfBetterResolution(fileResolution, matchResolution))
                            {
                                imageData.HasBetterResolution = true;
                                foundCount++;
                            }
                            else
                            {
                                noBetterResFoundCount++;
                            }

                            imagesToCheckForDuplicates.Add(imageData);
                        }
                        else
                        {
                            notFoundCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        log += $"{status}\n{ex.Message}\n";
                    }
                }
                else
                {
                    isNotImageCount++;
                }
            }
            currStatus = $"Better resolution found: {foundCount}\nNo better resolution: {noBetterResFoundCount}\nNo matches found: {notFoundCount}\nNot images: {isNotImageCount}";
            txtBlockData.Text = $"Finished checking folder! Check log for more info!\n{currStatus}";

            log = ImageHelper.MarkDuplicateImages(imagesToCheckForDuplicates);
            return log;
        }

        // downloads images from checked folder, reading from generated serialization XML
        private async void btnDeserializeFolder_Click(object sender, RoutedEventArgs e)
        {
            BlockAllButtons();

            string fileName = Utilities.BrowseFile(Constants.XmlFilter);

            if (!string.IsNullOrEmpty(fileName))
            {

                string xmlDoc = File.ReadAllText(fileName);

                List<ImageData> images = SerializingHelper.DeserializeImageList(xmlDoc);

                if (images != null && images.Count > 0)
                {
                    await DownloadSerializedImages(images);
                }
                else
                {
                    MessageBox.Show("No images found in XML file!", "Error");
                }
            }
            else
            {
                MessageBox.Show("No file was selected!", "Error");
            }

            ReleaseAllButtons();
        }

        // downloads images from serialized XML files
        public async Task DownloadSerializedImages(List<ImageData> imagesToDownload)
        {
            string currStatus = string.Empty;
            string listOfNotDownloadedImages = string.Empty;
            int currPic = 0;
            int lastPic = imagesToDownload.Count;
            int downloadedCount = 0;
            int failedCount = 0;

            foreach (ImageData imageContainer in imagesToDownload)
            {
                string status = string.Empty;
                currStatus = $"Successful downloads: {downloadedCount}\nFailed downloads: {failedCount}";
                currPic++;
                txtBlockData.Text = $"{currStatus}\nDownloading image {currPic} of {lastPic}...\n";

                string errorText = string.Empty;
                try
                {
                    await Task.Run(() => errorText = ImageHelper.DownloadBestImage(imageContainer.MatchingImages));

                    if (string.IsNullOrEmpty(errorText))
                    {
                        txtBlockData.Text += $"Successfully downloaded image!\n";
                        downloadedCount++;
                    }
                    else
                    {
                        status += errorText;
                        txtBlockData.Text += $"{errorText} Check logs for more info!";
                        failedCount++;
                        listOfNotDownloadedImages += imageContainer.OriginalImage + "\n";
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
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
                    if (!string.IsNullOrEmpty(status))
                    {
                        status = $"Original image: {imageContainer.OriginalImage}\n{status}";
                        File.AppendAllText(Utilities.GetLogFileName(), status + "\n" + Constants.VeryLongLine + "\n");
                    }
                }
            }
            currStatus = $"Successful downloads: {downloadedCount}\nFailed downloads: {failedCount}";
            txtBlockData.Text = $"Finished downloading images! Check log for more info!\n{currStatus}";

            if (failedCount > 0)
            {
                File.AppendAllText(Utilities.GetNotDownloadedFilename(), "Images not downloaded:\n" + listOfNotDownloadedImages + Constants.VeryLongLine + "\n");
            }
        }

        // opens the main download directory in the explorer
        private void menuOpenDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(Constants.MainDownloadDirectory))
            {
                Process.Start(Constants.MainDownloadDirectory);
            }
            else
            {
                MessageBox.Show("Nothing has been downloaded yet!", "Info");
            }
        }

        private void menuFromFile_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow tempHelpWindow = new HelpWindow(Constants.FromFileHelpText);
            tempHelpWindow.Show();
        }

        private void menuFromURL_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow tempHelpWindow = new HelpWindow(Constants.FromURLHelpText);
            tempHelpWindow.Show();
        }

        private void menuFromList_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow tempHelpWindow = new HelpWindow(Constants.FromListHelpText);
            tempHelpWindow.Show();
        }

        private void menuFromFolder_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow tempHelpWindow = new HelpWindow(Constants.FromFolderHelpText);
            tempHelpWindow.Show();
        }

        private void menuClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BlockAllButtons()
        {
            foreach (Button b in buttonsList)
            {
                b.IsEnabled = false;
            }
        }

        private void ReleaseAllButtons()
        {
            foreach (Button b in buttonsList)
            {
                b.IsEnabled = true;
            }
        }
    }
}
