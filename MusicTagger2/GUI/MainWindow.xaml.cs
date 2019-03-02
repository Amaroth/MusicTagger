using Microsoft.Win32;
using MusicTagger2.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MusicTagger2.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<Tag> selectedTags = new ObservableCollection<Tag>();
        private ObservableCollection<Song> selectedImportSongs = new ObservableCollection<Song>();
        private ObservableCollection<Song> selectedPlaylistSongs = new ObservableCollection<Song>();
        private Core.Core core = Core.Core.Instance;
        DispatcherTimer timer = new DispatcherTimer();
        DispatcherTimer fadeTimer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadWindowTitle()
        {
            Title = "Music Tagger 2.0";
        }

        public void NewFile()
        {
            timer.Stop();

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Xml file (*.xml)|*.xml"
            };

            OpenFileDialog folderBrowser = new OpenFileDialog
            {
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Folder Selection."
        };

            if (saveFileDialog.ShowDialog() == true)
            {
                if (folderBrowser.ShowDialog() == true)
                {
                    core.NewSettings(saveFileDialog.FileName, Path.GetDirectoryName(folderBrowser.FileName));
                    LoadData();
                    StartTimer();
                }
            }
        }

        private void OpenFile()
        {
            timer.Stop();
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Xml file (*.xml)|*.xml";
            if (openFileDialog.ShowDialog() == true)
            {
                core.LoadSettings(openFileDialog.FileName);
                LoadData();
                StartTimer();
            }
        }

        private void SaveFile()
        {
            core.SaveSettings(core.filePath);
        }

        private void SaveAsFile()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Xml file (*.xml)|*.xml"
            };
            if (saveFileDialog.ShowDialog() == true)
                core.SaveSettings(saveFileDialog.FileName);
        }

        private void LoadData()
        {
            importListView.ItemsSource = core.importList;
            tagListView.ItemsSource = core.tags;
            CollectionView playView = (CollectionView)CollectionViewSource.GetDefaultView(tagListView.ItemsSource);
            PropertyGroupDescription groupDescription = new PropertyGroupDescription("Category");
            playView.GroupDescriptions.Add(groupDescription);
        }

        private void StartTimer()
        {
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Start();
            fadeTimer.Tick += new EventHandler(fadeTimer_Tick);
        }

        private Tag GetFirstSelectedTag()
        {
            if (selectedTags.Count < 1)
                return null;
            foreach (var t in core.tags)
                if (selectedTags.Contains(t))
                    return t;
            return null;
        }

        private Song GetFirstSelectedPlaylistSong()
        {
            if (selectedPlaylistSongs.Count < 1)
                return null;
            foreach (var s in core.currentPlaylist)
                if (selectedPlaylistSongs.Contains(s))
                    return s;
            return null;
        }

        private void playButt_Click(object sender, RoutedEventArgs e)
        {
            if (!core.IsReallyPlaying())
                core.Play();
            else
                core.Pause();
        }

        private void prevButt_Click(object sender, RoutedEventArgs e)
        {
            core.Previous();
        }

        private void nextButt_Click(object sender, RoutedEventArgs e)
        {
            core.Next();
        }

        private void firstButt_Click(object sender, RoutedEventArgs e)
        {
            core.First();
        }

        private void lastButt_Click(object sender, RoutedEventArgs e)
        {
            core.Last();
        }

        private void stopButt_Click(object sender, RoutedEventArgs e)
        {
            core.Stop();
        }

        private void muteButt_Click(object sender, RoutedEventArgs e)
        {
            if (volumeSlider.IsEnabled)
            {
                muteButt.Content = "Unmute";
                volumeSlider.IsEnabled = false;
                core.Mute();
            }
            else
            {
                muteButt.Content = "Mute";
                volumeSlider.IsEnabled = true;
                core.Unmute();
            }
        }

        private void createTagButt_Click(object sender, RoutedEventArgs e)
        {
            core.CreateTag(tagNameTextBox.Text, tagCategoryTextBox.Text);
            ReloadColumnWidths();
        }

        private void editTagButt_Click(object sender, RoutedEventArgs e)
        {
            core.EditTag(GetFirstSelectedTag(), tagNameTextBox.Text, tagCategoryTextBox.Text);
            ReloadColumnWidths();
        }

        private void removeTagButt_Click(object sender, RoutedEventArgs e)
        {
            core.RemoveTag(GetFirstSelectedTag());
            ReloadColumnWidths();
        }

        private void filterButt_Click(object sender, RoutedEventArgs e)
        {
            playListView.ItemsSource = core.CreatePlaylist(selectedTags, (bool)andFilterRadio.IsChecked);
            ReloadColumnWidths();
        }

        private void assignButt_Click(object sender, RoutedEventArgs e)
        {
            core.AssignTags(selectedImportSongs, selectedTags, (bool)removeFromImportCheckbox.IsChecked, (bool)overwriteTagsCheckbox.IsChecked);
            ReloadColumnWidths();
        }

        private void importListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (Song item in e.RemovedItems)
                selectedImportSongs.Remove(item);
            foreach (Song item in e.AddedItems)
                selectedImportSongs.Add(item);
        }

        private void tagListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (Tag item in e.RemovedItems)
                selectedTags.Remove(item);
            foreach (Tag item in e.AddedItems)
                selectedTags.Add(item);
            if (GetFirstSelectedTag() != null)
            {
                tagIDIntUpDown.Text = GetFirstSelectedTag().ID.ToString();
                tagNameTextBox.Text = GetFirstSelectedTag().Name;
                tagCategoryTextBox.Text = GetFirstSelectedTag().Category;
            }
            else
                tagIDIntUpDown.Text = "Auto increment";
        }

        private void playListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (Song item in e.RemovedItems)
                selectedPlaylistSongs.Remove(item);
            foreach (Song item in e.AddedItems)
                selectedPlaylistSongs.Add(item);
        }

        private void importListView_Drop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            List<string> importList = new List<string>();
            foreach (string s in files)
            {
                if (Directory.Exists(s))
                    foreach (var f in Directory.GetFiles(s, "*.*", SearchOption.AllDirectories))
                        importList.Add(f);
                else
                    importList.Add(s);
            }
            core.AddIntoImport(importList);
            ReloadColumnWidths();
        }

        private void progressBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            double MousePosition = e.GetPosition(progressBar).X;
            progressBar.Value = e.GetPosition(progressBar).X / progressBar.ActualWidth * progressBar.Maximum;
            if (progressBar.Value == progressBar.Maximum)
                progressBar.Value--;
            core.MoveToTime((int)progressBar.Value);
        }

        private void ReloadColumnWidths()
        {
            foreach (var c in (tagListView.View as GridView).Columns)
            {
                if (double.IsNaN(c.Width))
                {
                    c.Width = c.ActualWidth;
                }
                c.Width = double.NaN;
            }
            foreach (var c in (playListView.View as GridView).Columns)
            {
                if (double.IsNaN(c.Width))
                {
                    c.Width = c.ActualWidth;
                }
                c.Width = double.NaN;
            }
            foreach (var c in (importListView.View as GridView).Columns)
            {
                if (double.IsNaN(c.Width))
                {
                    c.Width = c.ActualWidth;
                }
                c.Width = double.NaN;
            }
        }

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            core.SetVolume(volumeSlider.Value);
        }

        private void randomCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            core.Random = (bool)randomCheckBox.IsChecked;
        }

        private void repeatCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            core.Repeat = (bool)repeatCheckBox.IsChecked;
        }

        private void playListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (playListView.SelectedItems.Count > 0)
                core.PlaySong(playListView.SelectedItems[0] as Song);
        }

        private void NewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            NewFile();
        }

        private void SaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        private void SaveAsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveAsFile();
        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFile();
        }

        private void importListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (importListView.SelectedItems.Count > 0)
                core.PlayPreview(importListView.SelectedItems[0] as Song);
        }

        private void retagSongsButt_Click(object sender, RoutedEventArgs e)
        {
            foreach (Song s in playListView.SelectedItems)
                if (!core.importList.Contains(s))
                    core.importList.Add(s);
            ReloadColumnWidths();
        }

        private void renameSongButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPlaylistSongs.Count > 0)
            {
                var selectedSong = GetFirstSelectedPlaylistSong();
                var inputDialog = new StringInputDialog("Change name and/or subpath of first selected song:", selectedSong.SubPath);
                if (inputDialog.ShowDialog() == true)
                    core.MoveSong(selectedSong, inputDialog.Answer);
            }
        }

        private void moveSongsButt_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPlaylistSongs.Count > 0)
            {
                var selectedSong = GetFirstSelectedPlaylistSong();
                var inputDialog = new StringInputDialog("Choose new subdirectory path of selected songs:", selectedSong.SubDir);
                if (inputDialog.ShowDialog() == true)
                {
                    var result = inputDialog.Answer;
                    if (!result.EndsWith("\\"))
                        result += "\\";
                    core.MoveSongs(selectedPlaylistSongs, result);
                }
            }
        }

        private void removeSongButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = new List<Song>();
            foreach (Song s in playListView.SelectedItems)
                selectedItems.Add(s);

            if (selectedItems.Count > 0)
            {
                foreach (Song s in selectedItems)
                    core.RemoveSong(s);
            }
        }

        private void removeFromImportButt_Click(object sender, RoutedEventArgs e)
        {
            core.RemoveFromImport(selectedImportSongs);
        }

        private void clearImportButt_Click(object sender, RoutedEventArgs e)
        {
            core.ClearImport();
        }

        private int preFadeVolume = 0;
        private void fadeButt_Click(object sender, RoutedEventArgs e)
        {
            fadeButt.IsEnabled = false;
            preFadeVolume = core.GetVolume();
            if (preFadeVolume <= 3)
                fadeTimer.Interval = new TimeSpan(0, 0, 0, 1);
            else
                fadeTimer.Interval = new TimeSpan(0, 0, 0, 0, 3000 / preFadeVolume);
            volumeSlider.IsEnabled = false;
            fadeTimer.Start();
        }

        private void fadeTimer_Tick(object sender, EventArgs e)
        {
            if (preFadeVolume > 0)
                core.SetVolume(--preFadeVolume);
            else
            {
                core.Stop();
                volumeSlider.IsEnabled = true;
                fadeButt.IsEnabled = true;
                core.SetVolume(volumeSlider.Value);
                fadeTimer.Stop();
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (core.currentPlaylist != null)
            {
                core.CheckIsTimeForNext();

                progressBar.Maximum = core.GetCurrentLength();
                progressBar.Value = core.GetCurrentPosition();
                timeText.Text = string.Format("{0} / {1}", Utilities.GetTimeString(core.GetCurrentPosition() / 10), Utilities.GetTimeString(core.GetCurrentLength() / 10));

                var currentSong = core.GetCurrentSong();
                if (currentSong != null)
                    nameText.Text = currentSong.SongName;
                else
                {
                    nameText.Text = null;
                    progressBar.Value = 0;
                    progressBar.Maximum = 0;
                    timeText.Text = "0:00:00 / 0:00:00";
                }

                if (playListView.Items.Count > 0)
                {
                    for (int i = 0; i < playListView.Items.Count; i++)
                    {
                        if (((ListViewItem)playListView.ItemContainerGenerator.ContainerFromItem(core.currentPlaylist[i])) != null)
                            ((ListViewItem)playListView.ItemContainerGenerator.ContainerFromItem(core.currentPlaylist[i])).Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF042271"));
                    }
                    if (core.currentSongIndex > -1 && core.currentSongIndex < playListView.Items.Count)
                        if (((ListViewItem)(playListView.ItemContainerGenerator.ContainerFromIndex(core.currentSongIndex))) != null)
                            ((ListViewItem)(playListView.ItemContainerGenerator.ContainerFromIndex(core.currentSongIndex))).Foreground = new SolidColorBrush(Colors.Red);
                }

                if (core.IsReallyPlaying())
                    playButt.Content = "Pause";
                else
                    playButt.Content = "Play";
            }
        }
    }
}