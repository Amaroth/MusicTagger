using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MusicTagger2.Core
{
    class SongPlayList
    {
        public ObservableCollection<Song> CurrentPlayList = new ObservableCollection<Song>();
        public List<int> randomIndexList { get; private set; } = new List<int>();
        public bool Random;
        public bool Repeat;

        public int CurrentSongIndex { get; private set; } = -1;
        private int currentSongRandomIndex = -1;
        private Song _currentSong;
        public Song CurrentSong
        {
            get => _currentSong;
            private set
            {
                if (value != null)
                {
                    CurrentSongIndex = CurrentPlayList.IndexOf(value);
                    currentSongRandomIndex = randomIndexList.IndexOf(CurrentSongIndex);
                }
                else
                {
                    CurrentSongIndex = -1;
                    currentSongRandomIndex = -1;
                }
                _currentSong = value;
            }
        }
        private bool IsCurrentFirst => Random ? (currentSongRandomIndex == 0) : (CurrentSongIndex == 0);
        private bool IsCurrentLast => Random ? currentSongRandomIndex == (randomIndexList.Count - 1) : (CurrentSongIndex == CurrentPlayList.Count - 1);

        public Uri Next()
        {
            Song result = null;
            if ((CurrentSong != null) && (CurrentPlayList.Count > 0))
            {
                if (IsCurrentLast)
                {
                    if (Repeat)
                        return First();
                }
                else
                    result = Random ? CurrentPlayList[randomIndexList[currentSongRandomIndex + 1]] : CurrentPlayList[CurrentSongIndex + 1];
            }

            CurrentSong = result;
            if (result == null)
                return null;
            return new Uri(CurrentSong.FullPath);
        }

        public Uri Previous()
        {
            Song result = null;

            if ((CurrentSong != null) && (CurrentPlayList.Count > 0))
            {
                if (IsCurrentFirst)
                    return Last();
                else
                    result = Random ? CurrentPlayList[randomIndexList[currentSongRandomIndex - 1]] : CurrentPlayList[CurrentSongIndex - 1];
            }

            CurrentSong = result;
            if (result == null)
                return null;
            return new Uri(CurrentSong.FullPath);
        }

        public Uri First()
        {
            Song result = null;
            if (CurrentPlayList.Count > 0)
                result = Random ? CurrentPlayList[randomIndexList[0]] : CurrentPlayList[0];

            CurrentSong = result;
            if (result == null)
                return null;
            return new Uri(CurrentSong.FullPath);
        }

        public Uri Last()
        {
            Song result = null;
            if (CurrentPlayList.Count > 0)
                result = Random ? CurrentPlayList[randomIndexList[randomIndexList.Count - 1]] : CurrentPlayList[CurrentPlayList.Count - 1];

            CurrentSong = result;
            if (result == null)
                return null;
            return new Uri(CurrentSong.FullPath);
        }

        public Uri SetCurrent(Song song)
        {
            CurrentSong = song;
            if (CurrentSong == null)
                return null;
            return new Uri(CurrentSong.FullPath);
        }

        public void RemoveSong(Song song)
        {
            randomIndexList.Remove(CurrentPlayList.IndexOf(song));
            CurrentPlayList.Remove(song);
        }

        public void GenerateFilteredPlayList(List<SongTag> filterTags, Core.FilterType filterType)
        {
            try
            {
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
    }
}
