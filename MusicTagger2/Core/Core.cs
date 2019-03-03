using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

// FadeStop
// Sortování dle sloupců v listviewech
// Delete z import listu a HD
// Ukládání nastaveného volume


namespace MusicTagger2.Core
{
    /// <summary>
    /// Core handles all main functionalities and logical interaction between data, being controller behind GUI.
    /// </summary>
    class Core
    {
        private static Core instance;

        // Views
        public ObservableCollection<SongTag> SongTags = new ObservableCollection<SongTag>();
        public ObservableCollection<Song> ImportList = new ObservableCollection<Song>();
        public ObservableCollection<Song> CurrentPlayList = new ObservableCollection<Song>();

        // Data
        private Dictionary<int, SongTag> allSongTags = new Dictionary<int, SongTag>();
        private Dictionary<string, Song> allSongs = new Dictionary<string, Song>();
        public enum FilterType { Standard, And, Or }

        #region Singleton implementation...
        private Core()
        {
            
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
        #endregion

        #region Input and output file handling...
        /// <summary>
        /// Create a new settings file in a given path.
        /// </summary>
        /// <param name="filePath"></param>
        public void NewSettings(string filePath)
        {
            ClearAll();
            var writer = new SettingsWriter();
            writer.WriteSettings(filePath, allSongs, allSongTags);
        }

        /// <summary>
        /// Load settings from file in a given path.
        /// </summary>
        /// <param name="filePath"></param>
        public void LoadSettings(string filePath)
        {
            try
            {
                ClearAll();

                var reader = new SettingsReader();
                reader.ReadSettings(filePath);
                allSongTags = reader.GetSongTags();
                allSongs = reader.GetSongs(allSongTags);
                foreach (var st in allSongTags)
                    SongTags.Add(st.Value);
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Could not load settings from {0}. Error message:\n\n{1}", filePath, e.ToString()));
            }
        }

        /// <summary>
        /// Save settings to file in a given path.
        /// </summary>
        /// <param name="filePath"></param>
        public void SaveSettings(string filePath)
        {
            try
            {
                var writer = new SettingsWriter();
                writer.WriteSettings(filePath, allSongs, allSongTags);
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Could not save settings into {0}. Error message:\n\n{1}", filePath, e.ToString()));
            }
        }

        /// <summary>
        /// Empty all collections.
        /// </summary>
        private void ClearAll()
        {
            SongTags.Clear();
            ImportList.Clear();
            CurrentPlayList.Clear();
            allSongTags.Clear();
            allSongs.Clear();
            randomIndexList.Clear();
        }
        #endregion

        #region Playlist management - remove to dedicated class later
        public ObservableCollection<Song> CreatePlaylist(ObservableCollection<SongTag> tags, FilterType filterType)
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
                        var cats = new List<List<SongTag>>();
                        foreach (var t in allSongTags.Values)
                        {
                            bool found = false;
                            foreach (var l in cats)
                                if (l[0].Category == t.Category)
                                {
                                    found = true;
                                    l.Add(t);
                                }
                            if (!found)
                                cats.Add(new List<SongTag>() { t });
                        }
                        foreach (var s in allSongs.Values)
                        {
                            // Does at least 1 of song's tags match tags in category?
                            var finds = new List<bool>();
                            foreach (var c in cats)
                            {
                                finds.Add(false);
                                foreach (var t in c)
                                    if (s.tags.Contains(t))
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
                                if (!s.tags.Contains(t))
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
                                if (!result.Contains(s))
                                    result.Add(s);
                }
                // If there are no filter tags provided, return all songs in existence.
                else
                    foreach (var s in allSongs.Values)
                        result.Add(s);

                // Remember created playlist as new current playlist and generate randomized alternative for it.
                CurrentPlayList = result;
                GenerateRandomPlaylist();
            }
            catch (Exception e) { MessageBox.Show(string.Format("Something went wrong when attempting to provide filtered playlist. Error message:\n\n{0}", e.ToString())); }

            return result;
        }
        #endregion


