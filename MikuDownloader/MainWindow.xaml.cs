using System;
using System.Collections.Generic;
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

                        var imageList = ImageHelper.ReverseImageSearch(responseTuple.Item1, responseTuple.Item2, out status);

                        if (imageList != null && imageList.Count > 0)
                        {
                            ImageHelper.DownloadBestImage(imageList);
                            status += "Successfully downoaded image!\n";
                        }
                        txtBlockData.Text = status;
                    }
                    catch (Exception ex)
                    {
                        if (ex.InnerException != null)
                        {
                            status += String.Format("Original image: {0}\nFailed to download image!\n{1}\n{2}\n", imageURL, ex.Message, ex.InnerException.Message);
                        }
                        else
                        {
                            status += String.Format("Original image: {0}\nFailed to download image!\n", imageURL);
                        }
                        txtBlockData.Text = ex.Message + "\nContact pepkata when troubled!";
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

                    var imageList = ImageHelper.ReverseImageSearch(responseTuple.Item1, responseTuple.Item2, out status);

                    if (imageList != null && imageList.Count > 0)
                    {
                        var res = ImageHelper.GetResolution(filename);

                        if (imageList.First().Resolution.Equals(res))
                        {
                            status += "Image checked had same resolution! Image was not downloaded! If you want to download it anyway check the logs!\n";
                        }
                        else
                        {
                            ImageHelper.DownloadBestImage(imageList);
                            status += "Successfully downoaded image!\n";
                        }
                    }
                    File.AppendAllText(ImageHelper.GetLogFileName(), ImageHelper.GetLogTimestamp() + status);

                    txtBlockData.Text = status;
                }
                catch (Exception ex)
                {
                    status += String.Format("Original image: {0}\nFailed to download image!\n{1}\n{2}\n", filename, ex.Message, String.IsNullOrEmpty(ex.InnerException.Message) ? ex.InnerException.Message : string.Empty);
                    txtBlockData.Text = ex.Message + "\nContact pepkata when troubled!";
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
                    await ImageHelper.DownloadBulkImagesFromFolder(images);
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
        }

        private void ReleaseAllButtons()
        {
            btnDownloadFromFolder.IsEnabled = true;
            btnDownloadFromList.IsEnabled = true;
            btnDownloadSingleImage.IsEnabled = true;
            btnNewFile.IsEnabled = true;
        }

    }
}
