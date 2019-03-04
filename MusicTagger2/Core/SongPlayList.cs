using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace MusicTagger2.Core
{
    class SongPlayList
    {
        public Song currentSong { get; private set; }
        public Song previewSong { get; private set; }
        public ObservableCollection<Song> CurrentPlayList { get; private set; } = new ObservableCollection<Song>();
        public List<int> randomIndexList { get; private set; } = new List<int>();

        public bool Random = true;
        public bool Repeat = true;
        public int CurrentLength => (int)(mp.Duration * 10);
        public bool IsReallyPlaying => mp.PlayState == MediaPlayer.MPPlayStateConstants.mpPlaying;
        public int CurrentSongIndex { get; private set; } = -1;

        private int currentSongRandomIndex = -1;
        private bool supposedToBePlaying;
        private MediaPlayer.MediaPlayer mp = new MediaPlayer.MediaPlayer() { Volume = -2000 };
        private bool IsCurrentFirst => Random ? (currentSongRandomIndex == 0) : (CurrentSongIndex == 0);
        private bool IsCurrentLast => Random ? currentSongRandomIndex == (randomIndexList.Count - 1) : (CurrentSongIndex == CurrentPlayList.Count - 1);

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

        public void RemoveSong(Song song)
        {
            if ((song == currentSong) || (song == previewSong))
                Stop();
            randomIndexList.Remove(CurrentPlayList.IndexOf(song));
            CurrentPlayList.Remove(song);
        }

        public void GenerateFilteredPlayList(List<SongTag> filterTags, Core.FilterType filterType)
        {
            try
            {
                Stop();
                if (filterTags.Count == 0)
                    CurrentPlayList = new ObservableCollection<Song>(Core.Instance.Songs);
                else
                {
                    CurrentPlayList.Clear();
                    switch (filterType)
                    {
                        case Core.FilterType.Standard: ApplyStandardFilter(filterTags); break;
                        case Core.FilterType.And: ApplyAndFilter(filterTags); break;
                        case Core.FilterType.Or: ApplyOrFilter(filterTags); break;
                    }
                }
                GenerateRandomPlaylist();
            }
            catch (Exception e)
            {
                throw new Exception("Failed to generate playlist based on selected filters.", e);
            }
        }

        private void ApplyStandardFilter(List<SongTag> filterTags)
        {
            var filterTagsByCategories = new List<List<SongTag>>();
            foreach (var t in filterTags)
            {
                var found = false;
                foreach (var l in filterTagsByCategories)
                    if (l[0].Category == t.Category)
                    {
                        found = true;
                        l.Add(t);
                        break;
                    }
                if (!found)
                    filterTagsByCategories.Add(new List<SongTag>() { t });
            }

            foreach (var song in Core.Instance.Songs)
            {
                var finds = new List<bool>();
                foreach (var category in filterTagsByCategories)
                {
                    finds.Add(false);
                    foreach (var tag in category)
                        if (song.tags.Contains(tag))
                            finds[finds.Count - 1] = true;
                }
                
                var matches = true;
                foreach (var result in finds)
                {
                    if (!result)
                        matches = false;
                    break;
                }

                if (matches)
                    CurrentPlayList.Add(song);
            }
        }

        private void ApplyAndFilter(List<SongTag> filterTags)
        {
            foreach (var s in Core.Instance.Songs)
            {
                var matches = true;
                foreach (var t in filterTags)
                    if (!s.tags.Contains(t))
                    {
                        matches = false;
                        break;
                    }

                if (matches)
                    CurrentPlayList.Add(s);
            }
        }

        private void ApplyOrFilter(List<SongTag> filterTags)
        {
            foreach (var t in filterTags)
                foreach (var s in t.songs)
                    if (!CurrentPlayList.Contains(s))
                        CurrentPlayList.Add(s);
        }

        private void GenerateRandomPlaylist()
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






        private void SetCurrentSong(Song song)
        {
            currentSong = song;

            if (song != null)
            {
                CurrentSongIndex = CurrentPlayList.IndexOf(song);
                currentSongRandomIndex = randomIndexList.IndexOf(CurrentSongIndex);

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
                CurrentSongIndex = -1;
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
                        SetCurrentSong(CurrentPlayList[CurrentSongIndex + 1]);
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
                            SetCurrentSong(CurrentPlayList[CurrentSongIndex - 1]);
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