        private void GenerateRandomPlaylist()
        {
            try
            {
                randomIndexList.Clear();

                var random = new Random();
                var tmpList = new List<int>();

                for (var i = 0; i < CurrentPlayList.Count; i++)
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
                            var newSong = new Song(s) { WasTagged = false };

                            if (!allSongs.ContainsKey(newSong.FullPath))
                            {
                                ImportList.Add(newSong);
                                allSongs.Add(newSong.FullPath, newSong);
                            }
                            else if (!ImportList.Contains(allSongs[newSong.FullPath]))
                                ImportList.Add(allSongs[newSong.FullPath]);
                        }
                    }
                }
                catch (Exception e) { throw new Exception("Error occured while attempting to create song objects from provided import paths.", e); }
            }
            catch (Exception e) { MessageBox.Show(string.Format("Could not add at least one of the provided file paths into the import list. Error message:\n\n{0}", e.ToString())); }
        }

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
                    ImportList.Remove(s);
                }
            }
            catch (Exception e) { MessageBox.Show(string.Format("Could not remove provided songs from import list. Following error occured:\n\n{0}", e.ToString())); }
        }

        public void ClearImport()
        {
            try
            {
                var remove = new List<Song>();
                foreach (var s in ImportList)
                    if (s.tags.Count > 0)
                        remove.Add(s);
                foreach (var s in remove)
                    ImportList.Remove(s);
            }
            catch (Exception e) { MessageBox.Show(string.Format("Could not clear the import list. Following error occured:\n\n{0}", e.ToString())); }
        }

        public void AssignTags(ObservableCollection<Song> songs, ObservableCollection<SongTag> tags, bool remove, bool overwrite)
        {
            try
            {
                // If tags are to be overwritten, start by removing any old ones.
                try
                {
                    if (overwrite)
                        foreach (var s in songs)
                            s.RemoveFromAllTags();
                }
                catch (Exception e) { throw new Exception("Error occured while attempting to remove original tags from songs.", e); }

                // Assign all tags to all songs, make sure they are saved into config file on next save.
                try
                {
                    foreach (var t in tags)
                        foreach (var s in songs)
                        {
                            t.AddSong(s);
                            s.WasTagged = true;
                        }
                    if (!remove)
                        for (var i = 0; i < songs.Count; i++)
                            ImportList[ImportList.IndexOf(songs[i])] = songs[i];
                }
                catch (Exception e) { throw new Exception("Could not assing tags to songs.", e); }

                // If songs are to be removed from import, remove all of them from there.
                if (remove)
                    RemoveFromImport(songs);
            }
            catch (Exception e) { MessageBox.Show(string.Format("Could not assign tags to songs, or something else failed during the process. Error message:\n\n{0}", e.ToString())); }
        }

        public int GetNextFreeTagID()
        {
            var id = -1;
            try
            {
                if (allSongTags.Count > 0)
                {
                    foreach (var t in allSongTags.Values)
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

        public void CreateTag(string name, string category)
        {
            try
            {
                if ((name.Length > 0) && (category.Length > 0))
                {
                    var id = GetNextFreeTagID();
                    allSongTags.Add(id, new SongTag() { ID = id, Name = name, Category = category });
                }
                else
                    MessageBox.Show("Please, enter both name and category for new tag.");
            }
            catch (Exception e) { MessageBox.Show(string.Format("Could not create tag with name {0} and category {1}. Following error occured:\n\n{2}", name, category, e.ToString())); }
        }

        public void RemoveTag(SongTag tag)
        {
            try
            {
                if (tag != null)
                {
                    tag.RemoveFromSongs();
                    allSongTags.Remove(tag.ID);
                }
                else
                    MessageBox.Show("No tag was selected - nothing to remove.");
            }
            catch (Exception e) { MessageBox.Show(string.Format("Could not remove provided tag. Error message:\n\n{0}", e.ToString())); }
        }

        public void UpdateTag(SongTag tag, string name, string category)
        {
            try
            {
                if (tag != null)
                {
                    if ((name.Length > 0) && (category.Length > 0))
                    {
                        tag.Name = name;
                        tag.Category = category;
                        SongTags[SongTags.IndexOf(tag)] = tag;
                    }
                    else
                        MessageBox.Show("Please, make sure that both new name and category for edited tag are non-empty.");
                }
                else
                    MessageBox.Show("No tag was selected - nothing to edit.");
            }
            catch (Exception e) { MessageBox.Show(string.Format("Could not edit provided tag. Error message:\n\n{0}", e.ToString())); }
        }


        private void SetCurrentSong(Song song)
        {
            currentSong = song;

            if (song != null)
            {
                currentSongIndex = CurrentPlayList.IndexOf(song);
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

        public void Play()
        {
            supposedToBePlaying = true;
            if ((currentSong == null) && (CurrentPlayList.Count > 0))
                if (Random && (CurrentPlayList.Count > 0))
                    SetCurrentSong(CurrentPlayList[randomIndexList[0]]);
                else
                    SetCurrentSong(CurrentPlayList[0]);
            if ((currentSong != null) || (previewSong != null))
                mp.Play();
            else
                supposedToBePlaying = false;
        }


        public void Pause()
        {
            supposedToBePlaying = false;
            mp.Pause();
        }

        public void PlaySong(Song song)
        {
            SetCurrentSong(song);
            Play();
        }

        public void Next()
        {
            if ((currentSong != null) && (CurrentPlayList.Count > 0) && (previewSong == null))
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
                        SetCurrentSong(CurrentPlayList[randomIndexList[currentSongRandomIndex + 1]]);
                    else
                        SetCurrentSong(CurrentPlayList[currentSongIndex + 1]);
                }
            }
            if (previewSong != null)
                Stop();
        }

        public void Previous()
        {
            if ((currentSong != null) && (CurrentPlayList.Count > 0) && (previewSong == null))
            {
                if (mp.CurrentPosition < 1)
                {
                    mp.Stop();

                    if (IsCurrentFirst)
                        Last();
                    else
                    {
                        if (Random)
                            SetCurrentSong(CurrentPlayList[randomIndexList[currentSongRandomIndex - 1]]);
                        else
                            SetCurrentSong(CurrentPlayList[currentSongIndex - 1]);
                    }
                }
                else
                    MoveToTime(0);
            }
            if (previewSong != null)
                MoveToTime(0);
        }

        public void First()
        {
            if ((CurrentPlayList.Count > 0) && (previewSong == null))
            {
                if (Random)
                    SetCurrentSong(CurrentPlayList[randomIndexList[0]]);
                else
                    SetCurrentSong(CurrentPlayList[0]);
            }
            if (previewSong != null)
                MoveToTime(0);
        }

        public void Last()
        {
            if ((CurrentPlayList.Count) > 0 && (previewSong == null))
            {
                if (Random)
                    SetCurrentSong(CurrentPlayList[randomIndexList[randomIndexList.Count - 1]]);
                else
                    SetCurrentSong(CurrentPlayList[CurrentPlayList.Count - 1]);
            }
            if (previewSong != null)
                Stop();
        }

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


        public void Mute() => mp.Mute = true;

        public void Unmute() => mp.Mute = false;

        public void SetVolume(double volume) => mp.Volume = (volume > 0) ? (((int)volume - 100) * 40) : -10000;


        public void MoveToTime(int time) => mp.CurrentPosition = time / 10;

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

        public int GetCurrentPosition()
        {
            if ((int)(mp.CurrentPosition * 10) < (int)(mp.Duration * 10))
                return (int)(mp.CurrentPosition * 10);
            else
                return CurrentLength;
        }

        public Song GetCurrentSong() => previewSong != null ? previewSong : currentSong;


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

        public void MoveSongs(ObservableCollection<Song> songs, string targetDir)
        {
            foreach (var s in songs)
                s.Move(targetDir + s.FileName);
        }

        public void RemoveSong(Song song)
        {
            if ((song == currentSong) || (song == previewSong))
                Stop();
            CurrentPlayList.Remove(song);
            song.RemoveFromAllTags();
            ImportList.Remove(song);
            allSongs.Remove(song.FullPath);
            randomIndexList.Remove(randomIndexList.IndexOf(randomIndexList.Count - 1));
        }

        public List<int> randomIndexList = new List<int>();
        private Song currentSong;
        private Song previewSong;
        public int currentSongIndex = -1;
        private int currentSongRandomIndex = -1;

        //public ObservableCollection<Song> allSongs = new ObservableCollection<Song>();
        //private Dictionary<string, Song> songs = new Dictionary<string, Song>();
        public bool Random = true;
        public bool Repeat = true;
        //public string SettingsFilePath;
        private bool supposedToBePlaying;

        private MediaPlayer.MediaPlayer mp = new MediaPlayer.MediaPlayer() { Volume = -2000 };
        public bool IsReallyPlaying => mp.PlayState == MediaPlayer.MPPlayStateConstants.mpPlaying;
        private bool IsCurrentFirst => Random ? (currentSongRandomIndex == 0) : (currentSongIndex == 0);
        private bool IsCurrentLast => Random ? currentSongRandomIndex == (randomIndexList.Count - 1) : (currentSongIndex == CurrentPlayList.Count - 1);
        public int CurrentVolume => (mp.Volume == -10000) ? 0 : (mp.Volume / 40 + 100);
        public int CurrentLength => (int)(mp.Duration * 10);
    }
}