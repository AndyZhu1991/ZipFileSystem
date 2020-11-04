using System;
using System.Windows;

namespace ZipFileSystem
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

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            CurrentApplication().Shutdown();
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenZipFile();
        }

        private void OpenZipFile()
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
                CurrentApplication().MountZipFile(filename);
            }
        }

        private App CurrentApplication()
        {
            return (App)Application.Current;
        }
    }
}
