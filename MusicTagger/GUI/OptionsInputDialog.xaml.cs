using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace MusicTagger.GUI
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class OptionsInputDialog : Window, IDisposable
    {
        private string answer;
        private bool disposed = false;
        private SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);

        public string GetAnswer()
        {
            return answer;
        }

        public OptionsInputDialog(string title, string question, string[] buttonTexts)
        {
            InitializeComponent();

            Title = title;
            QuestionTextBlock.Text = question;
            if (buttonTexts.Length == 0)
            {
                DialogResult = false;
                Close();
            }
            else
                foreach (var s in buttonTexts)
                {
                    var butt = new Button()
                    {
                        Content = s,
                        Margin = new Thickness(2, 5, 2, 0),
                        Padding = new Thickness(5, 2, 5, 2)
                    };
                    butt.Click += Button_Click;
                    ButtonsWrapPanel.Children.Add(butt);
                }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            answer = (sender as Button).Content.ToString(); ;
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
