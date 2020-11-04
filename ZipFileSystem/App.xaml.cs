using DokanNet;
using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using ZipFileSystem.Dokans;
using ZipFileSystem.ZipTree;

namespace ZipFileSystem
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private static char MOUNT_POINT = 'z';

        private FileTreeNode mRoot;

        private TaskbarIcon taskbarIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Length == 0)
            {
                new MainWindow().Show();
            }
            else
            {
                MountZipFile(e.Args[0]);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Dokan.Unmount(MOUNT_POINT);
            taskbarIcon?.Dispose(); //the icon would clean up automatically, but this is cleaner
            base.OnExit(e);
        }

        public void MountZipFile(string filePath)
        {
            ZipArchive zipArchive = ZipFile.OpenRead(filePath);
            IList<ZipArchiveEntry> entries = zipArchive.Entries.ToList();

            mRoot = FileTreeNode.CreateRootNode();
            var sorted = entries.OrderBy(entry => entry.FullName);
            foreach (ZipArchiveEntry entry in entries)
            {
                mRoot.Insert(new ZipEntry(entry));
            }

            MountDokan();
        }

        private void MountDokan()
        {
            Thread thread1 = new Thread(this.MountWorker);
            thread1.Start();
            taskbarIcon = (TaskbarIcon)FindResource("NotifyIcon");
        }

        private void MountWorker()
        {
            var dk = new ZipDokanOperation(mRoot);
            dk.Mount(MOUNT_POINT + ":\\", DokanOptions.DebugMode | DokanOptions.StderrOutput);
        }
    }
}
