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
                btnDragDrop
            };
            checkBoxesList = new List<CheckBox>()
            {
            };
        }

        // Checks a list of Image URLs
        private async Task DownloadFromURLs(List<string> finalUrls)
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

            List<ImageData> allImages = new List<ImageData>();

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
                            imageList.HasBeenDownloaded = true;
                        }
                        else
                        {
                            status += errorText + "\n";
                            txtBlockData.Text += $"{errorText} Check logs for more info!";
                            failedCount++;
                            failedToDownloadImages += imageURL + "\n";
                            imageList.HasBeenDownloaded = false;
                        }

                        allImages.Add(imageList);

                    }
                    else
                    {
                        txtBlockData.Text += $"No matches found for {Path.GetFileName(imageURL)}!";
                        notFoundCount++;
                        noMatchesImages += imageURL + "\n";
                        allImages.Add(new ImageData(imageURL));
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

        // Checks a list of Image files
        private async Task<List<ImageData>> DownloadFromFiles(List<string> finalFiles)
        {
            string currStatus = string.Empty;
            int currPic = 0;
            int lastPic = finalFiles.Count;
            int downloadedCount = 0;
            int failedCount = 0;
            int notFoundCount = 0;
            string noMatchesImages = "Pictures with no relative matches found:\n";
            string failedToDownloadImages = "Pictures that failed to download:\n";
            string prevText = txtBlockData.Text;

            List<ImageData> allImages = new List<ImageData>();

            foreach (string imageFile in finalFiles)
            {
                currStatus = $"{prevText}\n\nSuccessful downloads: {downloadedCount}\nFailed downloads: {failedCount}\nNo matching images found: {notFoundCount}";
                currPic++;
                string status = string.Empty;
                txtBlockData.Text = $"{currStatus}\nChecking image {currPic} of {lastPic}...\n";

                try
                {
                    var responseTuple = await ImageHelper.GetResponseFromFile(imageFile);

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
                            imageList.HasBeenDownloaded = true;
                        }
                        else
                        {
                            status += errorText + "\n";
                            txtBlockData.Text += $"{errorText} Check logs for more info!";
                            failedCount++;
                            failedToDownloadImages += imageFile + "\n";
                            imageList.HasBeenDownloaded = false;
                        }

                        allImages.Add(imageList);

                    }
                    else
                    {
                        txtBlockData.Text += $"No matches found for {Path.GetFileName(imageFile)}!";
                        notFoundCount++;
                        noMatchesImages += imageFile + "\n";
                        allImages.Add(new ImageData(imageFile));
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    failedToDownloadImages += imageFile + "\n";
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

            return allImages;
        }

        // Makes all UI buttons not active
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

        // Makes all UI buttons active
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

        // Allows files to be dropped inside the list box
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

        // Performs different action based on type of file that is dropped inside the list box
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
                        else if (s.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase)
                                || s.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase)
                                || s.EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase)
                                || s.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
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
                // checks to see if text is multiple lines
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

        // Performs different action when items are pasted inside application or the Delete key is pressed while items are selected from the list box
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
                        string[] checkMultipleRows = url.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        if (checkMultipleRows.Count() > 1)
                        {
                            foreach (string row in checkMultipleRows)
                            {
                                Tuple<FileType, string> tempTuple = new Tuple<FileType, string>(FileType.URL, row.Trim());
                                dragAndDropItems.Add(tempTuple);
                            }
                        }
                        else
                        {
                            Tuple<FileType, string> tempTuple = new Tuple<FileType, string>(FileType.URL, url);
                            dragAndDropItems.Add(tempTuple);
                        }
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
                            else if (s.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) 
                                || s.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase)
                                || s.EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase) 
                                || s.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
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

        // Checks and downloads all images from the items in the list box
        private async void BtnDragDrop_Click(object sender, RoutedEventArgs e)
        {
            BlockAllButtons();
            SetRandomGif();

            if (dragAndDropItems.Count > 0)
            {
                List<string> URLs = new List<string>();
                List<string> filenames = new List<string>();

                List<ImageData> localImages = new List<ImageData>();

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

                txtBlockData.Text = String.Format($"Finished checking items:\nTotal URLs: {URLs.Count}\nTotal files to check: {filenames.Count}");

                if (URLs.Count > 0)
                {
                    await DownloadFromURLs(URLs);
                }

                if (filenames.Count > 0)
                {
                    localImages = await DownloadFromFiles(filenames);
                    string notDownloadedFilename = Utilities.GetNotDownloadedLinksFilename();

                    foreach (ImageData image in localImages)
                    {
                        string originalFile = image.OriginalImage;
                        string notLoadedDir = Utilities.GetNotLoadedDirectory();
                        string failLoadedDir = Utilities.GetFailLoadedDirectory();

                        List<string> failLoadedURLs = new List<string>(); 

                        try
                        {
                            string copyTo = string.Empty;

                            if (image.HasBeenDownloaded == null)
                            {
                                copyTo = Path.Combine(notLoadedDir, Path.GetFileName(image.OriginalImage));
                                Directory.CreateDirectory(notLoadedDir);
                                File.Copy(image.OriginalImage, copyTo); // Try to copy
                            }
                            else if (image.HasBeenDownloaded == false)
                            {
                                copyTo = Utilities.GetFailLoadedDirectory();
                                copyTo = Path.Combine(failLoadedDir, Path.GetFileName(image.OriginalImage));
                                Directory.CreateDirectory(failLoadedDir);
                                failLoadedURLs.AddRange(image.GetAllMatchingImages());
                                File.Copy(image.OriginalImage, copyTo); // Try to copy
                            }
                        }
                        catch (IOException ex)
                        {
                            string currStatus = txtBlockData.Text;
                            txtBlockData.Text = string.Format($"{currStatus}\nFailed to move a file! {0}\n", ex.Message);
                        }

                        File.AppendAllLines(notDownloadedFilename, failLoadedURLs);
                    }
                }
            }
            else
            {
                txtBlockData.Text = "Drag&Drop list has no items!";
            }

            ReleaseAllButtons();
        }

        // Changes the gif image to a random one from Resources
        private void SetRandomGif()
        {
            Random random = new Random();

            var template = btnGif.Template;
            var myControl = (Image)template.FindName("imgControlGif", btnGif);

            var files = Utilities.GetResourcesUnder("Resources/MikuGifs");

            int randomIndex = random.Next(0, files.Count() - 1);
            
            var file = files.ElementAt(randomIndex);

            var uri = new Uri("pack://application:,,,/Resources/Mikugifs/" + file);

            AnimationBehavior.SetSourceUri(myControl, uri);
        }

        // Open the main directory if it exists
        private void BtnLoaderFolder_Click(object sender, RoutedEventArgs e)
        {
            string mainDirPath = Constants.MainDirectory;

            if (Directory.Exists(mainDirPath))
            {
                Process.Start("explorer.exe", mainDirPath);
            }
            else
            {
                txtBlockData.Text = "No images have been loaded yet!";
            }
        }
    }
}
