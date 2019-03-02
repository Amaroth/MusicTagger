using System;
using System.Windows;

namespace MusicTagger2.GUI
{
    /// <summary>
    /// Interaction logic for StringInputDialog.xaml
    /// </summary>
    public partial class StringInputDialog : Window
    {
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

        public string Answer
        {
            get { return AnswerTextBox.Text; }
        }
    }
}
