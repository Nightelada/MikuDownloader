using FileTypeChecker.Abstracts;
using IqdbApi.Models;
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
using FileType = MikuDownloader.enums.FileType;
using Image = System.Windows.Controls.Image;

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

        // Used for pause-run function
        private bool loadedLatestData = false;
        private List<string> listOfImageUrls;
        private List<string> listOfImageFiles;
        public delegate void NextMikuLoaderDelegate();
        private bool continueLoading = false;
        private int lastFileLoadedIndex = 0;
        private int lastUrlLoadedIndex = 0;
        private int lastImageDownloaded = 0;
        private int lastImageSorted = 0;
        private bool finishedCheckingUrls = false;
        private bool finishedCheckingFiles = false;
        private bool finishedDownloadingFiles = false;
        private bool finishedSortingFiles = false;
        private string cementedText = string.Empty;
        private int urlsFoundCount = 0;
        private int filesFoundCount = 0;
        private int downloadedCount = 0;
        private int failDownloadedCount = 0;
        private int noImagesFoundCount = 0;

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

        private void ResetPauseParameters()
        {
            loadedLatestData = false;
            continueLoading = false;
            lastFileLoadedIndex = 0;
            lastUrlLoadedIndex = 0;
            lastImageDownloaded = 0;
            lastImageSorted = 0;
            finishedCheckingUrls = false;
            finishedCheckingFiles = false;
            finishedDownloadingFiles = false;
            finishedSortingFiles = false;
            cementedText = string.Empty;
            urlsFoundCount = 0;
            filesFoundCount = 0;
            downloadedCount = 0;
            failDownloadedCount = 0;
            noImagesFoundCount = 0;
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
                                || s.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase)
                                || s.EndsWith(".webp", StringComparison.InvariantCultureIgnoreCase))
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
            ResetPauseParameters();
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
                                || s.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase)
                                || s.EndsWith(".webp", StringComparison.InvariantCultureIgnoreCase))
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

                ResetPauseParameters();
                btnDragDrop.Content = "Go Go Miku Checker";
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

                ResetPauseParameters();
                btnDragDrop.Content = "Go Go Miku Checker";
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
            if (!loadedLatestData)
            {
                ResetPauseParameters();
                Tuple<List<string>, List<string>> dragNDropTuple;
                dragNDropTuple = await CheckDragNDropList();
                listOfImageUrls = dragNDropTuple.Item1;
                listOfImageFiles = dragNDropTuple.Item2;

                cementedText = string.Format($"Finished checking items:\nTotal URLs to check: {listOfImageUrls.Count}\nTotal files to check: {listOfImageFiles.Count}\nPress the Go Go Miku Loader button to start/stop the loading process");

                txtBlockData.Text = cementedText;
                loadedLatestData = true;
                btnDragDrop.Content = "Go Go Miku Loader";
            }
            else
            {
                if (continueLoading)
                {
                    continueLoading = false;
                    btnDragDrop.Content = "Resume Loading";
                }
                else
                {
                    continueLoading = true;
                    btnDragDrop.Content = "Stop Loading";
                    _ = btnDragDrop.Dispatcher.BeginInvoke(
                        DispatcherPriority.Normal,
                        new NextMikuLoaderDelegate(LoadNextImage));
                }
            }
        }

        public async void LoadNextImage()
        {
            int totalUrlCount = listOfImageUrls.Count;
            int totalFilesCount = listOfImageFiles.Count;
            int totalImagesCount = allImages.Count;

            long twoHundredMB = 200 * 1024 * 1024;
            if (Utilities.GetFreeSpace() <= twoHundredMB)
            {
                continueLoading = false;
                txtBlockData.Text = $"{txtBlockData.Text}\nAvailable free space is less than 200MB!\nFree up space and click the resume button!";
                btnDragDrop.Content = "Resume Loading";
                System.Media.SystemSounds.Exclamation.Play();
                return;
            }

            if (lastUrlLoadedIndex == totalUrlCount || totalUrlCount == 0)
            {
                if (!finishedCheckingUrls)
                {
                    cementedText = txtBlockData.Text;
                }
                finishedCheckingUrls = true;
            }
            if (lastFileLoadedIndex == totalFilesCount || totalFilesCount == 0)
            {
                if (!finishedCheckingFiles)
                {
                    cementedText = txtBlockData.Text;
                }
                finishedCheckingFiles = true;
            }
            if ((lastImageDownloaded == totalImagesCount && finishedCheckingUrls && finishedCheckingFiles) || chkBoxDownload.IsChecked == false)
            {
                if (!finishedDownloadingFiles)
                {
                    cementedText = txtBlockData.Text;
                }
                finishedDownloadingFiles = true;
            }
            if ((lastImageSorted == totalImagesCount && finishedCheckingUrls && finishedCheckingFiles) || chkBoxSort.IsChecked == false)
            {
                finishedSortingFiles = true;
            }

            if (!finishedCheckingUrls)
            {
                await ReverseSearchURL(lastUrlLoadedIndex, totalUrlCount);
                lastUrlLoadedIndex++;
            }
            else if (!finishedCheckingFiles)
            {
                await ReverseSearchFile(lastFileLoadedIndex, totalFilesCount);
                lastFileLoadedIndex++;
            }
            else if (!finishedDownloadingFiles)
            {
                await DownloadImage(lastImageDownloaded, totalImagesCount);
                lastImageDownloaded++;
            }
            else if (chkBoxSort.IsChecked == true && lastImageSorted < totalImagesCount)
            {
                await SortNonLoadedImage(lastImageSorted, totalImagesCount);
                lastImageSorted++;
            }/*
            else
            {
                Utilities.SaveSerializedList(Utilities.SerializeImageList(allImages));
            }*/

            if (finishedCheckingUrls && finishedCheckingFiles && finishedDownloadingFiles && finishedSortingFiles)
            {
                continueLoading = false;
                allImages = new List<ImageData>();
                btnDragDrop.Content = "Go Go Miku Loader";

                lastFileLoadedIndex = 0;
                lastUrlLoadedIndex = 0;
                lastImageDownloaded = 0;
                lastImageSorted = 0;
            }

            if (continueLoading)
            {
                await btnDragDrop.Dispatcher.BeginInvoke(
                        DispatcherPriority.Normal,
                        new NextMikuLoaderDelegate(LoadNextImage));
            }
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
                else if (tuple.Item1 == FileType.Other)
                {
                    Utilities.LogNotChecked(tuple.Item2);
                }
            }

            return new Tuple<List<string>, List<string>>(allURLs, allFiles);
        }

        // Checks a list of image URLs
        private async Task ReverseSearchURL(int urlToLoad, int totalUrlsToLoad)
        {
            string currStatus = string.Empty;
            int currPic = urlToLoad;
            int lastPic = totalUrlsToLoad;
            string prevText = cementedText;


            currStatus = $"{prevText}\n\nMatching images found: {urlsFoundCount}/{lastPic}";
            currPic++;
            string status = string.Empty;
            txtBlockData.Text = $"{currStatus}\nChecking image {currPic} of {lastPic}...\n";

            string imageURL = listOfImageUrls[urlToLoad];
            try
            {
                var responseTuple = await ImageHelper.IqdbApiUrlSearch(imageURL);

                var imageList = ImageHelper.IqdbApiImageSearch(responseTuple.Item1, responseTuple.Item2, FileType.URL, out status);

                if (imageList != null && imageList.MatchingImages.Count > 0)
                {
                    urlsFoundCount++;
                    allImages.Add(imageList);
                }
                else
                {
                    allImages.Add(new ImageData(imageURL, FileType.URL));
                    Utilities.LogNotLoaded(imageURL);
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
                Utilities.LogNotLoaded(imageURL);
            }
            finally
            {
                Utilities.LogSearch(status);
            }
        }

        // Checks a list of image files
        private async Task ReverseSearchFile(int fileToLoad, int totalFilesToLoad)
        {
            string currStatus;
            int currPic = fileToLoad;
            int lastPic = totalFilesToLoad;
            string prevText = cementedText;


            currStatus = $"{prevText}\n\nMatching images found: {filesFoundCount}/{lastPic}";
            currPic++;
            string status = string.Empty;
            string errors = string.Empty;
            bool isError = false;
            bool webpFlag = false;
            string webpFileName = string.Empty;

            txtBlockData.Text = $"{currStatus}\nChecking image {currPic} of {lastPic}...\n";

            string imageFile = listOfImageFiles[fileToLoad];
            try
            {
                errors += string.Format("Original image: {0}\n", imageFile);
                long fileSize = Utilities.GetFileSize(imageFile);
                errors += string.Format("File size: {0}\n", Utilities.GetSizeString(fileSize));
                IFileType fType = Utilities.GetFileType(imageFile);
                errors += string.Format("File type: ({1}) {0}\n", fType.Name, fType.Extension);

                if (fileSize > Constants.MaxFileSize)
                {
                    errors += "Failed to reverse search image!\nFile is too large or corrupted!\n";
                    isError = true;
                }

                if (!Constants.AcceptedFileExtensions.Contains(fType.Extension))
                {
                    if (fType.Extension.Equals("webp"))
                    {
                        webpFileName = Utilities.ConverWebpToJpg(imageFile);
                        webpFlag = true;
                    }
                    else
                    {
                        errors += "Failed to reverse search image!\nFile is too large or corrupted!\n";
                        isError = true;
                    }
                }

                if (!isError)
                {
                    Tuple<SearchResult, string> responseTuple;
                    if (webpFlag)
                    {
                        responseTuple = await ImageHelper.IqdbApiFileSearch(webpFileName);
                    }
                    else
                    {
                        responseTuple = await ImageHelper.IqdbApiFileSearch(imageFile);
                    }

                    var imageList = ImageHelper.IqdbApiImageSearch(responseTuple.Item1, responseTuple.Item2, FileType.Image, out status);

                    if (imageList != null && imageList.MatchingImages.Count > 0)
                    {
                        allImages.Add(imageList);
                        filesFoundCount++;
                    }
                    else
                    {
                        txtBlockData.Text += $"No matches found for {Path.GetFileName(imageFile)}!";
                        allImages.Add(new ImageData(imageFile, FileType.Image));
                        Utilities.LogNotLoaded(imageFile);
                    }
                }
                else
                {
                    Utilities.LogNotChecked(imageFile);
                }
            }
            catch (Exception ex)
            {
                isError = true;
                if (ex.InnerException != null)
                {
                    status += string.Format("Failed to reverse search image!\n{0}\n{1}\n", ex.Message, ex.InnerException.Message);
                    errors += string.Format("Failed to reverse search image!\n{0}\n{1}\n", ex.Message, ex.InnerException.Message);

                }
                else
                {
                    status += string.Format("Failed to reverse search image!\n{0}\n", ex.Message);
                    errors += string.Format("Failed to reverse search image!\n{0}\n", ex.Message);
                }
                Utilities.LogNotLoaded(imageFile);
            }
            finally
            {
                Utilities.LogSearch(status);
                if (isError)
                {
                    Utilities.LogSearchErrors(errors);
                }
            }
        }

        // Downloads a list of images
        private async Task DownloadImage(int imageToDownload, int totalImagesToDownload)
        {
            string currStatus;
            int currPic = imageToDownload;
            int lastPic = totalImagesToDownload;
            string prevText = cementedText;

            ImageData imageFile = allImages[imageToDownload];


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
                        Utilities.LogFailLoaded(imageFile.OriginalImage);
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
                Utilities.LogFailLoaded(imageFile.OriginalImage);

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


            currStatus = $"{prevText}\n\nSuccessful downloads: {downloadedCount}\nFailed downloads: {failDownloadedCount}\nNo images found: {noImagesFoundCount}";
            txtBlockData.Text = $"{currStatus}\nFinished downloading images! Check log for more info!";
        }

        private async Task SortNonLoadedImage(int imageToSort, int totalImagesToSort)
        {
            List<ImageData> urls = new List<ImageData>();
            List<ImageData> files = new List<ImageData>();
            ImageData image = allImages[imageToSort];

                if (image.HasBeenDownloaded == null || image.HasBeenDownloaded == false)
                {
                    if (image.OriginalImageType == FileType.URL)
                    {
                    await SortURL(image);
                    }
                    else if (image.OriginalImageType == FileType.Image)
                    {
                    await SortFile(image);
                    }
                }
        }

        // Sorta not loaded and fail loaded local images
        private async Task SortFile(ImageData image)
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

                await Task.Run(() => File.Copy(image.OriginalImage, copyTo, true));// Try to copy
            }
            catch (IOException ex)
            {
                Utilities.LogSort($"Failed to move the following file: {image.OriginalImage}\n{ex.Message}\n");
            }
        }

        // Sorts not loaded and fail loaded URL images
        private async Task SortURL(ImageData image)
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
