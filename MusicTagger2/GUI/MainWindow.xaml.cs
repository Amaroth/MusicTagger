using Microsoft.Win32;
using MusicTagger2.Core;
using System;
using System.Collections.Generic;
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
        private string CurrentVersionSignature = "Music Tagger 2.5.0";
        private string CurrentFilePath = "";
        private Core.Core core = Core.Core.Instance;

        private List<SongTag> selectedTags = new List<SongTag>();
        private List<Song> selectedImportSongs = new List<Song>();
        private List<Song> selectedPlaylistSongs = new List<Song>();

        private double currentSongLength;
        private DispatcherTimer infoTimer = new DispatcherTimer();

        private bool isPreviewPlaying = false;
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

        public MainWindow()
        {
            InitializeComponent();
            LoadWindowTitle();
            infoTimer.Tick += new EventHandler(infoTimer_Tick);
            infoTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            infoTimer.Start();
            UpdateButtons();
        }

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
                try
                {
                    core.NewProject(saveFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Could not create a Project in {0}. Error message: {1}", saveFileDialog.FileName, ex.Message));
                }
                CurrentFilePath = saveFileDialog.FileName;
            }
            LoadWindowTitle();
            UpdateButtons();
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
            try
            {
                core.LoadProject(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Failed to load Project from {0}. Error message: {1}", filePath, ex.Message));
            }
            ReloadViews();
            CurrentFilePath = filePath;
            LoadWindowTitle();
            UpdateButtons();
        }

        /// <summary>
        /// Save current Project file. If none is opened, go to Save As.
        /// </summary>
        private void SaveFile()
        {
            if (!string.IsNullOrEmpty(CurrentFilePath))
                try
                {
                    core.SaveProject(CurrentFilePath);
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
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Project could not be saved to {0}. Error message: {1}", saveFileDialog.FileName, ex.Message));
                }
                CurrentFilePath = saveFileDialog.FileName;
            }
            LoadWindowTitle();
        }
        #endregion

        #region Update UI elements functions...
        private void LoadWindowTitle()
        {
            Title = CurrentVersionSignature;
            if ((CurrentFilePath != null) && (CurrentFilePath != ""))
                Title += " - " + Path.GetFileName(CurrentFilePath);
        }

        private void ReloadViews()
        {
            ImportListView.ItemsSource = core.ImportList;
            TagListView.ItemsSource = core.SongTags;
            var playView = (CollectionView)CollectionViewSource.GetDefaultView(TagListView.ItemsSource);
            var groupDescription = new PropertyGroupDescription("Category");
            playView.GroupDescriptions.Clear();
            playView.GroupDescriptions.Add(groupDescription);
            LoadTagAdministrationFields(GetFirstSelectedTag());
            ReloadTagListViewColWidths();
            ReloadPlayListViewColWidths();
            ReloadImportListViewColWidths();
        }

        private void UpdateButtons()
        {
            PlayPauseButton.Content = IsSongPlayerPlaying ? "Pause" : "Play";
            FirstButton.IsEnabled = (SongPlayer.Source != null) && !isPreviewPlaying;
            PreviousButton.IsEnabled = (SongPlayer.Source != null) && !isPreviewPlaying;
            NextButton.IsEnabled = (SongPlayer.Source != null) && !isPreviewPlaying;
            LastButton.IsEnabled = (SongPlayer.Source != null) && !isPreviewPlaying;
            StopButton.IsEnabled = IsSongPlayerPlaying;
        }

        private void UpdatePlayingSongInfo()
        {
            if ((SongPlayer.Source != null) && SongPlayer.NaturalDuration.HasTimeSpan)
            {
                NameTextBlock.Text = Path.GetFileName(SongPlayer.Source.ToString());
                SongProgressBar.Maximum = currentSongLength;
                SongProgressBar.Value = SongPlayer.Position.TotalMilliseconds;
                TimeTextBlock.Text = string.Format("{0} / {1}",
                    Utilities.GetTimeString((int)SongPlayer.Position.TotalSeconds), Utilities.GetTimeString((int)SongPlayer.NaturalDuration.TimeSpan.TotalSeconds));
            }
            else
            {
                NameTextBlock.Text = "";
                SongProgressBar.Value = 0;
                SongProgressBar.Maximum = 1;
                TimeTextBlock.Text = "0:00:00 / 0:00:00";
            }
        }

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

        private void LoadTagAdministrationFields(SongTag tag)
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

        private void ReloadImportListViewColWidths()
        {
            foreach (var c in (ImportListView.View as GridView).Columns)
            {
                if (double.IsNaN(c.Width))
                    c.Width = c.ActualWidth;
                c.Width = double.NaN;
            }
        }

        private void ReloadPlayListViewColWidths()
        {
            foreach (var c in (PlayListView.View as GridView).Columns)
            {
                if (double.IsNaN(c.Width))
                    c.Width = c.ActualWidth;
                c.Width = double.NaN;
            }
        }

        private void ReloadTagListViewColWidths()
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
        private SongTag GetFirstSelectedTag()
        {
            if (selectedTags.Count < 1)
                return null;
            foreach (var t in core.SongTags)
                if (selectedTags.Contains(t))
                    return t;
            return null;
        }

        private Song GetFirstSelectedPlaylistSong()
        {
            if (selectedPlaylistSongs.Count < 1)
                return null;
            foreach (var s in core.CurrentPlayList)
                if (selectedPlaylistSongs.Contains(s))
                    return s;
            return null;
        }
        #endregion

        #region Play controls functions...
        private void PlayPreview(Song song)
        {
            if (song != null)
            {
                Stop();
                SongPlayer.Source = new Uri(song.FullPath);
                SongPlayer.Play();
                isPreviewPlaying = true;
            }
        }

        private void Play()
        {
            if (SongPlayer.Source == null)
                SongPlayer.Source = core.First();
            if (SongPlayer.Source != null)
            {
                SongPlayer.Play();
                IsSongPlayerPlaying = true;
            }
        }

        private void Pause()
        {
            SongPlayer.Pause();
            IsSongPlayerPlaying = false;
        }

        private void Stop()
        {
            SongPlayer.Stop();
            SongPlayer.Source = null;
            IsSongPlayerPlaying = false;
            isPreviewPlaying = false;
        }

        private void JumpTo(Song song)
        {
            Uri currentUri = core.SetCurrent(song);
            if (currentUri != null)
            {
                SongPlayer.Source = currentUri;
                Play();
            }
        }

        private void Next()
        {
            SongPlayer.Source = core.Next();
            if (SongPlayer.Source == null)
                Stop();
        }

        private void Previous()
        {
            if (IsSongPlayerPlaying)
                if (SongPlayer.Position.TotalSeconds < 1)
                    SongPlayer.Source = core.Previous();
                else
                    SongPlayer.Position = new TimeSpan(0);
        }

        private void Last()
        {
            SongPlayer.Source = core.Last();
        }

        private void First()
        {
            SongPlayer.Source = core.First();
        }
        #endregion

        #region Menu event handlers...
        private void NewMenuItem_Click(object sender, RoutedEventArgs e) => NewFile();

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e) => OpenFile();

        private void SaveMenuItem_Click(object sender, RoutedEventArgs e) => SaveFile();

        private void SaveAsMenuItem_Click(object sender, RoutedEventArgs e) => SaveAsFile();
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
        private void CreateTagButton_Click(object sender, RoutedEventArgs e)
        {
            core.CreateTag(TagNameTextBox.Text, TagCategoryTextBox.Text);
            ReloadTagListViewColWidths();
        }

        private void EditTagButton_Click(object sender, RoutedEventArgs e)
        {
            core.UpdateTag(GetFirstSelectedTag(), TagNameTextBox.Text, TagCategoryTextBox.Text);
            ReloadTagListViewColWidths();
        }

        private void RemoveTagButton_Click(object sender, RoutedEventArgs e)
        {
            core.RemoveTag(GetFirstSelectedTag());
            ReloadTagListViewColWidths();
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

                core.GenerateFilteredPlayList(selectedTags, filter);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            PlayListView.ItemsSource = null;
            PlayListView.ItemsSource = core.CurrentPlayList;
            ReloadPlayListViewColWidths();
        }

        /// <summary>
        /// Adds all selected songs into import list for retagging.
        /// </summary>
        private void RetagSongsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                core.AddIntoImport(selectedPlaylistSongs);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            ReloadImportListViewColWidths();
        }

        /// <summary>
        /// Changes path to the first selected song in playlist, moving its file in the process.
        /// </summary>
        private void RenameSongButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPlaylistSongs.Count > 0)
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
            ReloadImportListViewColWidths();
            ReloadPlayListViewColWidths();
        }

        /// <summary>
        /// Moves all selected song files to provided directory, keeping file names.
        /// </summary>
        private void MoveSongsButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPlaylistSongs.Count > 0)
            {
                using (var inputDialog = new StringInputDialog("Choose new path of selected songs:", Path.GetDirectoryName(GetFirstSelectedPlaylistSong().FullPath)))
                    if (inputDialog.ShowDialog() == true)
                    {
                        try
                        {
                            core.MoveSongsToDir(selectedPlaylistSongs, inputDialog.Answer + "\\");
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
                core.RemoveFromImport(selectedImportSongs);
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
                core.AssignTags(selectedImportSongs, selectedTags, (bool)RemoveFromImportCheckbox.IsChecked, (bool)OverwriteTagsCheckbox.IsChecked);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Failed to add tags to songs. Error message: {0}", ex.Message));
            }
            ReloadImportListViewColWidths();
            ImportListView.ItemsSource = null;
            ImportListView.ItemsSource = core.ImportList;
        }
        #endregion

        #region Play list view event handlers...
        private void PlayListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PlayListView.SelectedItems.Count > 0)
                JumpTo(GetFirstSelectedPlaylistSong());
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
            ReloadImportListViewColWidths();
        }

        private void ImportListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ImportListView.SelectedItems.Count > 0)
                PlayPreview(ImportListView.SelectedItems[0] as Song);
        }
        #endregion

        #region Tag list view event handlers...
        private void TagListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (SongTag item in e.RemovedItems)
                selectedTags.Remove(item);
            foreach (SongTag item in e.AddedItems)
                selectedTags.Add(item);
            
            LoadTagAdministrationFields(GetFirstSelectedTag());
        }
        #endregion

        #region Timer event handlers...
        private void infoTimer_Tick(object sender, EventArgs e)
        {
            UpdatePlayingSongInfo();
            MarkPlaying();
        }
        #endregion

        private void SongPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            IsSongPlayerPlaying = true;
            currentSongLength = IsSongPlayerPlaying ? SongPlayer.NaturalDuration.TimeSpan.TotalMilliseconds : 0;
        }

        private void SongPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            IsSongPlayerPlaying = false;
            SongPlayer.Source = null;
            if (!isPreviewPlaying)
                Next();
        }

        private void SongPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            IsSongPlayerPlaying = false;
            isPreviewPlaying = false;
        }
    }
}