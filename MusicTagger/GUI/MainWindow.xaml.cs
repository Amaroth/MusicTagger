using Microsoft.Win32;
using MusicTagger.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MusicTagger.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string CurrentVersionSignature = "Music Tagger 2.10.0";
        private string CurrentProjectFilePath = "";

        private Core.Core core = Core.Core.Instance;
        private StartupConfig startupConfig = new StartupConfig();

        private double currentSongLength;
        private DispatcherTimer infoTimer = new DispatcherTimer();

        private Song PreviewSong = null;
        private bool _isSongPlayerPlaying = false;

        private bool IsSongPlayerPlaying
        {
            get => _isSongPlayerPlaying;
            set
            {
                _isSongPlayerPlaying = value;
                UpdateButtons();
            }
        }

        private Uri CurrentSongUri
        {
            get => SongPlayer.Source;
            set
            {
                SongPlayer.Source = value;
                MarkPlaying();
            }
        }

        #region Window open/close...
        public MainWindow()
        {
            InitializeComponent();
            UpdateElementIsEnabled(false);
        }

        /// <summary>
        /// Load startup config file and set user's settings according to it, reload views and controls accordingly.
        /// </summary>
        private void ReloadUI()
        {
            LoadWindowTitle();
            infoTimer.Tick += new EventHandler(InfoTimer_Tick);
            infoTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            infoTimer.Start();
            ReloadViews();
            UpdateButtons();
            UpdateRecentMenu();
        }

        /// <summary>
        /// Loads window settings saved into startup config file and applies them.
        /// </summary>
        private void LoadStartup()
        {
            startupConfig.LoadFile();

            RandomCheckBox.IsChecked = startupConfig.PlayRandom;
            RepeatCheckBox.IsChecked = startupConfig.PlayRepeat;

            StandardFilterRadio.IsChecked = (startupConfig.SelectedFilter == Core.Core.FilterType.Standard);
            AndFilterRadio.IsChecked = (startupConfig.SelectedFilter == Core.Core.FilterType.And);
            OrFilterRadio.IsChecked = (startupConfig.SelectedFilter == Core.Core.FilterType.Or);

            SongVolumeSlider.Value = startupConfig.SongVolume;
            SoundsVolumeSlider.Value = startupConfig.SoundsVolume;

            SongPlayer.IsMuted = startupConfig.SongMute;
            SongVolumeSlider.IsEnabled = !SongPlayer.IsMuted;
            SongMuteUnmuteButton.Content = SongPlayer.IsMuted ? "Unmute" : "Mute";

            Width = startupConfig.WindowWidth;
            Height = startupConfig.WindoHeight;
            WindowState = startupConfig.WindowState;
        }

        /// <summary>
        /// Makes sure Open Recent menu item is enable only if it has at least 1 file in it, and fills it with 5 most recent files.
        /// </summary>
        private void UpdateRecentMenu()
        {
            if ((string)(FileMenuItem.Items[FileMenuItem.Items.Count - 1] as MenuItem).Header == "Open _Recent")
                FileMenuItem.Items.RemoveAt(FileMenuItem.Items.Count - 1);

            var recentMenuItem = new MenuItem()
            {
                Header = "Open _Recent",
                IsEnabled = (startupConfig.RecentProjects.Count > 0)
            };
            FileMenuItem.Items.Add(recentMenuItem);
            if (startupConfig.RecentProjects.Count > 0)
            {
                for (var i = startupConfig.RecentProjects.Count - 1; (i > -1) && (i > startupConfig.RecentProjects.Count - 6); i--)
                {
                    var newRecent = new MenuItem() { Header = startupConfig.RecentProjects[i] };
                    newRecent.Click += OpenRecent_Click;
                    recentMenuItem.Items.Add(newRecent);
                }
            }
        }

        /// <summary>
        /// If window is about to be closed, save current user's settings into startup file.
        /// </summary>
        private void SaveStartup()
        {
            Core.Core.FilterType filterType;
            if (StandardFilterRadio.IsChecked == true)
                filterType = Core.Core.FilterType.Standard;
            else if (AndFilterRadio.IsChecked == true)
                filterType = Core.Core.FilterType.And;
            else
                filterType = Core.Core.FilterType.Or;
            startupConfig.SaveFile(RandomCheckBox.IsChecked == true, RepeatCheckBox.IsChecked == true, filterType, SongVolumeSlider.Value, SoundsVolumeSlider.Value,
                SongPlayer.IsMuted, SongPlayer.IsMuted, Width, Height, WindowState);

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadStartup();
            ReloadUI();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) => SaveStartup();
        #endregion

        #region Menu functions...
        /// <summary>
        /// Create a new settings file.
        /// </summary>
        public void NewFile()
        {
            Stop();
            var saveFileDialog = new SaveFileDialog { Filter = "Project file (*.mtg)|*.mtg" };
            if (saveFileDialog.ShowDialog() == true)
            {
                UpdateElementIsEnabled(false);
                try
                {
                    core.NewProject(saveFileDialog.FileName);
                    startupConfig.AddRecentProject(saveFileDialog.FileName);
                    UpdateElementIsEnabled(true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Could not create a Project in {0}. Error message: {1}", saveFileDialog.FileName, ex.Message));
                }
                CurrentProjectFilePath = saveFileDialog.FileName;
            }
            LoadWindowTitle();
            ReloadUI();
        }

        /// <summary>
        /// Open a Project file after retrieving path to it from user.
        /// </summary>
        private void OpenFile()
        {
            var openFileDialog = new OpenFileDialog() { Filter = "Project file (*.mtg)|*.mtg" };
            if (openFileDialog.ShowDialog() == true)
                OpenFile(openFileDialog.FileName);
        }

        /// <summary>
        /// Opens given Project file.
        /// </summary>
        /// <param name="filePath"></param>
        public void OpenFile(string filePath)
        {
            Stop();
            UpdateElementIsEnabled(false);
            try
            {
                core.LoadProject(filePath);
                startupConfig.AddRecentProject(filePath);
                UpdateElementIsEnabled(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Failed to load Project from {0}. Error message: {1}", filePath, ex.Message));
            }
            CurrentProjectFilePath = filePath;
            ReloadUI();
        }

        /// <summary>
        /// Save current Project file. If none is opened, go to Save As.
        /// </summary>
        private void SaveFile()
        {
            if (!string.IsNullOrEmpty(CurrentProjectFilePath))
                try
                {
                    core.SaveProject(CurrentProjectFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Current Project could not be saved. Error message: {0}", ex.Message));
                }
            else
                SaveAsFile();
        }

        /// <summary>
        /// Save current Project into new file.
        /// </summary>
        private void SaveAsFile()
        {
            var saveFileDialog = new SaveFileDialog { Filter = "Project file (*.mtg)|*.mtg" };
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    core.SaveProject(saveFileDialog.FileName);
                    startupConfig.AddRecentProject(saveFileDialog.FileName);
                    UpdateRecentMenu();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Project could not be saved to {0}. Error message: {1}", saveFileDialog.FileName, ex.Message));
                }
                CurrentProjectFilePath = saveFileDialog.FileName;
            }
            LoadWindowTitle();
        }

        /// <summary>
        /// Opens a window for adding a YT video to the queue.
        /// </summary>
        private void YouTubeDownloadWindow()
        {
            using (var inputDialog = new YouTubeDownloadDialog())
                if (inputDialog.ShowDialog() == true)
                {
                    try
                    {
                        var answers = inputDialog.GetAnswers();

                        /*core.DownloadYouTubeSong(answers.Item1, answers.Item2);
                        if (File.Exists(answers.Item2))
                            MessageBox.Show(string.Format("File {0} was successfully downloaded and converted!", answers.Item2));*/
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
        }
        #endregion

        #region Update UI elements functions...
        /// <summary>
        /// 
        /// </summary>
        /// <param name="isProjectLoaded"></param>
        private void UpdateElementIsEnabled(bool isProjectLoaded)
        {
            SaveMenuItem.IsEnabled = isProjectLoaded;
            SaveAsMenuItem.IsEnabled = isProjectLoaded;
            TagListView.IsEnabled = isProjectLoaded;
            ListsTabView.IsEnabled = isProjectLoaded;
            StandardFilterRadio.IsEnabled = isProjectLoaded;
            AndFilterRadio.IsEnabled = isProjectLoaded;
            OrFilterRadio.IsEnabled = isProjectLoaded;
            PlayPanelGrid.IsEnabled = isProjectLoaded;
        }

        /// <summary>
        /// Updates title of main window with app name, version and current file name.
        /// </summary>
        private void LoadWindowTitle()
        {
            Title = CurrentVersionSignature;
            if ((CurrentProjectFilePath != null) && (CurrentProjectFilePath != ""))
                Title += " - " + Path.GetFileName(CurrentProjectFilePath);
        }

        /// <summary>
        /// Completely reloads all views.
        /// </summary>
        private void ReloadViews()
        {
            ImportListView.ItemsSource = core.ImportList;
            TagListView.ItemsSource = core.SongTags;
            PlayListView.ItemsSource = core.CurrentPlayList;
            DownloadListView.ItemsSource = core.DownloadList;
            var playView = (CollectionView)CollectionViewSource.GetDefaultView(TagListView.ItemsSource);
            var groupDescription = new PropertyGroupDescription("Category");
            playView.GroupDescriptions.Clear();
            playView.GroupDescriptions.Add(groupDescription);
            LoadTagAdministrationFields(GetFirstSelectedTag());
            UpdateTagListViewColWidths();
            UpdatePlayListViewColWidths();
            UpdateImportListViewColWidths();
            UpdatePlayingSongInfo();
        }

        /// <summary>
        /// Updates whether buttons in play panel should be enabled or not and text on Play / Pause button.
        /// </summary>
        private void UpdateButtons()
        {
            PlayPauseButton.Content = IsSongPlayerPlaying ? "Pause" : "Play";
            FirstButton.IsEnabled = (CurrentSongUri != null) && (PreviewSong == null);
            PreviousButton.IsEnabled = (CurrentSongUri != null) && (PreviewSong == null);
            NextButton.IsEnabled = (CurrentSongUri != null) && (PreviewSong == null);
            LastButton.IsEnabled = (CurrentSongUri != null) && (PreviewSong == null);
            StopButton.IsEnabled = IsSongPlayerPlaying;
        }

        /// <summary>
        /// If song from playlist is playing or being paused, mark it with red in playlist.
        /// </summary>
        private void MarkPlaying()
        {
            if (PlayListView.Items.Count > 0)
            {
                for (var i = 0; i < PlayListView.Items.Count; i++)
                {
                    if (((ListViewItem)PlayListView.ItemContainerGenerator.ContainerFromItem(core.CurrentPlayList[i])) != null)
                        ((ListViewItem)PlayListView.ItemContainerGenerator.ContainerFromItem(core.CurrentPlayList[i])).Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF042271"));
                }
                if (core.GetCurrentSongIndex() > -1 && core.GetCurrentSongIndex() < PlayListView.Items.Count)
                    if (((ListViewItem)(PlayListView.ItemContainerGenerator.ContainerFromIndex(core.GetCurrentSongIndex()))) != null)
                        ((ListViewItem)(PlayListView.ItemContainerGenerator.ContainerFromIndex(core.GetCurrentSongIndex()))).Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        /// <summary>
        /// Fires 10 times per second and ensures progress bar and play panel texts are being updated.
        /// </summary>
        private void InfoTimer_Tick(object sender, EventArgs e) => UpdatePlayingSongInfo();

        /// <summary>
        /// Updates progress bar and texts in play panel depending on song being played.
        /// </summary>
        private void UpdatePlayingSongInfo()
        {
            if ((CurrentSongUri != null) && SongPlayer.NaturalDuration.HasTimeSpan)
            {
                NameTextBlock.Text = (PreviewSong != null) ? "Previewing: " : "";
                NameTextBlock.Text += Path.GetFileName(CurrentSongUri.ToString());
                TagsTextBlock.Text = (PreviewSong != null) ? PreviewSong.TagNames : core.GetCurrentSongTagNames();
                SongProgressBar.Maximum = currentSongLength;
                SongProgressBar.Value = SongPlayer.Position.TotalMilliseconds;
                TimeTextBlock.Text = string.Format("{0} / {1}",
                    Utilities.GetTimeString((int)SongPlayer.Position.TotalSeconds), Utilities.GetTimeString((int)SongPlayer.NaturalDuration.TimeSpan.TotalSeconds));
            }
            else
            {
                NameTextBlock.Text = "";
                TagsTextBlock.Text = "";
                SongProgressBar.Value = 0;
                SongProgressBar.Maximum = 1;
                TimeTextBlock.Text = "0:00:00 / 0:00:00";
            }
        }

        private void TagListView_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadTagAdministrationFields(GetFirstSelectedTag());

        /// <summary>
        /// Updates fields in tag administration form depending on tag currently selected.
        /// </summary>
        /// <param name="tag">Currently selected tag.</param>
        private void LoadTagAdministrationFields(SongTag tag)
        {
            UpdateTagButton.IsEnabled = (tag != null);
            RemoveTagButton.IsEnabled = (tag != null);
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

        /// <summary>
        /// Updates widths of columns in import list to fit width of their contents.
        /// </summary>
        private void UpdateImportListViewColWidths()
        {
            foreach (var c in (ImportListView.View as GridView).Columns)
            {
                if (double.IsNaN(c.Width))
                    c.Width = c.ActualWidth;
                c.Width = double.NaN;
            }
        }

        /// <summary>
        /// Updates widths of columns in playlist to fit width of their contents.
        /// </summary>
        private void UpdatePlayListViewColWidths()
        {
            foreach (var c in (PlayListView.View as GridView).Columns)
            {
                if (double.IsNaN(c.Width))
                    c.Width = c.ActualWidth;
                c.Width = double.NaN;
            }
        }

        /// <summary>
        /// Updates widths of columns in tag list to fit width of their contents.
        /// </summary>
        private void UpdateTagListViewColWidths()
        {
            foreach (var c in (TagListView.View as GridView).Columns)
            {
                if (double.IsNaN(c.Width))
                    c.Width = c.ActualWidth;
                c.Width = double.NaN;
            }
        }
        #endregion

        #region Get first selected functions...
        /// <summary>
        /// Gets the first tag from all tags which is currently selected.
        /// </summary>
        private SongTag GetFirstSelectedTag()
        {
            if (TagListView.SelectedItems.Count < 1)
                return null;
            foreach (var t in TagListView.Items)
                if (TagListView.SelectedItems.Contains(t))
                    return t as SongTag;
            return null;
        }

        /// <summary>
        /// Gets the first song in playlist from all songs which is currently selected.
        /// </summary>
        private Song GetFirstSelectedPlaylistSong()
        {
            if (PlayListView.SelectedItems.Count < 1)
                return null;
            foreach (var s in PlayListView.Items)
                if (PlayListView.SelectedItems.Contains(s))
                    return s as Song;
            return null;
        }
        #endregion

        #region Play controls functions...
        private void PlayPreview(Song song)
        {
            try
            {
                if (song != null)
                {
                    Stop();
                    CurrentSongUri = new Uri(song.FullPath);
                    SongPlayer.Play();
                    PreviewSong = song;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Play()
        {
            try
            {
                if (CurrentSongUri == null)
                    CurrentSongUri = core.First();
                if (CurrentSongUri != null)
                {
                    SongPlayer.Play();
                    IsSongPlayerPlaying = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Pause()
        {
            try
            {
                SongPlayer.Pause();
                IsSongPlayerPlaying = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Stop()
        {
            try
            {
                SongPlayer.Stop();
                core.SetCurrent(null);
                CurrentSongUri = null;
                IsSongPlayerPlaying = false;
                PreviewSong = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void JumpTo(Song song)
        {
            try
            {
                Uri currentUri = core.SetCurrent(song);
                if (currentUri != null)
                {
                    CurrentSongUri = currentUri;
                    Play();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Next()
        {
            try
            {
                CurrentSongUri = core.Next();
                if (CurrentSongUri == null)
                    Stop();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Previous()
        {
            try
            {
                if (IsSongPlayerPlaying)
                    if (SongPlayer.Position.TotalSeconds < 1)
                        CurrentSongUri = core.Previous();
                    else
                        SongPlayer.Position = new TimeSpan(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Last()
        {
            try
            {
                CurrentSongUri = core.Last();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void First()
        {
            try
            {
                CurrentSongUri = core.First();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region Menu event handlers...
        private void NewMenuItem_Click(object sender, RoutedEventArgs e) => NewFile();

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e) => OpenFile();

        private void SaveMenuItem_Click(object sender, RoutedEventArgs e) => SaveFile();

        private void SaveAsMenuItem_Click(object sender, RoutedEventArgs e) => SaveAsFile();

        private void OpenRecent_Click(object sender, RoutedEventArgs e) => OpenFile((sender as MenuItem).Header.ToString());
        #endregion

        #region Play panel event handlers...
        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsSongPlayerPlaying)
                Pause();
            else
                Play();
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e) => Previous();

        private void NextButton_Click(object sender, RoutedEventArgs e) => Next();

        private void FirstButton_Click(object sender, RoutedEventArgs e) => First();

        private void LastButton_Click(object sender, RoutedEventArgs e) => Last();

        private void StopButton_Click(object sender, RoutedEventArgs e) => Stop();

        private void SongMuteUnmuteButton_Click(object sender, RoutedEventArgs e)
        {
            SongPlayer.IsMuted = !SongPlayer.IsMuted;
            SongVolumeSlider.IsEnabled = !SongPlayer.IsMuted;
            SongMuteUnmuteButton.Content = SongPlayer.IsMuted ? "Unmute" : "Mute";
        }

        private void SongVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SongPlayer.Volume = SongVolumeSlider.Value / 100;
        }

        private void VolumeSlider_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            (sender as Slider).Value += (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) ? e.Delta / 24 : e.Delta / 120;
        }

        private void SongProgressBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            double MousePosition = e.GetPosition(SongProgressBar).X;
            SongProgressBar.Value = e.GetPosition(SongProgressBar).X / SongProgressBar.ActualWidth * SongProgressBar.Maximum;
            if (SongProgressBar.Value == SongProgressBar.Maximum)
                SongProgressBar.Value--;
            SongPlayer.Position = new TimeSpan(0, 0, 0, 0, (int)SongProgressBar.Value);
        }

        private void RandomCheckBox_Checked(object sender, RoutedEventArgs e) => core.Random = (bool)RandomCheckBox.IsChecked;

        private void RepeatCheckBox_Checked(object sender, RoutedEventArgs e) => core.Repeat = (bool)RepeatCheckBox.IsChecked;
        #endregion

        #region Tag management event handlers...
        /// <summary>
        /// Creates a new tag with provided name and category.
        /// </summary>
        private void CreateTagButton_Click(object sender, RoutedEventArgs e)
        {
            core.CreateTag(TagNameTextBox.Text, TagCategoryTextBox.Text);
            UpdateTagListViewColWidths();
        }

        /// <summary>
        /// Updates selected tag with new name and category.
        /// </summary>
        private void UpdateTagButton_Click(object sender, RoutedEventArgs e)
        {
            core.UpdateTag(GetFirstSelectedTag(), TagNameTextBox.Text, TagCategoryTextBox.Text);
            UpdateTagListViewColWidths();
        }

        /// <summary>
        /// Removes selected tag from Project.
        /// </summary>
        private void RemoveTagButton_Click(object sender, RoutedEventArgs e)
        {
            core.RemoveTag(GetFirstSelectedTag());
            UpdateTagListViewColWidths();
        }
        #endregion

        #region Playlist buttons event handlers...
        /// <summary>
        /// Gets a new playlist based on filter criteria.
        /// </summary>
        private void BuildPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Core.Core.FilterType filter;
                if (StandardFilterRadio.IsChecked == true)
                    filter = Core.Core.FilterType.Standard;
                else if (AndFilterRadio.IsChecked == true)
                    filter = Core.Core.FilterType.And;
                else
                    filter = Core.Core.FilterType.Or;

                var selected = new List<SongTag>();
                foreach (var t in TagListView.SelectedItems)
                    selected.Add(t as SongTag);
                core.GenerateFilteredPlayList(selected, filter);
                if (PreviewSong == null)
                    Stop();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            PlayListView.ItemsSource = null;
            PlayListView.ItemsSource = core.CurrentPlayList;
            UpdatePlayListViewColWidths();
        }

        /// <summary>
        /// Adds all selected songs into import list for retagging.
        /// </summary>
        private void RetagSongsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selected = new List<string>();
                foreach (Song s in PlayListView.SelectedItems)
                    selected.Add(s.FullPath);
                core.AddIntoImport(selected);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            UpdateImportListViewColWidths();
        }

        /// <summary>
        /// Changes path to the first selected song in playlist, moving its file in the process.
        /// </summary>
        private void RenameSongButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlayListView.SelectedItems.Count > 0)
            {
                Song selectedSong = GetFirstSelectedPlaylistSong();
                using (var inputDialog = new StringInputDialog("Change name and/or path of the first selected song:", selectedSong.FullPath))
                    if (inputDialog.ShowDialog() == true)
                        try
                        {
                            core.MoveSongFile(selectedSong, inputDialog.Answer);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
            }
            UpdateImportListViewColWidths();
            UpdatePlayListViewColWidths();
        }

        /// <summary>
        /// Moves all selected song files to provided directory, keeping file names.
        /// </summary>
        private void MoveSongsButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlayListView.SelectedItems.Count > 0)
            {
                using (var inputDialog = new StringInputDialog("Choose new path of selected songs:", Path.GetDirectoryName(GetFirstSelectedPlaylistSong().FullPath)))
                    if (inputDialog.ShowDialog() == true)
                    {
                        try
                        {
                            var selected = new List<Song>();
                            foreach (Song s in PlayListView.SelectedItems)
                                selected.Add(s);
                            core.MoveSongsToDir(selected, inputDialog.Answer + "\\");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
            }
        }

        /// <summary>
        /// Removes all selected songs from settings, optinally also from drive.
        /// </summary>
        private void RemoveSongsButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlayListView.SelectedItems.Count > 0)
            {
                var selectedItems = new List<Song>();
                foreach (Song s in PlayListView.SelectedItems)
                    selectedItems.Add(s);

                using (var oid = new OptionsInputDialog("Remove songs",
                    string.Format("Are you sure? Do you want to delete {0} song files only from settings, or from drive as well?", selectedItems.Count),
                    new string[] { "Settings only", "Drive as well", "Abort" }))
                    if (oid.ShowDialog() == true)
                        if (oid.GetAnswer() != "Abort")
                            try
                            {
                                foreach (var s in selectedItems)
                                    core.RemoveSong(s, (oid.GetAnswer() == "Drive as well"));
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
            }
        }
        #endregion

        #region Tag assignment event handlers...
        /// <summary>
        /// Clears import from all songs which already have at least one tag added.
        /// </summary>
        private void ClearImportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                core.ClearImport();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Failed to clean import from already tagged songs. Error message: {0}", ex.Message));
            }

        }

        /// <summary>
        /// Removes all currently selected songs in import list from it.
        /// </summary>
        private void RemoveFromImportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selected = new List<Song>();
                foreach (Song s in ImportListView.SelectedItems)
                    selected.Add(s);
                core.RemoveFromImport(selected);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Failed to remove selected songs from import. Error message: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Assigns selected tags to songs selected in import list. Optionally removes newly tagged songs from import and overwrites old tags.
        /// </summary>
        private void AssignButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTags = new List<SongTag>();
                var selectedSongs = new List<Song>();
                foreach (SongTag t in TagListView.SelectedItems)
                    selectedTags.Add(t);
                foreach (Song s in ImportListView.SelectedItems)
                    selectedSongs.Add(s);
                core.AssignTags(selectedSongs, selectedTags, (bool)RemoveFromImportCheckbox.IsChecked, (bool)OverwriteTagsCheckbox.IsChecked);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Failed to add tags to songs. Error message: {0}", ex.Message));
            }
            UpdateImportListViewColWidths();
            ImportListView.ItemsSource = null;
            ImportListView.ItemsSource = core.ImportList;

            UpdatePlayListViewColWidths();
            PlayListView.ItemsSource = null;
            PlayListView.ItemsSource = core.CurrentPlayList;
        }
        #endregion

        #region Play list view event handlers...
        /// <summary>
        /// Plays selected song in playlist after double clicking it.
        /// </summary>
        private void PlayListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PlayListView.SelectedItems.Count > 0)
                JumpTo(GetFirstSelectedPlaylistSong());
        }
        #endregion

        #region Import list view event handlers...
        private void ImportListView_Drop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var importList = new List<string>();

            foreach (string s in files)
            {
                if (Path.GetDirectoryName(s).StartsWith(Path.GetDirectoryName(CurrentProjectFilePath)))
                {
                    if (Directory.Exists(s))
                        foreach (var f in Directory.GetFiles(s, "*.*", SearchOption.AllDirectories))
                            importList.Add(f);
                    else if (File.Exists(s))
                        importList.Add(s);
                }
            }

            core.AddIntoImport(importList);
            UpdateImportListViewColWidths();
        }

        private void ImportListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ImportListView.SelectedItems.Count > 0)
                PlayPreview(ImportListView.SelectedItems[0] as Song);
        }
        #endregion

        #region Song player event handlers...
        private void SongPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            IsSongPlayerPlaying = true;
            currentSongLength = IsSongPlayerPlaying ? SongPlayer.NaturalDuration.TimeSpan.TotalMilliseconds : 0;
        }

        private void SongPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            IsSongPlayerPlaying = false;
            CurrentSongUri = null;
            if ((PreviewSong == null))
                Next();
        }

        private void SongPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            IsSongPlayerPlaying = false;
            PreviewSong = null;
        }


        #endregion

        #region YT > MP3 event handlers...
        private void DownloadListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            /*if (DownloadListView.SelectedItems.Count > 0)
                PlayPreview(DownloadListView.SelectedItems[0] as DownloadItem);*/
        }

        private void AddURLButton_Click(object sender, RoutedEventArgs e)
        {
            core.AddIntoDownload(URLTextBox.Text, TargetPathTextBox.Text);
        }

        private void UpdateDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (DownloadListView.SelectedItems.Count > 0)
                core.UpdateDownloadItem(DownloadListView.SelectedItems[0] as DownloadItem, URLTextBox.Text, TargetPathTextBox.Text);
        }

        private void RemoveURLButton_Click(object sender, RoutedEventArgs e)
        {
            List<DownloadItem> selected = new List<DownloadItem>();
            foreach (DownloadItem i in DownloadListView.SelectedItems)
                selected.Add(i);
            core.RemoveFromDownload(selected);
        }

        private void DownloadURLButton_Click(object sender, RoutedEventArgs e)
        {
            List<DownloadItem> selected = new List<DownloadItem>();
            foreach (DownloadItem i in DownloadListView.SelectedItems)
                selected.Add(i);
            core.DownloadSelected(selected);
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e) => core.DownloadAll();

        private void DownloadPathButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog() { Filter = "MP3 file (*.mp3)|*.mp3" };
            if (saveFileDialog.ShowDialog() == true)
                TargetPathTextBox.Text = saveFileDialog.FileName;
        }

        private void DownloadListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DownloadListView.SelectedItems.Count > 0)
            {
                URLTextBox.Text = (DownloadListView.SelectedItems[0] as DownloadItem).URL;
                TargetPathTextBox.Text = (DownloadListView.SelectedItems[0] as DownloadItem).FilePath;
            }
        }
        #endregion
    }
}