using System.Windows;
using System.Windows.Controls;

namespace MusicTagger2.GUI
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class OptionsInputDialog : Window
    {
        private string answer;

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

        public string GetAnswer()
        {
            return answer;
        }
    }
}
