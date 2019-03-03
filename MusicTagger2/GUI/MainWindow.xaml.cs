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

        private int preFadeVolume = 0;
        private DispatcherTimer fadeTimer = new DispatcherTimer();
        private DispatcherTimer playSongTimer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();
            LoadWindowTitle();
            ImportListView.ItemsSource = core.importList;
            TagListView.ItemsSource = core.tags;
            ReloadViews();

            playSongTimer.Tick += new EventHandler(Timer_Tick);
            playSongTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            playSongTimer.Start();
            fadeTimer.Tick += new EventHandler(FadeTimer_Tick);
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
            core.Stop();
            var saveFileDialog = new SaveFileDialog { Filter = "Xml file (*.xml)|*.xml" };
            if (saveFileDialog.ShowDialog() == true)
            {
                core.NewSettings(saveFileDialog.FileName);
                ReloadViews();
            }

            LoadWindowTitle();
        }

        /// <summary>
        /// Open a settings file.
        /// </summary>
        private void OpenFile()
        {
            core.Stop();
            var openFileDialog = new OpenFileDialog() { Filter = "Xml file (*.xml)|*.xml" };
            if (openFileDialog.ShowDialog() == true)
            {
                core.LoadSettings(openFileDialog.FileName);
                ReloadViews();
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
        private void NewMenuItem_Click(object sender, RoutedEventArgs e) => NewFile();

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e) => OpenFile();

        private void SaveMenuItem_Click(object sender, RoutedEventArgs e) => SaveFile();

        private void SaveAsMenuItem_Click(object sender, RoutedEventArgs e) => SaveAsFile();
        #endregion

        #region Play panel event handlers...
        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!core.IsReallyPlaying())
                core.Play();
            else
                core.Pause();
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e) => core.Previous();

        private void NextButton_Click(object sender, RoutedEventArgs e) => core.Next();

        private void FirstButton_Click(object sender, RoutedEventArgs e) => core.First();

        private void LastButton_Click(object sender, RoutedEventArgs e) => core.Last();

        private void StopButton_Click(object sender, RoutedEventArgs e) => core.Stop();

        private void MuteUnmuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (SongVolumeSlider.IsEnabled)
            {
                MuteUnmuteButton.Content = "Unmute";
                SongVolumeSlider.IsEnabled = false;
                core.Mute();
            }
            else
            {
                MuteUnmuteButton.Content = "Mute";
                SongVolumeSlider.IsEnabled = true;
                core.Unmute();
            }
        }

        private void FadeButton_Click(object sender, RoutedEventArgs e)
        {
            FadeButton.IsEnabled = false;
            preFadeVolume = core.GetVolume();
            if (preFadeVolume <= 3)
                fadeTimer.Interval = new TimeSpan(0, 0, 0, 1);
            else
                fadeTimer.Interval = new TimeSpan(0, 0, 0, 0, 3000 / preFadeVolume);
            SongVolumeSlider.IsEnabled = false;
            fadeTimer.Start();
        }

        private void SongVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => core.SetVolume(SongVolumeSlider.Value);

        private void SongProgressBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            double MousePosition = e.GetPosition(SongProgressBar).X;
            SongProgressBar.Value = e.GetPosition(SongProgressBar).X / SongProgressBar.ActualWidth * SongProgressBar.Maximum;
            if (SongProgressBar.Value == SongProgressBar.Maximum)
                SongProgressBar.Value--;
            core.MoveToTime((int)SongProgressBar.Value);
        }

        private void RandomCheckBox_Checked(object sender, RoutedEventArgs e) => core.Random = (bool)RandomCheckBox.IsChecked;

        private void RepeatCheckBox_Checked(object sender, RoutedEventArgs e) => core.Repeat = (bool)RepeatCheckBox.IsChecked;
        #endregion

        #region Tag management event handlers...
        private void CreateTagButton_Click(object sender, RoutedEventArgs e)
        {
            core.CreateTag(TagNameTextBox.Text, TagCategoryTextBox.Text);
            ReloadColumnWidths();
        }

        private void EditTagButton_Click(object sender, RoutedEventArgs e)
        {
            core.EditTag(GetFirstSelectedTag(), TagNameTextBox.Text, TagCategoryTextBox.Text);
            ReloadColumnWidths();
        }

        private void RemoveTagButton_Click(object sender, RoutedEventArgs e)
        {
            core.RemoveTag(GetFirstSelectedTag());
            ReloadColumnWidths();
        }
        #endregion

        #region Playlist buttons event handlers...
        private void BuildPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            Core.Core.FilterType filter;
            if (StandardFilterRadio.IsChecked == true)
                filter = Core.Core.FilterType.Standard;
            else if (AndFilterRadio.IsChecked == true)
                filter = Core.Core.FilterType.And;
            else
                filter = Core.Core.FilterType.Or;

            PlayListView.ItemsSource = core.CreatePlaylist(selectedTags, filter);
            ReloadColumnWidths();
        }

        private void RetagSongsButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (Song s in PlayListView.SelectedItems)
                if (!core.importList.Contains(s))
                    core.importList.Add(s);

            ReloadColumnWidths();
        }

        private void RenameSongButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPlaylistSongs.Count > 0)
            {
                var selectedSong = GetFirstSelectedPlaylistSong();
                using (var inputDialog = new StringInputDialog("Change name and/or path of the first selected song:", selectedSong.FullPath))
                    if (inputDialog.ShowDialog() == true)
                        core.MoveSong(selectedSong, inputDialog.Answer);
            }

            ReloadColumnWidths();
        }

        private void MoveSongsButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPlaylistSongs.Count > 0)
            {
                var selectedSong = GetFirstSelectedPlaylistSong();
                using (var inputDialog = new StringInputDialog("Choose new path of selected songs:", Path.GetDirectoryName(selectedSong.FullPath)))
                    if (inputDialog.ShowDialog() == true)
                    {
                        string result = inputDialog.Answer + "\\";
                        core.MoveSongs(selectedPlaylistSongs, result);
                    }
            }
        }

        private void RemoveSongsButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = new List<Song>();
            foreach (Song s in PlayListView.SelectedItems)
                selectedItems.Add(s);

            if (selectedItems.Count > 0)
            {
                using (var oid = new OptionsInputDialog("Remove songs",
                    string.Format("Are you sure? Do you want to delete {0} song files only from settings, or from drive as well?", selectedItems.Count),
                    new string[] { "Settings only", "Drive as well", "Abort" }))
                    if (oid.ShowDialog() == true)
                    {
                        if (oid.GetAnswer() != "Abort")
                        {
                            foreach (var s in selectedItems)
                                core.RemoveSong(s);
                        }
                        if (oid.GetAnswer() == "Drive as well")
                            foreach (var s in selectedItems)
                                try
                                {
                                    File.Delete(s.FullPath);
                                }
                                catch { }
                    }
            }
        }
        #endregion

        #region Tag assignment event handlers...
        private void ClearImportButton_Click(object sender, RoutedEventArgs e) => core.ClearImport();

        private void RemoveFromImportButton_Click(object sender, RoutedEventArgs e) => core.RemoveFromImport(selectedImportSongs);

        private void AssignButton_Click(object sender, RoutedEventArgs e)
        {
            core.AssignTags(selectedImportSongs, selectedTags, (bool)RemoveFromImportCheckbox.IsChecked, (bool)OverwriteTagsCheckbox.IsChecked);
            ReloadColumnWidths();
        }
        #endregion

        #region Play list view event handlers...
        private void PlayListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PlayListView.SelectedItems.Count > 0)
                core.PlaySong(PlayListView.SelectedItems[0] as Song);
        }

        private void PlayListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (Song item in e.RemovedItems)
                selectedPlaylistSongs.Remove(item);
            foreach (Song item in e.AddedItems)
                selectedPlaylistSongs.Add(item);
        }
        #endregion

        #region Import list view event handlers...
        private void ImportListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (Song item in e.RemovedItems)
                selectedImportSongs.Remove(item);
            foreach (Song item in e.AddedItems)
                selectedImportSongs.Add(item);
        }

        private void ImportListView_Drop(object sender, DragEventArgs e)
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

        private void ImportListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ImportListView.SelectedItems.Count > 0)
                core.PlayPreview(ImportListView.SelectedItems[0] as Song);
        }
        #endregion

        #region Tag list view event handlers...
        private void TagListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (Tag item in e.RemovedItems)
                selectedTags.Remove(item);
            foreach (Tag item in e.AddedItems)
                selectedTags.Add(item);

            LoadTagAdministration(GetFirstSelectedTag());
        }
        #endregion

        #region Timer event handlers...
        private void FadeTimer_Tick(object sender, EventArgs e)
        {
            if (preFadeVolume > 0)
                core.SetVolume(--preFadeVolume);
            else
            {
                core.Stop();
                SongVolumeSlider.IsEnabled = true;
                FadeButton.IsEnabled = true;
                core.SetVolume(SongVolumeSlider.Value);
                fadeTimer.Stop();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            core.CheckIsTimeForNext();

            UpdatePlayingSongInfo();
            MarkPlaying();

            PlayPauseButton.Content = core.IsReallyPlaying() ? "Pause" : "Play";
        }
        #endregion
        #endregion

        private void UpdatePlayingSongInfo()
        {
            SongProgressBar.Maximum = core.GetCurrentLength();
            SongProgressBar.Value = core.GetCurrentPosition();

            Song currentSong = core.GetCurrentSong();
            if (currentSong != null)
            {
                NameTextBlock.Text = currentSong.SongName;
                TimeTextBlock.Text = string.Format("{0} / {1}", Utilities.GetTimeString(core.GetCurrentPosition() / 10), Utilities.GetTimeString(core.GetCurrentLength() / 10));
            }
            else
            {
                NameTextBlock.Text = "";
                SongProgressBar.Value = 0;
                SongProgressBar.Maximum = 0;
                TimeTextBlock.Text = "0:00:00 / 0:00:00";
            }
        }

        private void MarkPlaying()
        {
            if (PlayListView.Items.Count > 0)
            {
                for (int i = 0; i < PlayListView.Items.Count; i++)
                {
                    if (((ListViewItem)PlayListView.ItemContainerGenerator.ContainerFromItem(core.currentPlaylist[i])) != null)
                        ((ListViewItem)PlayListView.ItemContainerGenerator.ContainerFromItem(core.currentPlaylist[i])).Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF042271"));
                }
                if (core.currentSongIndex > -1 && core.currentSongIndex < PlayListView.Items.Count)
                    if (((ListViewItem)(PlayListView.ItemContainerGenerator.ContainerFromIndex(core.currentSongIndex))) != null)
                        ((ListViewItem)(PlayListView.ItemContainerGenerator.ContainerFromIndex(core.currentSongIndex))).Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private void LoadTagAdministration(Tag tag)
        {
            if (tag != null)
            {
                TagIDTextBox.Text = GetFirstSelectedTag().ID.ToString();
                TagNameTextBox.Text = GetFirstSelectedTag().Name;
                TagCategoryTextBox.Text = GetFirstSelectedTag().Category;
            }
            else
            {
                TagIDTextBox.Text = "Auto increment";
                TagNameTextBox.Text = "";
                TagCategoryTextBox.Text = "";
            }
        }

        private void ReloadViews()
        {
            var playView = (CollectionView)CollectionViewSource.GetDefaultView(TagListView.ItemsSource);
            var groupDescription = new PropertyGroupDescription("Category");
            playView.GroupDescriptions.Clear();
            playView.GroupDescriptions.Add(groupDescription);
            LoadTagAdministration(GetFirstSelectedTag());
            ReloadColumnWidths();
        }

        private void ReloadColumnWidths()
        {
            foreach (var c in (TagListView.View as GridView).Columns)
            {
                if (double.IsNaN(c.Width))
                {
                    c.Width = c.ActualWidth;
                }
                c.Width = double.NaN;
            }
            foreach (var c in (PlayListView.View as GridView).Columns)
            {
                if (double.IsNaN(c.Width))
                {
                    c.Width = c.ActualWidth;
                }
                c.Width = double.NaN;
            }
            foreach (var c in (ImportListView.View as GridView).Columns)
            {
                if (double.IsNaN(c.Width))
                {
                    c.Width = c.ActualWidth;
                }
                c.Width = double.NaN;
            }
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
    }
}