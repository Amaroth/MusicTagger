using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace MusicTagger.GUI
{
    /// <summary>
    /// Interaction logic for StringInputDialog.xaml
    /// </summary>
    public partial class StringInputDialog : Window, IDisposable
    {
        private bool disposed = false;
        private SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);

        public string Answer
        {
            get { return AnswerTextBox.Text; }
        }

        public StringInputDialog(string question)
        {
            InitializeComponent();

            QuestionTextBlock.Text = question;
            AnswerTextBox.Text = "";
        }

        public StringInputDialog(string question, string defaultAnswer = "")
        {
            InitializeComponent();

            QuestionTextBlock.Text = question;
            AnswerTextBox.Text = defaultAnswer;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            AnswerTextBox.SelectAll();
            AnswerTextBox.Focus();
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
