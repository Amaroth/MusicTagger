using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace MusicTagger2.Core
{
    class SongPlayList
    {
        public Song CurrentNormalSong { get; private set; }
        public Song CurrentPreviewSong { get; private set; }
        public Song GetCurrentSong() => CurrentPreviewSong ?? CurrentNormalSong;
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
            if ((song == CurrentNormalSong) || (song == CurrentPreviewSong))
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
                    {
                        matches = false;
                        break;
                    }
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

            for (var i = 0; i < CurrentPlayList.Count; i++)
            {
                int rnd = random.Next(0, tmpList.Count);
                randomIndexList.Add(tmpList[rnd]);
                tmpList.RemoveAt(rnd);
            }
        }






        private void SetCurrentSong(Song song)
        {
            CurrentNormalSong = song;

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
            if ((CurrentNormalSong == null) && (CurrentPlayList.Count > 0))
                if (Random && (CurrentPlayList.Count > 0))
                    SetCurrentSong(CurrentPlayList[randomIndexList[0]]);
                else
                    SetCurrentSong(CurrentPlayList[0]);
            if ((CurrentNormalSong != null) || (CurrentPreviewSong != null))
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
            if ((CurrentNormalSong != null) && (CurrentPlayList.Count > 0) && (CurrentPreviewSong == null))
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
            if (CurrentPreviewSong != null)
                Stop();
        }

        public void Previous()
        {
            if ((CurrentNormalSong != null) && (CurrentPlayList.Count > 0) && (CurrentPreviewSong == null))
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
            if (CurrentPreviewSong != null)
                CurrentPosition = 0;
        }

        public void First()
        {
            if ((CurrentPlayList.Count > 0) && (CurrentPreviewSong == null))
            {
                if (Random)
                    SetCurrentSong(CurrentPlayList[randomIndexList[0]]);
                else
                    SetCurrentSong(CurrentPlayList[0]);
            }
            if (CurrentPreviewSong != null)
                CurrentPosition = 0;
        }

        public void Last()
        {
            if ((CurrentPlayList.Count) > 0 && (CurrentPreviewSong == null))
            {
                if (Random)
                    SetCurrentSong(CurrentPlayList[randomIndexList[randomIndexList.Count - 1]]);
                else
                    SetCurrentSong(CurrentPlayList[CurrentPlayList.Count - 1]);
            }
            if (CurrentPreviewSong != null)
                Stop();
        }

        public void Stop()
        {
            if (CurrentNormalSong != null)
            {
                supposedToBePlaying = false;
                mp.Stop();
                SetCurrentSong(null);
            }
            if (CurrentPreviewSong != null)
            {
                supposedToBePlaying = false;
                mp.Stop();
                CurrentPreviewSong = null;
            }
        }


        

       
        


        public void CheckIsTimeForNext()
        {
            if (mp.PlayState == MediaPlayer.MPPlayStateConstants.mpStopped)
            {
                if (supposedToBePlaying)
                    Next();
                else
                    CurrentPreviewSong = null;
            }
        }

        

        


        public void PlayPreview(Song song)
        {
            if (File.Exists(song.FullPath))
            {
                supposedToBePlaying = false;
                CurrentPreviewSong = song;
                mp.Stop();
                mp.FileName = song.FullPath;
                mp.Play();
            }
        }
    }
}
