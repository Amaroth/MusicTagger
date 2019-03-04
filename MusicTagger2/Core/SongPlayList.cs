using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace MusicTagger2.Core
{
    class SongPlayList
    {
        public List<int> randomIndexList = new List<int>();
        public Song currentSong;
        public Song previewSong;
        public int currentSongIndex = -1;
        private int currentSongRandomIndex = -1;
        public ObservableCollection<Song> CurrentPlayList = new ObservableCollection<Song>();

        public bool Random = true;
        public bool Repeat = true;
        private bool supposedToBePlaying;

        private MediaPlayer.MediaPlayer mp = new MediaPlayer.MediaPlayer() { Volume = -2000 };
        public bool IsReallyPlaying => mp.PlayState == MediaPlayer.MPPlayStateConstants.mpPlaying;
        private bool IsCurrentFirst => Random ? (currentSongRandomIndex == 0) : (currentSongIndex == 0);
        private bool IsCurrentLast => Random ? currentSongRandomIndex == (randomIndexList.Count - 1) : (currentSongIndex == CurrentPlayList.Count - 1);
        
        public int CurrentLength => (int)(mp.Duration * 10);

        public int Volume
        {
            get => (mp.Volume == -10000) ? 0 : (mp.Volume / 40 + 100);
            set => mp.Volume = (value > 0) ? ((value - 100) * 40) : -10000;
        }

        public bool Muted
        {
            get => mp.Mute;
            set => mp.Mute = value;
        }

        public int CurrentPosition
        {
            get
            {
                if ((int)(mp.CurrentPosition * 10) < (int)(mp.Duration * 10))
                    return (int)(mp.CurrentPosition * 10);
                else
                    return CurrentLength;
            }
            set => mp.CurrentPosition = value / 10;
        }

        public void RemovePreview(Song song)
        {
            if (song == previewSong)
                Stop();
        }

        public void RemoveSong(Song song)
        {
            if ((song == currentSong) || (song == previewSong))
                Stop();
            randomIndexList.Remove(CurrentPlayList.IndexOf(song));
            CurrentPlayList.Remove(song);
        }

        public ObservableCollection<Song> CreatePlaylist(List<SongTag> filterTags, Core.FilterType filterType, ObservableCollection<SongTag> allTags, ObservableCollection<Song> allSongs)
        {
            // Stop playing if anything is playing.
            Stop();
            var result = new ObservableCollection<Song>();

            try
            {
                // If there is at least one tag, filter.
                if (filterTags.Count > 0)
                {
                    // If use Standard filter, apply OR between tags with the same category and AND between categories of tags.
                    if (filterType == Core.FilterType.Standard)
                    {
                        // Split filters by categories.
                        var cats = new List<List<SongTag>>();
                        foreach (var t in allTags)
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
                        foreach (var s in allSongs)
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
                    else if (filterType == Core.FilterType.And)
                    {
                        foreach (var s in allSongs)
                        {
                            bool matches = true;
                            foreach (var t in filterTags)
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
                        foreach (var t in filterTags)
                            foreach (var s in t.songs)
                                if (!result.Contains(s))
                                    result.Add(s);
                }
                // If there are no filter tags provided, return all songs in existence.
                else
                    foreach (var s in allSongs)
                        result.Add(s);

                // Remember created playlist as new current playlist and generate randomized alternative for it.
                CurrentPlayList = result;
                GenerateRandomPlaylist();
            }
            catch (Exception e) { MessageBox.Show(string.Format("Something went wrong when attempting to provide filtered playlist. Error message:\n\n{0}", e.ToString())); }

            return result;
        }

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
                    CurrentPosition = 0;
            }
            if (previewSong != null)
                CurrentPosition = 0;
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
                CurrentPosition = 0;
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
    }
}
