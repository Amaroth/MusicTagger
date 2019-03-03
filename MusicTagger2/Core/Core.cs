using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

// FadeStop
// Sortování dle sloupců v listviewech
// Delete z import listu a HD
// Ukládání nastaveného volume
// Full file saving/loading options


namespace MusicTagger2.Core
{
    /// <summary>
    /// Core handles all main functionalities and logical interaction between data, being controller behind GUI.
    /// </summary>
    class Core
    {
        private static Core instance;
        private Config conf = Config.Instance;

        public ObservableCollection<Tag> tags = new ObservableCollection<Tag>();
        public ObservableCollection<Song> importList = new ObservableCollection<Song>();
        public ObservableCollection<Song> currentPlaylist = new ObservableCollection<Song>();

        public List<int> randomIndexList = new List<int>();
        private Song currentSong;
        private Song previewSong;
        public int currentSongIndex = -1;
        private int currentSongRandomIndex = -1;

        public Dictionary<string, Song> allSongs = new Dictionary<string, Song>();
        public bool Random;
        public bool Repeat;
        public string SettingsFilePath;
        private bool supposedToBePlaying;

        private MediaPlayer.MediaPlayer mp = new MediaPlayer.MediaPlayer();
        public bool IsReallyPlaying => mp.PlayState == MediaPlayer.MPPlayStateConstants.mpPlaying;
        private bool IsCurrentFirst => Random ? (currentSongRandomIndex == 0) : (currentSongIndex == 0);
        private bool IsCurrentLast => Random ? currentSongRandomIndex == (randomIndexList.Count - 1) : (currentSongIndex == currentPlaylist.Count - 1);
        public int CurrentVolume => (mp.Volume == -10000) ? 0 : (mp.Volume / 40 + 100);
        public int CurrentLength => (int)(mp.Duration * 10);

        public enum FilterType
        {
            Standard,
            And,
            Or
        }

        private Core()
        {
            mp.Volume = -2000;
            Repeat = true;
            Random = true;
        }

        public static Core Instance
        {
            get
            {
                if (instance == null)
                    instance = new Core();
                return instance;
            }
        }

        #region Input and output file handling...
        /// <summary>
        /// Creates a new XML file for settings to be saved into.
        /// </summary>
        /// <param name="file">New settings XML file to be created.</param>
        public void NewSettings(string file)
        {
            SettingsFilePath = file;
            conf.NewSettings(file);
        }

        /// <summary>
        /// Loads all data from file.
        /// </summary>
        /// <param name="file">Input settings XML file.</param>
        public void LoadSettings(string file)
        {
            try
            {
                conf.LoadSettings(file);
                SettingsFilePath = file;
            }
            catch (Exception e) { MessageBox.Show(string.Format("Could not load settings from {0}. Error message:\n\n{1}", file, e.ToString())); }
        }

        /// <summary>
        /// Saves all data into file.
        /// </summary>
        /// <param name="file">Output settings XML file.</param>
        public void SaveSettings(string file)
        {
            try
            {
                conf.SaveUserSettings(tags, allSongs, file);
                SettingsFilePath = file;
            }
            catch (Exception e) { MessageBox.Show(string.Format("Could not save settings into {0}. Error message:\n\n{1}", file, e.ToString())); }
        }
        #endregion

        #region Playlist generation...
        /// <summary>
        /// Creates new playlist by filtering songs with provided filter tags, enabling AND or OR logic between individual tags.
        /// </summary>
        /// <param name="tags">Filter tags which songs have to have.</param>
        /// <param name="useAndFilter">Use AND between HasTags?</param>
        /// <returns></returns>
        public ObservableCollection<Song> CreatePlaylist(ObservableCollection<Tag> tags, FilterType filterType)
        {
            // Stop playing if anything is playing.
            Stop();
            var result = new ObservableCollection<Song>();

            try
            {
                // If there is at least one tag, filter.
                if (tags.Count > 0)
                {
                    // If use Standard filter, apply OR between tags with the same category and AND between categories of tags.
                    if (filterType == FilterType.Standard)
                    {
                        // Split filters by categories.
                        var cats = new List<List<Tag>>();
                        foreach (var t in tags)
                        {
                            bool found = false;
                            foreach (var l in cats)
                                if (l[0].Category == t.Category)
                                {
                                    found = true;
                                    l.Add(t);
                                }
                            if (!found)
                                cats.Add(new List<Tag>() { t });
                        }
                        foreach (var s in allSongs.Values)
                        {
                            // Does at least 1 of song's tags match tags in category?
                            var finds = new List<bool>();
                            foreach (var c in cats)
                            {
                                finds.Add(false);
                                foreach (var t in c)
                                    if (s.tags.ContainsKey(t.ID))
                                        finds[finds.Count - 1] = true;
                            }
                            // Was song matching for at least 1 tag per category?
                            var correct = true;
                            foreach (var b in finds)
                            {
                                if (!b)
                                    correct = false;
                                break;
                            }

                            if (correct)
                                result.Add(s);
                        }
                    }
                    // If use And filter, make sure every song in output has all filter tags.
                    else if (filterType == FilterType.And)
                    {
                        foreach (var s in allSongs.Values)
                        {
                            bool matches = true;
                            foreach (var t in tags)
                                if (!s.tags.ContainsKey(t.ID))
                                {
                                    matches = false;
                                    break;
                                }

                            if (matches)
                                result.Add(s);
                        }
                    }
                    // If use Or filter, make sure every song in output has at least one of filter tags.
                    else
                        foreach (var t in tags)
                            foreach (var s in t.songs)
                                if (!result.Contains(s.Value))
                                    result.Add(s.Value);
                }
                // If there are no filter tags provided, return all songs in existence.
                else
                    foreach (var s in allSongs.Values)
                        result.Add(s);

                // Remember created playlist as new current playlist and generate randomized alternative for it.
                currentPlaylist = result;
                GenerateRandomPlaylist();
            }
            catch (Exception e) { MessageBox.Show(string.Format("Something went wrong when attempting to provide filtered playlist. Error message:\n\n{0}", e.ToString())); }

            return result;
        }

