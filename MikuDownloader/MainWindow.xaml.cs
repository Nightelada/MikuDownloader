using MikuDownloader.image;
using MikuDownloader.misc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
        
        // downloads from URL inputted in text field
        private async void btnDownloadFromURL_Click(object sender, RoutedEventArgs e)
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
                        File.AppendAllText(Utilities.GetLogFileName(), Utilities.GetLogTimestamp() + status);
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
        private async void btnDownLoadFromFile_Click(object sender, RoutedEventArgs e)
        {
            BlockAllButtons();

            txtBlockData.Text = "Downloading...";
            string filename = Utilities.BrowseFile(Constants.ImagesFilter);
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
                        var res = Utilities.GetResolution(filename);
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
                    File.AppendAllText(Utilities.GetLogFileName(), Utilities.GetLogTimestamp() + status);
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

            string folderPath = Utilities.BrowseDirectory(Constants.ImagesFilter);

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

        // downloads images from checked folder, reading from generated serialization XML
        private void btnDeserializeFolder_Click(object sender, RoutedEventArgs e)
        {
            BlockAllButtons();

            string fileName = Utilities.BrowseFile(Constants.XmlFilter);

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
                    bool? ignoreResolution = chkBoxIgnoreResolution.IsChecked;

                    txtBlockData.Text = "Checking folder for duplicates and lower resolution images...";

                    logger += string.Format("Attempting to generate list of duplicate images for folder : {0}\n", folderPath);

                    var watch = Stopwatch.StartNew();

                    logger += await ImageHelper.CheckFolderFull(images, ignoreResolution);

                    watch.Stop();
                    var elapsedMs = watch.ElapsedMilliseconds;

                    logger += string.Format("Total time to write logfile and move images: {0} seconds\n", elapsedMs / 1000.0);

                    File.AppendAllText(Utilities.GetDuplicatesLogFileName(), logger);

                    txtBlockData.Text = "Finished checking folder! Check log for more info!";
                }
                else
                {
                    txtBlockData.Text = "No images found in folder!";
                }
            }
            else
            {
                MessageBox.Show("No file selected!", "Error");
            }

            ReleaseAllButtons();
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
