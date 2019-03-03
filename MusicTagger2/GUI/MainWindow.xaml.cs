using Microsoft.Win32;
using MusicTagger2.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
            LoadWindowTitle();
            importListView.ItemsSource = core.importList;
            tagListView.ItemsSource = core.tags;
            ReloadViews();
        }

        /// <summary>
        /// Update window title and include opened settings file name.
        /// </summary>
        private void LoadWindowTitle()
        {
            Title = "Music Tagger 2.1";
            if ((core.SettingsFilePath != null) && (core.SettingsFilePath != ""))
                Title += " - " + Path.GetFileName(core.SettingsFilePath);
        }

        #region Menu functions...
        /// <summary>
        /// Create a new settings file.
        /// </summary>
        public void NewFile()
        {
            timer.Stop();

            var saveFileDialog = new SaveFileDialog { Filter = "Xml file (*.xml)|*.xml" };
            if (saveFileDialog.ShowDialog() == true)
            {
                core.NewSettings(saveFileDialog.FileName);
                ReloadViews();
                StartTimer();
            }

            LoadWindowTitle();
        }

        /// <summary>
        /// Open a settings file.
        /// </summary>
        private void OpenFile()
        {
            timer.Stop();

            var openFileDialog = new OpenFileDialog() { Filter = "Xml file (*.xml)|*.xml" };
            if (openFileDialog.ShowDialog() == true)
            {
                core.LoadSettings(openFileDialog.FileName);
                ReloadViews();
                StartTimer();
            }

            LoadWindowTitle();
        }

        /// <summary>
        /// Save current settings file. If none is opened, go to Save As.
        /// </summary>
        private void SaveFile()
        {
            if (core.SettingsFilePath != null)
            {
                core.SaveSettings(core.SettingsFilePath);
                LoadWindowTitle();
            }
            else
                SaveAsFile();
        }

        /// <summary>
        /// Save current settings into new file.
        /// </summary>
        private void SaveAsFile()
        {
            var saveFileDialog = new SaveFileDialog { Filter = "Xml file (*.xml)|*.xml" };
            if (saveFileDialog.ShowDialog() == true)
                core.SaveSettings(saveFileDialog.FileName);

            LoadWindowTitle();
        }
        #endregion

        #region Event handlers...
        #region Menu event handlers...
        private void NewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            NewFile();
        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFile();
        }

        private void SaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        private void SaveAsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveAsFile();
        }
        #endregion

        #region Play panel event handlers...
        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!core.IsReallyPlaying())
                core.Play();
            else
                core.Pause();
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            core.Previous();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            core.Next();
        }

        private void FirstButton_Click(object sender, RoutedEventArgs e)
        {
            core.First();
        }

        private void LastButton_Click(object sender, RoutedEventArgs e)
        {
            core.Last();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            core.Stop();
        }

        private void MuteUnmuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (volumeSlider.IsEnabled)
            {
                MuteUnmuteButton.Content = "Unmute";
                volumeSlider.IsEnabled = false;
                core.Mute();
            }
            else
            {
                MuteUnmuteButton.Content = "Mute";
                volumeSlider.IsEnabled = true;
                core.Unmute();
            }
        }
        #endregion

        #region Tag management event handlers...
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
        #endregion

        #region Playlist event handlers...
        private void BuildPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            Core.Core.FilterType filter;
            if (standardFilterRadio.IsChecked == true)
                filter = Core.Core.FilterType.Standard;
            else if (andFilterRadio.IsChecked == true)
                filter = Core.Core.FilterType.And;
            else
                filter = Core.Core.FilterType.Or;

            playListView.ItemsSource = core.CreatePlaylist(selectedTags, filter);
            ReloadColumnWidths();
        }

        private void RetagSongsButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (Song s in playListView.SelectedItems)
                if (!core.importList.Contains(s))
                    core.importList.Add(s);
            ReloadColumnWidths();
        }

        private void RenameSongButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPlaylistSongs.Count > 0)
            {
                var selectedSong = GetFirstSelectedPlaylistSong();
                var inputDialog = new StringInputDialog("Change name and/or path of the first selected song:", selectedSong.FullPath);
                if (inputDialog.ShowDialog() == true)
                    core.MoveSong(selectedSong, inputDialog.Answer);
            }
        }

        private void MoveSongsButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPlaylistSongs.Count > 0)
            {
                var selectedSong = GetFirstSelectedPlaylistSong();
                var inputDialog = new StringInputDialog("Choose new path of selected songs:", selectedSong.FullPath);
                if (inputDialog.ShowDialog() == true)
                {
                    var result = inputDialog.Answer;
                    if (!result.EndsWith("\\"))
                        result += "\\";
                    core.MoveSongs(selectedPlaylistSongs, result);
                }
            }
        }

        private void RemoveSongsButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = new List<Song>();
            foreach (Song s in playListView.SelectedItems)
                selectedItems.Add(s);

            if (selectedItems.Count > 0)
            {
                var oid = new OptionsInputDialog("Remove songs",
                    string.Format("Are you sure? Do you want to delete {0} song files only from settings, or from drive as well?", selectedItems.Count),
                    new string[] { "Settings only", "Drive as well", "Abort" });
                if (oid.ShowDialog() == true)
                {
                    if (oid.GetAnswer() != "Abort")
                    {
                        foreach (Song s in selectedItems)
                            core.RemoveSong(s);
                    }
                    if (oid.GetAnswer() == "Drive as well")
                        foreach (Song s in selectedItems)
                            try
                            {
                                File.Delete(s.FullPath);
                            }
                            catch { }
                }
            }
        }
        #endregion
        #endregion







        private void ReloadViews()
        {
            CollectionView playView = (CollectionView)CollectionViewSource.GetDefaultView(tagListView.ItemsSource);
            PropertyGroupDescription groupDescription = new PropertyGroupDescription("Category");
            playView.GroupDescriptions.Clear();
            playView.GroupDescriptions.Add(groupDescription);

            if (GetFirstSelectedTag() != null)
            {
                tagIDIntUpDown.Text = GetFirstSelectedTag().ID.ToString();
                tagNameTextBox.Text = GetFirstSelectedTag().Name;
                tagCategoryTextBox.Text = GetFirstSelectedTag().Category;
            }
            else
            {
                tagIDIntUpDown.Text = "Auto increment";
                tagNameTextBox.Text = "";
                tagCategoryTextBox.Text = "";
            }
            ReloadColumnWidths();
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
            var importList = new List<string>();
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

        

        private void importListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (importListView.SelectedItems.Count > 0)
                core.PlayPreview(importListView.SelectedItems[0] as Song);
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
                    PlayPauseButton.Content = "Pause";
                else
                    PlayPauseButton.Content = "Play";
            }
        }

        private void SoundSearchByNameButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SoundSearchByTagsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SoundsVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }
    }
}