        /// <summary>
        /// Generates randomized alternative for current playlist.
        /// </summary>
        private void GenerateRandomPlaylist()
        {
            try
            {
                randomIndexList.Clear();

                var random = new Random();
                var tmpList = new List<int>();

                for (var i = 0; i < currentPlaylist.Count; i++)
                    tmpList.Add(i);

                var amount = tmpList.Count;
                for (var i = 0; i < amount; i++)
                {
                    int rnd = random.Next(0, tmpList.Count);
                    randomIndexList.Add(tmpList[rnd]);
                    tmpList.RemoveAt(rnd);
                }
            }
            catch (Exception e) { MessageBox.Show(string.Format("Random version of generated filtered playlist could not be created. Error message:\n\n{0}", e.ToString())); }
        }
        #endregion

        #region Import list handling...
        /// <summary>
        /// Checks validity of provided file paths, creates song objects for them and inserts them into import list.
        /// </summary>
        /// <param name="filePaths">Paths to input files.</param>
        public void AddIntoImport(List<string> filePaths)
        {
            try
            {
                // Clean up paths to standart appearance.
                try
                {
                    for (var i = 0; i < filePaths.Count; i++)
                        filePaths[i] = filePaths[i].Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                }
                catch (Exception e) { throw new Exception("Could not clean directory separators in import paths.", e); }

                // Check whether files exist and whether they are supported. In that case, create song objects and add them into import.
                try
                {
                    foreach (var s in filePaths)
                    {
                        if (File.Exists(s) && Utilities.IsFileSupported(s))
                        {
                            var newSong = new Song(s) { Save = false };

                            if (!allSongs.ContainsKey(newSong.FullPath))
                            {
                                importList.Add(newSong);
                                allSongs.Add(newSong.FullPath, newSong);
                            }
                            else if (!importList.Contains(allSongs[newSong.FullPath]))
                                importList.Add(allSongs[newSong.FullPath]);
                        }
                    }
                }
                catch (Exception e) { throw new Exception("Error occured while attempting to create song objects from provided import paths.", e); }
            }
            catch (Exception e) { MessageBox.Show(string.Format("Could not add at least one of the provided file paths into the import list. Error message:\n\n{0}", e.ToString())); }
        }

        /// <summary>
        /// Removes all given songs from current import list.
        /// </summary>
        /// <param name="forRemoval">Collection of songs to be removed.</param>
        public void RemoveFromImport(ObservableCollection<Song> forRemoval)
        {
            try
            {
                var removeList = new List<Song>();
                foreach (var s in forRemoval)
                    removeList.Add(s);

                foreach (var s in removeList)
                {
                    if (s == previewSong)
                        Stop();
                    importList.Remove(s);
                }
            }
            catch (Exception e) { MessageBox.Show(string.Format("Could not remove provided songs from import list. Following error occured:\n\n{0}", e.ToString())); }
        }

        /// <summary>
        /// Clear all songs from import list, which already have at least one tag assigned.
        /// </summary>
        public void ClearImport()
        {
            try
            {
                var remove = new List<Song>();
                foreach (var s in importList)
                    if (s.tags.Count > 0)
                        remove.Add(s);
                foreach (var s in remove)
                    importList.Remove(s);
            }
            catch (Exception e) { MessageBox.Show(string.Format("Could not clear the import list. Following error occured:\n\n{0}", e.ToString())); }
        }

