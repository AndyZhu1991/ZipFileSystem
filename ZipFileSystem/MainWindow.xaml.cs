using DokanNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ZipFileSystem.Dokans;
using ZipFileSystem.ZipTree;

namespace ZipFileSystem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static char MOUNT_POINT = 'z';

        private FileTreeNode mRoot;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenZipFile();
            Dokan.Unmount(MOUNT_POINT);
        }

        private async void OpenZipFile()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".zip", // Default file extension
                Filter = "Compressed file (.zip)|*.zip" // Filter files by extension
            };

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;
                ZipArchive zipArchive = ZipFile.OpenRead(filename);
                IList<ZipArchiveEntry> entries = zipArchive.Entries.ToList();

                mRoot = new FileTreeNode(null);
                var sorted = entries.OrderBy(entry => entry.FullName);
                foreach (ZipArchiveEntry entry in entries)
                {
                    mRoot.Insert(entry);
                }

                MountDokan();
            }
        }

        private void MountDokan()
        {
            Thread thread1 = new Thread(this.MountWorker);
            thread1.Start();
        }

        private void MountWorker()
        {
            var dk = new ZipDokanOperation(mRoot);
            dk.Mount(MOUNT_POINT + ":\\", DokanOptions.DebugMode | DokanOptions.StderrOutput);
        }
    }
}
