using MikuDownloader.enums;
using MikuDownloader.image;
using MikuDownloader.misc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using XamlAnimatedGif;

namespace MikuDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Button> buttonsList;
        private List<CheckBox> checkBoxesList;
        ObservableCollection<Tuple<FileType, string>> dragAndDropItems;

        public MainWindow()
        {
            InitializeComponent();

            dragAndDropItems = new ObservableCollection<Tuple<FileType, string>>();
            lstBoxDragAndDrop.ItemsSource = dragAndDropItems;

            buttonsList = new List<Button>()
            {
                btnDownloadFromList,
                btnDownloadFromFile,
                btnDownloadFromURL,
                btnCheckFolder,
                btnDeserializeFolder,
                btnDragDrop
            };
            checkBoxesList = new List<CheckBox>()
            {
                chkBoxAutoDownload,
                chkBoxIgnoreResolution,
                chkBoxKeepFilenames,
                chkBoxIgnoreResolutionFolder
            };
        }

        // checks image from URL inputted in text field
        private async void btnDownloadFromURL_Click(object sender, RoutedEventArgs e)
        {
            BlockAllButtons();

            var template = btnGif.Template;
            var myControl = (Image)template.FindName("imgControlGif", btnGif);
            var uri = new Uri("pack://application:,,,/Resources/__hatsune_miku_vocaloid_drawn_by_shigatake__99bc05c1f70100eb769dd41c90907483.gif");

            AnimationBehavior.SetSourceUri(myControl, uri);

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
        private async Task DownloadFromList(List<string> finalUrls)
        {
            string currStatus = string.Empty;
            int currPic = 0;
            int lastPic = finalUrls.Count;
            int downloadedCount = 0;
            int failedCount = 0;
            int notFoundCount = 0;
            string noMatchesImages = "Pictures with no relative matches found:\n";
            string failedToDownloadImages = "Pictures that failed to download:\n";
            string prevText = txtBlockData.Text;

            foreach (string imageURL in finalUrls)
            {
                currStatus = $"{prevText}\n\nSuccessful downloads: {downloadedCount}\nFailed downloads: {failedCount}\nNo matching images found: {notFoundCount}";
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
            currStatus = $"{prevText}\n\nSuccessful downloads: {downloadedCount}\nFailed downloads: {failedCount}\nNo matching images found: {notFoundCount}";
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

                    bool? autoDownload = chkBoxAutoDownload.IsChecked;
                    bool? ignoreResolution = chkBoxIgnoreResolutionFolder.IsChecked;
                    string checkLog = await CheckFolderFull(images, autoDownload, ignoreResolution);
                    logger += checkLog;

                    watch.Stop();
                    var elapsedMs = watch.ElapsedMilliseconds;

                    logger += string.Format("Total time to write logfile and move images: {0} seconds\n", elapsedMs / 1000.0);

                    if (!string.IsNullOrEmpty(checkLog))
                    {
                        File.AppendAllText(Utilities.GetDuplicatesLogFileName(), logger);
                    }
                }
                else
                {
                    MessageBox.Show("No images found in folder!", "Error");
                }
            }
            else
            {
                MessageBox.Show("No folder was selected!", "Error");
            }

            ReleaseAllButtons();
        }

        // checks a folder for duplicates and find better resolutions
        private async Task<string> CheckFolderFull(List<string> imagesToCheck, bool? autoDownload, bool? ignoreResolution)
        {
            List<ImageData> imagesToCheckForDuplicates = new List<ImageData>();

            string log = string.Empty;
            string status = string.Empty;
            string errorLog = string.Empty;

            string currStatus = string.Empty;
            int currFile = 0;
            int lastFile = imagesToCheck.Count;
            int foundCount = 0;
            int notFoundCount = 0;
            int noBetterResFoundCount = 0;

            foreach (string file in imagesToCheck)
            {
                currStatus = $"Better resolution found: {foundCount}\nNo better resolution: {noBetterResFoundCount}\nNo matches found: {notFoundCount}\n";
                currFile++;

                txtBlockData.Text = $"{currStatus}\nChecking file {currFile} of {lastFile}...\n";

                if (Utilities.IsImage(file))
                {
                    try
                    {
                        var responseTuple = await ImageHelper.GetResponseFromFile(file);

                        var imageData = ImageHelper.ReverseImageSearch(responseTuple.Item1, responseTuple.Item2, out status);
                        if (imageData != null)
                        {
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
                        else
                        {
                            notFoundCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        notFoundCount++;
                        errorLog += $"{status}\n{ex.Message}\n";
                    }
                }
            }

            log = ImageHelper.MarkDuplicateImages(imagesToCheckForDuplicates, ignoreResolution, out string serializedImages);
            if (!string.IsNullOrEmpty(serializedImages))
            {
                if (autoDownload == true)
                {
                    List<ImageData> images = SerializingHelper.DeserializeImageList(serializedImages);
                    await DownloadSerializedImages(images);
                }
                else
                {
                    string fileName = string.Format("{0}_{1}", DateTime.Now.ToString("yyyyMMdd_HHmmss"), Constants.BetterResolutionFilename);
                    File.WriteAllText(fileName, serializedImages);
                }
            }
            int duplicatesCount = imagesToCheckForDuplicates.Count(x => x.Duplicate == true);

            currStatus = $"Better resolution found: {foundCount}\nNo better resolution: {noBetterResFoundCount}\nNo matches found: {notFoundCount}\nPossible duplicates: {duplicatesCount}";
            if (foundCount > 0 && autoDownload != true)
            {
                currStatus += "\nImages have been sorted into corresponding folders and an XML file containing images with better resolution was saved where program was executed";
            }
            else
            {
                currStatus += "\nImages have been sorted into corresponding folders";
            }

            if (duplicatesCount > 0)
            {
                log = $"Duplicates:\n{log}";
            }
            else
            {
                log = string.Empty;
            }

            txtBlockData.Text = $"Finished checking folder! Check log for more info!\n{currStatus}";

            if (!string.IsNullOrEmpty(errorLog))
            {
                log = $"Errors:\n{errorLog}\n{log}";
            }

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
        private async Task DownloadSerializedImages(List<ImageData> imagesToDownload)
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
            foreach (CheckBox c in checkBoxesList)
            {
                c.IsEnabled = false;
            }
        }

        private void ReleaseAllButtons()
        {
            foreach (Button b in buttonsList)
            {
                b.IsEnabled = true;
            }
            foreach (CheckBox c in checkBoxesList)
            {
                c.IsEnabled = true;
            }
        }

        private void LstBoxDragAndDrop_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void LstBoxDragAndDrop_Drop(object sender, DragEventArgs e)
        {
            FileType ft = FileType.Other;
            string[] draggedFiles = new string[] { };
            string draggedText = string.Empty;
            string[] draggedImages = new string[] { };

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                draggedFiles = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            }

            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                draggedText = (string)e.Data.GetData(DataFormats.Text, false);
            }

            if (draggedFiles.Count() > 0)
            {
                foreach (string s in draggedFiles)
                {
                    if (Directory.Exists(s))
                    {
                        ft = FileType.Directory;
                    }
                    else
                    {
                        if (s.EndsWith(".txt"))
                        {
                            ft = FileType.Txt;
                        }
                        else if (s.EndsWith(".jpg") || s.EndsWith(".jpeg") || s.EndsWith(".jfif")
                            || s.EndsWith(".tiff") || s.EndsWith(".gif")
                            || s.EndsWith(".png") || s.EndsWith(".bmp"))
                        {
                            ft = FileType.Image;
                        }
                        else
                        {
                            ft = FileType.Other;
                        }
                    }

                    Tuple<FileType, string> tempTuple = new Tuple<FileType, string>(ft, s);
                    dragAndDropItems.Add(tempTuple);
                }
            }

            if (!String.IsNullOrEmpty(draggedText))
            {
                string[] splitDraggedText = draggedText.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                ft = FileType.URL;

                foreach (string s2 in splitDraggedText)
                {
                    if (!String.IsNullOrEmpty(s2))
                    {
                        Tuple<FileType, string> tempTuple = new Tuple<FileType, string>(ft, s2.Trim());
                        dragAndDropItems.Add(tempTuple);
                    }
                }
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                IDataObject d = Clipboard.GetDataObject();
                FileType ft = FileType.Other;

                // Check if text is copied
                if (d.GetDataPresent(DataFormats.Text))
                {
                    string url = Clipboard.GetText();
                    if (!String.IsNullOrEmpty(url))
                    {
                        Tuple<FileType, string> tempTuple = new Tuple<FileType, string>(FileType.URL, url);
                        dragAndDropItems.Add(tempTuple);
                    }
                }
                // Check if local file or directory has been copied
                else if (d.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])d.GetData(DataFormats.FileDrop);

                    foreach (string s in files)
                    {
                        if (Directory.Exists(s))
                        {
                            ft = FileType.Directory;
                        }
                        else
                        {
                            if (s.EndsWith(".txt"))
                            {
                                ft = FileType.Txt;
                            }
                            else if (s.EndsWith(".jpg") || s.EndsWith(".jpeg") || s.EndsWith(".jfif")
                                || s.EndsWith(".tiff") || s.EndsWith(".gif")
                                || s.EndsWith(".png") || s.EndsWith(".bmp"))
                            {
                                ft = FileType.Image;
                            }
                            else
                            {
                                ft = FileType.Other;
                            }
                        }

                        Tuple<FileType, string> tempTuple = new Tuple<FileType, string>(ft, s);
                        dragAndDropItems.Add(tempTuple);

                    }
                }
                // Check if online image has been copied
                else if (d.GetDataPresent(DataFormats.Html))
                {
                    var files = d.GetData(DataFormats.Html);
                    string imgUrl = Utilities.GetUrlFromClipboardImage(files.ToString());
                    Tuple<FileType, string> tempTuple = new Tuple<FileType, string>(FileType.URL, imgUrl);
                    dragAndDropItems.Add(tempTuple);
                }
            }

            else if (e.Key == Key.Delete && lstBoxDragAndDrop.HasItems && lstBoxDragAndDrop.SelectedItems != null)
            {
                List<Tuple<FileType, string>> itemsToDelete = new List<Tuple<FileType, string>>();

                foreach (var item in lstBoxDragAndDrop.SelectedItems)
                {
                    itemsToDelete.Add((Tuple<FileType, string>)item);
                }

                foreach (Tuple<FileType, string> delItem in itemsToDelete)
                {
                    dragAndDropItems.Remove(delItem);
                }
            }
        }

        private async void BtnDragDrop_Click(object sender, RoutedEventArgs e)
        {
            BlockAllButtons();

            if (dragAndDropItems.Count > 0)
            {
                List<string> URLs = new List<string>();
                List<string> filenames = new List<string>();

                foreach (Tuple<FileType, string> tuple in dragAndDropItems)
                {
                    if (tuple.Item1 == FileType.Image)
                    {
                        filenames.Add(tuple.Item2);
                    }
                    else if (tuple.Item1 == FileType.URL)
                    {
                        URLs.Add(tuple.Item2);
                    }
                    else if (tuple.Item1 == FileType.Txt)
                    {
                        URLs.AddRange(Utilities.ParseURLs(tuple.Item2));
                    }
                    else if (tuple.Item1 == FileType.Directory)
                    {
                        filenames.AddRange(Utilities.GetAllImagesFromFolder(tuple.Item2));
                    }
                }

                txtBlockData.Text = String.Format($"Finished checking items:{Environment.NewLine}Total URLs: {URLs.Count}{Environment.NewLine}Total files to check: {filenames.Count}");

                if (URLs.Count > 0)
                {
                    await DownloadFromList(URLs);
                }

                if (filenames.Count > 0)
                {

                }
            }
            else
            {
                txtBlockData.Text = "Drag&Drop list has no items!";
            }

            ReleaseAllButtons();

        }
    }
}