        /// <summary>
        /// Assigns given tags to given songs.
        /// </summary>
        /// <param name="songs">Songs to be tagged.</param>
        /// <param name="tags">Tags to be added to songs.</param>
        /// <param name="remove">Remove songs from import list afterwards?</param>
        /// <param name="overwrite">Remove current tags from given songs before adding new ones?</param>
        public void AssignTags(ObservableCollection<Song> songs, ObservableCollection<Tag> tags, bool remove, bool overwrite)
        {
            try
            {
                // If tags are to be overwritten, start by removing any old ones.
                try
                {
                    if (overwrite)
                        foreach (var s in songs)
                            s.RemoveFromTags();
                }
                catch (Exception e) { throw new Exception("Error occured while attempting to remove original tags from songs.", e); }

                // Assign all tags to all songs, make sure they are saved into config file on next save.
                try
                {
                    foreach (var t in tags)
                        foreach (var s in songs)
                        {
                            t.AddSong(s);
                            s.Save = true;
                        }
                    if (!remove)
                        for (var i = 0; i < songs.Count; i++)
                            importList[importList.IndexOf(songs[i])] = songs[i];
                }
                catch (Exception e) { throw new Exception("Could not assing tags to songs.", e); }

                // If songs are to be removed from import, remove all of them from there.
                if (remove)
                    RemoveFromImport(songs);
            }
            catch (Exception e) { MessageBox.Show(string.Format("Could not assign tags to songs, or something else failed during the process. Error message:\n\n{0}", e.ToString())); }
        }
        #endregion

        #region Tag management...
        /// <summary>
        /// Finds next free tag ID in tag collection (auto increment).
        /// </summary>
        /// <returns>Next free tag ID.</returns>
        public int GetNextFreeTagID()
        {
            var id = -1;
            try
            {
                if (tags.Count > 0)
                {
                    foreach (var t in tags)
                        if (t.ID > id)
                            id = t.ID;
                    id++;
                }
                else
                    id = 0;
            }
            catch (Exception e) { MessageBox.Show(string.Format("Failed to retrieve next free tag ID. Error message:\n\n{0}", e.ToString())); }
            return id;
        }

        /// <summary>
        /// Creates a new tag.
        /// </summary>
        /// <param name="name">Name of tag.</param>
        /// <param name="category">Category tag falls into.</param>
        public void CreateTag(string name, string category)
        {
            try
            {
                if ((name.Length > 0) && (category.Length > 0))
                    tags.Add(new Tag() { ID = GetNextFreeTagID(), Name = name, Category = category });
                else
                    MessageBox.Show("Please, enter both name and category for new tag.");
            }
            catch (Exception e) { MessageBox.Show(string.Format("Could not create tag with name {0} and category {1}. Following error occured:\n\n{2}", name, category, e.ToString())); }
        }

        /// <summary>
        /// Deletes provided tag from both tag collection and from songs as well.
        /// </summary>
        /// <param name="tag">Tag to be removed.</param>
        public void RemoveTag(Tag tag)
        {
            try
            {
                if (tag != null)
                {
                    tag.RemoveFromSongs();
                    tags.Remove(tag);
                }
                else
                    MessageBox.Show("No tag was selected - nothing to remove.");
            }
            catch (Exception e) { MessageBox.Show(string.Format("Could not remove provided tag. Error message:\n\n{0}", e.ToString())); }
        }

