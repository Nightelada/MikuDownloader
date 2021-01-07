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
using System.Windows.Threading;
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
        private ObservableCollection<Tuple<FileType, string>> dragAndDropItems;
        private List<ImageData> allImages;
        private readonly DispatcherTimer gifTimer = new DispatcherTimer(DispatcherPriority.Render);

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
                chkBoxDownload,
                chkBoxSort
            };
            allImages = new List<ImageData>();

            lblVersion.Content = Utilities.GetAppVersion();

            gifTimer.Tick += new EventHandler(GifTimer_tick);
            gifTimer.Interval = new TimeSpan(0, 0, 0, 0, 5000);
            gifTimer.IsEnabled = true;
            gifTimer.Start();
        }

        private void GifTimer_tick(object sender, EventArgs e)
        {
            SetRandomGif();
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
                                Tuple<FileType, string> tempTuple;
                                if (Utilities.CheckIfRealUrl(row))
                                {
                                    tempTuple = new Tuple<FileType, string>(FileType.URL, row.Trim());
                                }
                                else
                                {
                                    tempTuple = new Tuple<FileType, string>(FileType.Other, row.Trim());
                                }
                                dragAndDropItems.Add(tempTuple);
                            }
                        }
                        else
                        {
                            Tuple<FileType, string> tempTuple;

                            if (Utilities.CheckIfRealUrl(url))
                            {
                                tempTuple = new Tuple<FileType, string>(FileType.URL, url);
                            }
                            else
                            {
                                tempTuple = new Tuple<FileType, string>(FileType.Other, url);
                            }
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

            else if (e.Key == Key.A && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                lstBoxDragAndDrop.SelectAll();
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

        // Open the main download directory if it exists
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

        // Open the main log directory if it exists
        private void btnLogsFolder_Click(object sender, RoutedEventArgs e)
        {
            string mainLogDir = Constants.MainLogDirectory;

            if (Directory.Exists(mainLogDir))
            {
                Process.Start("explorer.exe", mainLogDir);
            }
            else
            {
                txtBlockData.Text = "No logs have been recorded yet!";
            }
        }

        // Checks and downloads all images from the items in the list box
        private async void BtnDragDrop_Click(object sender, RoutedEventArgs e)
        {
            BlockAllButtons();

            if (dragAndDropItems.Count > 0)
            {
                await this.Dispatcher.Invoke(() => MainLoad());
            }
            else
            {
                txtBlockData.Text = "Drag&Drop list has no items!";
            }

            ReleaseAllButtons();
        }

        private async Task MainLoad()
        {
            Tuple<List<string>, List<string>> dragNDropTuple;
            dragNDropTuple = await CheckDragNDropList();

            txtBlockData.Text = string.Format($"Finished checking items:\nTotal URLs to check: {dragNDropTuple.Item1.Count}\nTotal files to check: {dragNDropTuple.Item2.Count}");

            if (dragNDropTuple.Item1.Count > 0)
            {
                await ReverseSearchFURLs(dragNDropTuple.Item1);
            }

            if (dragNDropTuple.Item2.Count > 0)
            {
                await ReverseSearchFiles(dragNDropTuple.Item2);
            }

            if (chkBoxDownload.IsChecked == true)
            {
                await DownloadImages(allImages);

                if (chkBoxSort.IsChecked == true)
                {
                    await SortNonLoadedImages(allImages);
                }
            }
            else
            {
                Utilities.SaveSerializedList(Utilities.SerializeImageList(allImages));
            }

            allImages = new List<ImageData>();
        }

        private async Task<Tuple<List<string>,List<string>>> CheckDragNDropList()
        {
            List<string> allURLs = new List<string>();
            List<string> allFiles = new List<string>();

            foreach (Tuple<FileType, string> tuple in dragAndDropItems)
            {
                if (tuple.Item1 == FileType.Image)
                {
                    allFiles.Add(tuple.Item2);
                }
                else if (tuple.Item1 == FileType.URL)
                {
                    allURLs.Add(tuple.Item2);
                }
                else if (tuple.Item1 == FileType.Txt)
                {
                    allURLs.AddRange(Utilities.ParseURLs(tuple.Item2));
                }
                else if (tuple.Item1 == FileType.Directory)
                {
                    await Task.Run(() => allFiles.AddRange(Utilities.GetAllImagesFromFolder(tuple.Item2)));
                }
            }

            return new Tuple<List<string>, List<string>>(allURLs, allFiles);
        }

        // Checks a list of image URLs
        private async Task ReverseSearchFURLs(List<string> finalUrls)
        {
            string currStatus = string.Empty;
            int currPic = 0;
            int lastPic = finalUrls.Count;
            int foundCount = 0;
            string prevText = txtBlockData.Text;

            foreach (string imageURL in finalUrls)
            {
                currStatus = $"{prevText}\n\nMatching images found: {foundCount}/{lastPic}";
                currPic++;
                string status = string.Empty;
                txtBlockData.Text = $"{currStatus}\nChecking image {currPic} of {lastPic}...\n";

                try
                {
                    var responseTuple = await ImageHelper.GetResponseFromURL(imageURL);

                    var imageList = ImageHelper.ReverseImageSearch(responseTuple.Item1, responseTuple.Item2, FileType.URL, out status);

                    if (imageList != null && imageList.MatchingImages.Count > 0)
                    {
                        foundCount++;
                        allImages.Add(imageList);
                    }
                    else
                    {
                        allImages.Add(new ImageData(imageURL, FileType.URL));
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        status += string.Format("Failed to reverse search image!\n{0}\n{1}\n", ex.Message, ex.InnerException.Message);
                    }
                    else
                    {
                        status += string.Format("Failed to reverse search image!\n{0}\n", ex.Message);
                    }
                }
                finally
                {
                    Utilities.LogSearch(status);
                }
            }

            currStatus = $"{prevText}\n\nMatching images found: {foundCount}/{lastPic}";
            txtBlockData.Text = $"{currStatus}\nFinished checking URLs list! Check log for more info!";
        }

        // Checks a list of image files
        private async Task ReverseSearchFiles(List<string> finalFiles)
        {
            string currStatus;
            int currPic = 0;
            int lastPic = finalFiles.Count;
            int foundCount = 0;
            string prevText = txtBlockData.Text;

            foreach (string imageFile in finalFiles)
            {
                currStatus = $"{prevText}\n\nMatching images found: {foundCount}/{lastPic}";
                currPic++;
                string status = string.Empty;
                txtBlockData.Text = $"{currStatus}\nChecking image {currPic} of {lastPic}...\n";

                try
                {
                    var responseTuple = await ImageHelper.GetResponseFromFile(imageFile);

                    var imageList = ImageHelper.ReverseImageSearch(responseTuple.Item1, responseTuple.Item2, FileType.Image, out status);

                    if (imageList != null && imageList.MatchingImages.Count > 0)
                    {
                        allImages.Add(imageList);
                        foundCount++;
                    }
                    else
                    {
                        txtBlockData.Text += $"No matches found for {Path.GetFileName(imageFile)}!";
                        allImages.Add(new ImageData(imageFile, FileType.Image));
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        status += string.Format("Failed to reverse search image!\n{0}\n{1}\n", ex.Message, ex.InnerException.Message);
                    }
                    else
                    {
                        status += string.Format("Failed to reverse search image!\n{0}\n", ex.Message);
                    }
                }
                finally
                {
                    Utilities.LogSearch(status);
                }
            }

            currStatus = $"{prevText}\n\nMatching images found: {foundCount}/{lastPic}";
            txtBlockData.Text = $"{currStatus}\nFinished checking files list! Check log for more info!";
        }

        // Downloads a list of images
        private async Task DownloadImages(List<ImageData> images)
        {
            string currStatus;
            int currPic = 0;
            int lastPic = images.Count;
            int downloadedCount = 0;
            int failDownloadedCount = 0;
            int noImagesFoundCount = 0;
            string prevText = txtBlockData.Text;

            foreach (ImageData imageFile in images)
            {
                currStatus = $"{prevText}\n\nSuccessful downloads: {downloadedCount}\nFailed downloads: {failDownloadedCount}\nNo images found: {noImagesFoundCount}";
                currPic++;
                string status = $"Attempting to download image(s) for {imageFile.OriginalImage}\n";
                txtBlockData.Text = $"{currStatus}\nDownloading image {currPic} of {lastPic}...\n";

                try
                {
                    string errorText = string.Empty;

                    if (imageFile.MatchingImages.Count > 0)
                    {
                        await Task.Run(() => errorText = ImageHelper.DownloadBestImage(imageFile.MatchingImages));

                        if (string.IsNullOrEmpty(errorText))
                        {
                            status += "Successfully downloaded image!\n";
                            downloadedCount++;
                            imageFile.HasBeenDownloaded = true;
                        }
                        else
                        {
                            status += $"Failed to download image. {errorText}\n";
                            failDownloadedCount++;
                            imageFile.HasBeenDownloaded = false;
                        }
                    }
                    else
                    {
                        status += $"No matching images found.\n";
                        imageFile.HasBeenDownloaded = null;
                        noImagesFoundCount++;
                    }

                }
                catch (Exception ex)
                {
                    failDownloadedCount++;
                    imageFile.HasBeenDownloaded = false;

                    if (ex.InnerException != null)
                    {
                        status += string.Format("Failed to download image!\n{0}\n{1}\n", ex.Message, ex.InnerException.Message);
                    }
                    else
                    {
                        status += string.Format("Failed to download image!\n{0}\n", ex.Message);
                    }
                }
                finally
                {
                    Utilities.LogDownload(status);
                }
            }

            currStatus = $"{prevText}\n\nSuccessful downloads: {downloadedCount}\nFailed downloads: {failDownloadedCount}\nNo images found: {noImagesFoundCount}";
            txtBlockData.Text = $"{currStatus}\nFinished downloading images! Check log for more info!";
        }

        private async Task SortNonLoadedImages(List<ImageData> images)
        {
            List<ImageData> urls = new List<ImageData>();
            List<ImageData> files = new List<ImageData>();

            foreach (ImageData image in images)
            {
                if (image.HasBeenDownloaded == null || image.HasBeenDownloaded == false)
                {
                    if (image.OriginalImageType == FileType.URL)
                    {
                        urls.Add(image);
                    }
                    else if (image.OriginalImageType == FileType.Image)
                    {
                        files.Add(image);
                    }
                }
            }

            await SortFiles(files);
            await SortURLs(urls);
        }

        // Sorta not loaded and fail loaded local images
        private async Task SortFiles(List<ImageData> files)
        {
            foreach (ImageData image in files)
            {
                try
                {
                    string copyTo = string.Empty;

                    if (image.HasBeenDownloaded == null)
                    {
                        copyTo = Path.Combine(Utilities.GetNotLoadedDirectory(), Path.GetFileName(image.OriginalImage));
                        Directory.CreateDirectory(Utilities.GetNotLoadedDirectory());
                    }
                    else if (image.HasBeenDownloaded == false)
                    {
                        copyTo = Path.Combine(Utilities.GetFailLoadedDirectory(), Path.GetFileName(image.OriginalImage));
                        Directory.CreateDirectory(Utilities.GetFailLoadedDirectory());
                    }

                    await Task.Run(() => File.Copy(image.OriginalImage, copyTo));// Try to copy
                }
                catch (IOException ex)
                {
                    Utilities.LogSort($"Failed to move the following file: {image.OriginalImage}\n{ex.Message}\n");
                }
            }
        }

        // Sorts not loaded and fail loaded URL images
        private async Task SortURLs(List<ImageData> urls)
        {
            foreach (ImageData image in urls)
            {
                try
                {
                    if (image.HasBeenDownloaded == null)
                    {
                        await Task.Run(() => Utilities.SaveImage(Utilities.GetNotLoadedDirectory(), image.OriginalImage, string.Empty));

                    }
                    else if (image.HasBeenDownloaded == false)
                    {
                        await Task.Run(() => Utilities.SaveImage(Utilities.GetFailLoadedDirectory(), image.OriginalImage, string.Empty));
                    }
                }
                catch (IOException ex)
                {
                    Utilities.LogSort($"Failed to download image from original URL: {image.OriginalImage}\n{ex.Message}\n");
                }
            }
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
    }
}
