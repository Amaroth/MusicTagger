using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace MusicTagger.GUI
{
    /// <summary>
    /// Interaction logic for YouTubeDownloadDialog.xaml
    /// </summary>
    public partial class YouTubeDownloadDialog : Window, IDisposable
    {
        private bool disposed = false;
        private SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);

        /// <summary>
        /// Returns the unser's input in the dialog, Item1 being the URL, Item2 being the Path.
        /// </summary>
        public Tuple<string, string> GetAnswers()
        {
            return new Tuple<string, string>(URLTextBox.Text, PathTextBox.Text);
        }

        public YouTubeDownloadDialog()
        {
            InitializeComponent();
        }

        private void SelectPathButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog() { Filter = "MP3 file (*.mp3)|*.mp3" };
            if (saveFileDialog.ShowDialog() == true)
                PathTextBox.Text = saveFileDialog.FileName;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                handle.Dispose();

            disposed = true;
        }
    }
}