        /// <summary>
        /// Edits provided tag's name and category.
        /// </summary>
        /// <param name="tag">Tag to be edited.</param>
        /// <param name="name">Tag's new name.</param>
        /// <param name="category">Tag's new category.</param>
        public void UpdateTag(Tag tag, string name, string category)
        {
            try
            {
                if (tag != null)
                {
                    if ((name.Length > 0) && (category.Length > 0))
                    {
                        tag.Name = name;
                        tag.Category = category;
                        tags[tags.IndexOf(tag)] = tag;
                    }
                    else
                        MessageBox.Show("Please, make sure that both new name and category for edited tag are non-empty.");
                }
                else
                    MessageBox.Show("No tag was selected - nothing to edit.");
            }
            catch (Exception e) { MessageBox.Show(string.Format("Could not edit provided tag. Error message:\n\n{0}", e.ToString())); }
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="song"></param>
        private void SetCurrentSong(Song song)
        {
            currentSong = song;

            if (song != null)
            {
                currentSongIndex = currentPlaylist.IndexOf(song);
                currentSongRandomIndex = randomIndexList.IndexOf(currentSongIndex);

                if (File.Exists(song.FullPath))
                {
                    mp.FileName = song.FullPath;
                    mp.Pause();
                }
                else
                    Next();
            }
            else
            {
                currentSongIndex = -1;
                currentSongRandomIndex = -1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Play()
        {
            supposedToBePlaying = true;
            if ((currentSong == null) && (currentPlaylist.Count > 0))
                if (Random && (currentPlaylist.Count > 0))
                    SetCurrentSong(currentPlaylist[randomIndexList[0]]);
                else
                    SetCurrentSong(currentPlaylist[0]);
            if ((currentSong != null) || (previewSong != null))
                mp.Play();
            else
                supposedToBePlaying = false;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Pause()
        {
            supposedToBePlaying = false;
            mp.Pause();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="song"></param>
        public void PlaySong(Song song)
        {
            SetCurrentSong(song);
            Play();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Next()
        {
            if ((currentSong != null) && (currentPlaylist.Count > 0) && (previewSong == null))
            {
                mp.Stop();

                if (IsCurrentLast)
                {
                    if (Repeat)
                        First();
                    else
                        Stop();
                }
                else
                {
                    if (Random)
                        SetCurrentSong(currentPlaylist[randomIndexList[currentSongRandomIndex + 1]]);
                    else
                        SetCurrentSong(currentPlaylist[currentSongIndex + 1]);
                }
            }
            if (previewSong != null)
                Stop();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Previous()
        {
            if ((currentSong != null) && (currentPlaylist.Count > 0) && (previewSong == null))
            {
                if (mp.CurrentPosition < 1)
                {
                    mp.Stop();

                    if (IsCurrentFirst)
                        Last();
                    else
                    {
                        if (Random)
                            SetCurrentSong(currentPlaylist[randomIndexList[currentSongRandomIndex - 1]]);
                        else
                            SetCurrentSong(currentPlaylist[currentSongIndex - 1]);
                    }
                }
                else
                    MoveToTime(0);
            }
            if (previewSong != null)
                MoveToTime(0);
        }

        /// <summary>
        /// 
        /// </summary>
        public void First()
        {
            if ((currentPlaylist.Count > 0) && (previewSong == null))
            {
                if (Random)
                    SetCurrentSong(currentPlaylist[randomIndexList[0]]);
                else
                    SetCurrentSong(currentPlaylist[0]);
            }
            if (previewSong != null)
                MoveToTime(0);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Last()
        {
            if ((currentPlaylist.Count) > 0 && (previewSong == null))
            {
                if (Random)
                    SetCurrentSong(currentPlaylist[randomIndexList[randomIndexList.Count - 1]]);
                else
                    SetCurrentSong(currentPlaylist[currentPlaylist.Count - 1]);
            }
            if (previewSong != null)
                Stop();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            if (currentSong != null)
            {
                supposedToBePlaying = false;
                mp.Stop();
                SetCurrentSong(null);
            }
            if (previewSong != null)
            {
                supposedToBePlaying = false;
                mp.Stop();
                previewSong = null;
            }
        }

        #region Volume settings...
        /// <summary>
        /// 
        /// </summary>
        public void Mute() => mp.Mute = true;

        /// <summary>
        /// 
        /// </summary>
        public void Unmute() => mp.Mute = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="volume"></param>
        public void SetVolume(double volume) => mp.Volume = (volume > 0) ? (((int)volume - 100) * 40) : -10000;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        public void MoveToTime(int time) => mp.CurrentPosition = time / 10;

        /// <summary>
        /// 
        /// </summary>
        public void CheckIsTimeForNext()
        {
            if (mp.PlayState == MediaPlayer.MPPlayStateConstants.mpStopped)
            {
                if (supposedToBePlaying)
                    Next();
                else
                    previewSong = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int GetCurrentPosition()
        {
            if ((int)(mp.CurrentPosition * 10) < (int)(mp.Duration * 10))
                return (int)(mp.CurrentPosition * 10);
            else
                return CurrentLength;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Song GetCurrentSong() => previewSong != null ? previewSong : currentSong;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="song"></param>
        public void PlayPreview(Song song)
        {
            if (File.Exists(song.FullPath))
            {
                supposedToBePlaying = false;
                previewSong = song;
                mp.Stop();
                mp.FileName = song.FullPath;
                mp.Play();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="song"></param>
        /// <param name="destinationPath"></param>
        public void MoveSong(Song song, string destinationPath)
        {
            if (File.Exists(song.FullPath))
            {
                if (!File.Exists(destinationPath))
                    song.Move(destinationPath);
                else
                    MessageBox.Show("Such file already exists!");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="songs"></param>
        /// <param name="targetDir"></param>
        public void MoveSongs(ObservableCollection<Song> songs, string targetDir)
        {
            foreach (var s in songs)
                s.Move(targetDir + s.FileName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="song"></param>
        public void RemoveSong(Song song)
        {
            if ((song == currentSong) || (song == previewSong))
                Stop();
            currentPlaylist.Remove(song);
            song.RemoveFromTags();
            importList.Remove(song);
            allSongs.Remove(song.FullPath);
            randomIndexList.Remove(randomIndexList.IndexOf(randomIndexList.Count - 1));
        }
    }
}