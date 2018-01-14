using MikuDownloader.image;
using MikuDownloader.misc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace MikuDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        
        // downloads from URL inputted in text field
        private async void btnDownloadSingleImage_Click(object sender, RoutedEventArgs e)
        {
            BlockAllButtons();
            txtBlockData.Text = "Downloading...";

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
                        var responseTuple = await ImageHelper.GetResponseFromURL(imageURL);

                        var imageData = ImageHelper.ReverseImageSearch(responseTuple.Item1, responseTuple.Item2, out status);
                        List<ImageDetails> matchesList = imageData.MatchingImages;

                        if (matchesList != null && matchesList.Count > 0)
                        {
                            ImageHelper.DownloadBestImage(matchesList);
                            status += "Successfully downoaded image!\n";
                        }
                        txtBlockData.Text = status;
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
                        
                        txtBlockData.Text = status;
                    }
                    finally
                    {
                        File.AppendAllText(ImageHelper.GetLogFileName(), ImageHelper.GetLogTimestamp() + status);
                    }
                }
                else
                {
                    txtBlockData.Text = "Not a valid URL! Check if it starts with http:// or https://";
                }
            }
            else
            {
                txtBlockData.Text = "No URL!";
            }
            
            ReleaseAllButtons();
        }

        // downloads from file selected with brose button
        private async void btnNewFile_Click(object sender, RoutedEventArgs e)
        {
            BlockAllButtons();

            txtBlockData.Text = "Downloading...";
            string filename = ImageHelper.BrowseFile(Constants.ImagesFilter);
            string status = string.Empty;

            if (!string.IsNullOrEmpty(filename))
            {
                try
                {
                    var responseTuple = await ImageHelper.GetResponseFromFile(filename);

                    var imageData = ImageHelper.ReverseImageSearch(responseTuple.Item1, responseTuple.Item2, out status);
                    List<ImageDetails> matchesList = imageData.MatchingImages;

                    if (matchesList != null && matchesList.Count > 0)
                    {
                        var res = ImageHelper.GetResolution(filename);
                        bool? ignoreResolution = chkBoxIgnoreResolution.IsChecked;
                        bool? keepFilenames = chkBoxKeepFilenames.IsChecked;


                        if (matchesList.First().Resolution.Equals(res) && ignoreResolution != true)
                        {
                            status += "Image checked had same resolution! Image was not downloaded! If you want to download it anyway check the logs!\n";
                        }
                        else
                        {
                            string oldFilename = String.Empty;
                            if(keepFilenames == true)
                            {
                                oldFilename = Path.GetFileNameWithoutExtension(filename);
                            }
                            ImageHelper.DownloadBestImage(matchesList, oldFilename);
                            status += "Successfully downoaded image!\n";
                        }
                    }
                    txtBlockData.Text = status;
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

                    txtBlockData.Text = status;
                }
                finally
                {
                    File.AppendAllText(ImageHelper.GetLogFileName(), ImageHelper.GetLogTimestamp() + status);
                }
            }
            else
            {
                txtBlockData.Text = "No file selected!";
            }
            ReleaseAllButtons();
        }

        // downloads IMGUR post links (will fix later) from selected teext file
        private async void btnDownloadFromList_Click(object sender, RoutedEventArgs e)
        {
            BlockAllButtons();

            string filepath = ImageHelper.BrowseFile(Constants.TextFilter);

            if (!string.IsNullOrEmpty(filepath))
            {
                List<string> urls = ImageHelper.ParseURLs(filepath);
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
                    txtBlockData.Text = "Downloading images...";

                    await ImageHelper.DownloadBulkImages(finalUrls);

                    txtBlockData.Text = "Finished downloading images! Check log for more info!";
                }
                else
                {
                    txtBlockData.Text = "No images found in text file!";
                }
            }
            else
            {
                MessageBox.Show("No file selected!", "Error");
            }

            ReleaseAllButtons();
        }

        // downloads best res for all images in folder
        private async void btnDownloadFromFolder_Click(object sender, RoutedEventArgs e)
        {
            BlockAllButtons();

            string folderPath = ImageHelper.BrowseDirectory(Constants.ImagesFilter);

            if (!string.IsNullOrEmpty(folderPath))
            {
                List<string> images = Directory.GetFiles(folderPath).ToList();

                if (images != null && images.Count > 0)
                {
                    txtBlockData.Text = "Downloading images...";
                    bool? keepFilenames = chkBoxKeepFilenames.IsChecked;
                    bool? ignoreResolution = chkBoxIgnoreResolution.IsChecked;
                    
                    await ImageHelper.DownloadBulkImagesFromFolder(images, keepFilenames, ignoreResolution);

                    txtBlockData.Text = "Finished downloading images! Check log for more info!";
                }
                else
                {
                    txtBlockData.Text = "No images found in text file!";
                }
            }
            else
            {
                MessageBox.Show("No file selected!", "Error");
            }

            ReleaseAllButtons();
        }

        private void BlockAllButtons()
        {
            btnDownloadFromFolder.IsEnabled = false;
            btnDownloadFromList.IsEnabled = false;
            btnDownloadSingleImage.IsEnabled = false;
            btnNewFile.IsEnabled = false;
            btnDownloadRecommendations.IsEnabled = false;
            btnDuplicateCheck.IsEnabled = false;
            btnTestSerialization.IsEnabled = false;
            btnDeserializeFolder.IsEnabled = false;
        }

        private void ReleaseAllButtons()
        {
            btnDownloadFromFolder.IsEnabled = true;
            btnDownloadFromList.IsEnabled = true;
            btnDownloadSingleImage.IsEnabled = true;
            btnNewFile.IsEnabled = true;
            btnDownloadRecommendations.IsEnabled = true;
            btnDuplicateCheck.IsEnabled = true;
            btnTestSerialization.IsEnabled = true;
            btnDeserializeFolder.IsEnabled = true;
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
        
        // TODO: Add kawaii icons for all buttons
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

        // downloads sankaku recommendations
        private async void btnDownloadRecommendations_Click(object sender, RoutedEventArgs e)
        {
            BlockAllButtons();

            string folderPath = ImageHelper.BrowseDirectory(Constants.ImagesFilter);

            if (!string.IsNullOrEmpty(folderPath))
            {
                List<string> images = Directory.GetFiles(folderPath).ToList();

                if (images != null && images.Count > 0)
                {
                    txtBlockData.Text = "Downloading images...";
                    bool? keepFilenames = chkBoxKeepFilenames.IsChecked;
                    bool? ignoreResolution = chkBoxIgnoreResolution.IsChecked;

                    await ImageHelper.DownloadBulkRecommendationsFromFolder(images);

                    txtBlockData.Text = "Finished downloading images! Check log for more info!";
                }
                else
                {
                    txtBlockData.Text = "No images found in text file!";
                }
            }
            else
            {
                MessageBox.Show("No file selected!", "Error");
            }

            ReleaseAllButtons();
        }

        // checks a folder if there are duplicate images inside
        private async void btnDuplicateCheck_Click(object sender, RoutedEventArgs e)
        {
            BlockAllButtons();

            string logger = string.Empty;

            string folderPath = ImageHelper.BrowseDirectory(Constants.ImagesFilter);

            if (!string.IsNullOrEmpty(folderPath))
            {
                List<string> images = Directory.GetFiles(folderPath).ToList();

                if (images != null && images.Count > 0)
                {
                    txtBlockData.Text = "Checking images...";

                    logger += string.Format("Attempting to generate list of duplicate images for folder : {0}\n", folderPath);

                    var watch = Stopwatch.StartNew();

                    Dictionary<Tuple<string, string>, List<string>> duplicatesList = await ImageHelper.CheckDuplicateImages(images);

                    watch.Stop();
                    var elapsedMs = watch.ElapsedMilliseconds;

                    logger += string.Format("Total files in folder that were checked: {0}\nTotal time to check images: {1} seconds\n", images.Count(), elapsedMs/1000.0);

                    watch.Reset();
                    watch.Start();

                    foreach(KeyValuePair<Tuple<string, string>, List<string>> kvp in duplicatesList)
                    {
                        logger += string.Format("{0}\nPost URL of duplicate image: {1} | Resolution: {2}\nDuplicate images:\n",Constants.VeryLongLine, kvp.Key.Item1, kvp.Key.Item2);

                        foreach (string file in kvp.Value)
                        {
                            string resolution = ImageHelper.GetResolution(Path.Combine(folderPath, file));
                            logger += string.Format("Image: {0} | Resolution: {1}", file, resolution);

                            try
                            {
                                string copyFrom = Path.Combine(folderPath, file);
                                string duplicateDirectory = string.Empty;
                                string moveTo = string.Empty;

                                if (kvp.Key.Item2.Equals(resolution))
                                {
                                    duplicateDirectory = Path.Combine(folderPath, Constants.DuplicatesDirectory);
                                    logger += "\n";
                                }
                                else
                                {
                                    duplicateDirectory = Path.Combine(folderPath, Constants.BadDuplicatesDirectory);
                                    logger += " - Bad Resolution!\n";
                                }

                                moveTo = Path.Combine(duplicateDirectory, Path.GetFileName(file));
                                Directory.CreateDirectory(duplicateDirectory);

                                File.Move(copyFrom, moveTo); // Try to move
                            }
                            catch (IOException ex)
                            {
                                logger += string.Format("Failed to move file! {0}\nCheck other folder!\n", ex.Message);
                            }   
                        }
                    }
                    watch.Stop();
                    elapsedMs = watch.ElapsedMilliseconds;

                    logger += string.Format("Total time to write logfile and move duplicates: {0} seconds\n", elapsedMs / 1000.0);

                    txtBlockData.Text = "Finished checking images! Check log for more info!";

                    File.AppendAllText(ImageHelper.GetDuplicatesLogFileName(), logger);
                }
                else
                {
                    txtBlockData.Text = "No images found in text file!";
                }
            }
            else
            {
                MessageBox.Show("No file selected!", "Error");
            }
            ReleaseAllButtons();
        }

        private async void btnTestSerialization_Click(object sender, RoutedEventArgs e)
        {
            BlockAllButtons();
            string status = string.Empty;
            string errorLog = string.Empty;

            string folderPath = ImageHelper.BrowseDirectory(Constants.ImagesFilter);

            if (!string.IsNullOrEmpty(folderPath))
            {
                List<string> images = Directory.GetFiles(folderPath).ToList();

                if (images != null && images.Count > 0)
                {
                    txtBlockData.Text = "Seriazlizing images...";
                    bool? keepFilenames = chkBoxKeepFilenames.IsChecked;
                    bool? ignoreResolution = chkBoxIgnoreResolution.IsChecked;

                    string xmlStart = "<?xml version=\"1.0\"?>";

                    List<ImageData> allImages = new List<ImageData>();

                    foreach (string image in images)
                    {
                        var responseTuple = await ImageHelper.GetResponseFromFile(image);

                        var imageData = ImageHelper.ReverseImageSearch(responseTuple.Item1, responseTuple.Item2, out status);
                        List<ImageDetails> matchesList = imageData.MatchingImages;

                        if (matchesList != null && matchesList.Count > 0)
                        {
                            string fileResolution = ImageHelper.GetResolution(image);
                            string matchResolution = matchesList.First().Resolution;
                            if (ImageHelper.CheckIfBetterResolution(fileResolution, matchResolution))
                            {
                                allImages.Add(imageData);

                                try
                                {
                                    string serializedDirectory = Path.Combine(folderPath, Constants.SerializedDirectory);

                                    string moveTo = Path.Combine(serializedDirectory, Path.GetFileName(image));

                                    Directory.CreateDirectory(serializedDirectory);

                                    File.Move(image, moveTo); // Try to move
                                }
                                catch (IOException ex)
                                {
                                    errorLog += string.Format("Failed to move file! {0}\n", ex.Message);
                                }
                            }
                        }
                    }

                    if (allImages != null && allImages.Count > 0)
                    {

                        string newThing = string.Format("{0}\n{1}", xmlStart, SerializingHelper.SerializeImageList(allImages));

                        string fileName = string.Format("{0}_{1}", DateTime.Now.ToString("yyyyMMdd_HHmmss"), Constants.SerializationFilename);

                        File.WriteAllText(fileName, newThing);

                        File.WriteAllText("serialization_errors.txt", errorLog);
                    }

                    txtBlockData.Text = "Finished serializing images! Check log for more info!";
                }
                else
                {
                    txtBlockData.Text = "No images found in text file!";
                }
            }
            else
            {
                MessageBox.Show("No file selected!", "Error");
            }

            ReleaseAllButtons();
        }

        private void btnDeserializeFolder_Click(object sender, RoutedEventArgs e)
        {
            BlockAllButtons();

            string fileName = ImageHelper.BrowseFile(Constants.XmlFilter);

            if (!string.IsNullOrEmpty(fileName))
            {

                string xmlDoc = File.ReadAllText(fileName);

                List<ImageData> images = SerializingHelper.DeserializeImageList(xmlDoc);

                if (images != null && images.Count > 0)
                {
                    txtBlockData.Text = "Downloading images...";
                    ImageHelper.DownloadSerializedImages(images);

                    txtBlockData.Text = "Finished downloading images! Check log for more info!";
                }
                else
                {
                    txtBlockData.Text = "No images found in text file!";
                }
            }
            else
            {
                MessageBox.Show("No file selected!", "Error");
            }

            ReleaseAllButtons();
        }
    }
}
