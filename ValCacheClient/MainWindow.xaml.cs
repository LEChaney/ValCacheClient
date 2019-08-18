using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ServiceModel;

namespace ValCacheClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<string> RemoteFiles { get; set; }
        public ObservableCollection<string> LocalFiles { get; set; }

        public const string DOWNLOAD_DIR = @".\downloads";

        public MainWindow()
        {
            this.RemoteFiles = new ObservableCollection<string>();
            this.LocalFiles = new ObservableCollection<string>();

            InitializeComponent();

            // Refresh the list of downloaded files
            RefreshDownloadedFilesList();

            // Setup folder watcher for displaying downloaded files
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = DOWNLOAD_DIR;
            watcher.NotifyFilter = NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.FileName
                                 | NotifyFilters.DirectoryName;
            watcher.Filter = "*.*";

            // Add event handlers.
            watcher.Changed += OnDownloadFolderChanged;
            watcher.Created += OnDownloadFolderChanged;
            watcher.Deleted += OnDownloadFolderChanged;
            watcher.Renamed += OnDownloadFolderChanged;

            watcher.EnableRaisingEvents = true;
        }

        private void OnDownloadFolderChanged(object source, FileSystemEventArgs e)
        {
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                RefreshDownloadedFilesList();
            });
        }

        // Refresh locally downloaded files list
        private void RefreshDownloadedFilesList()
        {
            LocalFiles.Clear();
            DirectoryInfo d = new DirectoryInfo(DOWNLOAD_DIR);
            FileInfo[] files = d.GetFiles("*.*");
            foreach (FileInfo file in files)
            {
                LocalFiles.Add(file.Name);
            }
        }

        // Enable / disable download button based on selection in listview
        private void RemoteFileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RemoteFileList.SelectedItem != null)
                DownloadBtn.IsEnabled = true;
            else
                DownloadBtn.IsEnabled = false;
        }

        // Download selected file
        private void Download_Click(object sender, RoutedEventArgs e)
        {
            string filename = RemoteFileList.SelectedItem.ToString();
            string savePath = Path.Combine(DOWNLOAD_DIR, filename);

            using (var client = new FilerServerProxyService.FileServerClient())
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                    using (var fileStream = new FileStream(savePath, FileMode.Create))
                    {
                        client.GetFile(filename).CopyTo(fileStream);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.Message);
                    MessageBox.Show(ex.Message, "Error");
                }
            }
        }

        // Refresh the remote file list
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshRemoteFiles();
        }

        private void RefreshRemoteFiles()
        {
            using (var client = new FilerServerProxyService.FileServerClient())
            {
                RemoteFiles.Clear();

                try
                {
                    string[] availableFiles = client.GetAvailableFiles();
                    foreach (string file in availableFiles)
                    {
                        RemoteFiles.Add(file);
                    }
                }
                // Display error and continue
                catch (Exception ex)
                {
                    Trace.TraceError(ex.Message);
                    MessageBox.Show(ex.Message, "Error");
                }
            }
        }

        // Opens a downloaded file
        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            string filename = LocalFilesList.SelectedItem.ToString();
            string filePath = Path.Combine(DOWNLOAD_DIR, filename);

            try
            {
                System.Diagnostics.Process.Start(filePath);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                MessageBox.Show(ex.Message, "Error");
                RefreshDownloadedFilesList();
            }
        }

        private void LocalFilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LocalFilesList.SelectedItem != null)
                OpenFileBtn.IsEnabled = true;
            else
                OpenFileBtn.IsEnabled = false;
        }
    }
}
