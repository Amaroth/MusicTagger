﻿using System;
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
        private SongPlayList CurrentSongPlayList = new SongPlayList();
        private ImportList CurrentImportList = new ImportList();

        public ObservableCollection<SongTag> SongTags = new ObservableCollection<SongTag>();
        public ObservableCollection<Song> Songs = new ObservableCollection<Song>();

        public ObservableCollection<Song> ImportList => CurrentImportList.Songs;
        public ObservableCollection<Song> CurrentPlayList => CurrentSongPlayList.CurrentPlayList;

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
        /// Creates a new, blank Project in a given path.
        /// </summary>
        /// <param name="filePath"></param>
        public void NewProject(string filePath)
        {
            ClearAll();
            var writer = new ProjectWriter();
            writer.WriteSettings(filePath, Songs, SongTags);
        }

        /// <summary>
        /// Opens and loads Project from given path.
        /// </summary>
        /// <param name="filePath"></param>
        public void LoadProject(string filePath)
        {
            ClearAll();
            var reader = new ProjectReader();
            reader.ReadSettings(filePath);
            SongTags = reader.GetSongTags();
            Songs = reader.GetSongs(SongTags);
        }

        /// <summary>
        /// Saves current Project into given path.
        /// </summary>
        /// <param name="filePath"></param>
        public void SaveProject(string filePath)
        {
            var writer = new ProjectWriter();
            writer.WriteSettings(filePath, Songs, SongTags);
        }

        /// <summary>
        /// Clear all current data.
        /// </summary>
        private void ClearAll()
        {
            SongTags.Clear();
            Songs.Clear();
            ImportList.Clear();
            CurrentPlayList.Clear();
        }
        #endregion

        #region PlayList delegation...
        public bool Muted
        {
            get => CurrentSongPlayList.Muted;
            set => CurrentSongPlayList.Muted = value;
        }

        public int Volume
        {
            get => CurrentSongPlayList.Volume;
            set => CurrentSongPlayList.Volume = value;
        }

        public int CurrentTime
        {
            get => CurrentSongPlayList.CurrentPosition;
            set => CurrentSongPlayList.CurrentPosition = value;
        }

        public bool Random
        {
            get => CurrentSongPlayList.Random;
            set => CurrentSongPlayList.Random = value;
        }

        public bool Repeat
        {
            get => CurrentSongPlayList.Repeat;
            set => CurrentSongPlayList.Repeat = value;
        }

        public void GenerateFilteredPlayList(List<SongTag> filterTags, FilterType filterType) => CurrentSongPlayList.GenerateFilteredPlayList(filterTags, filterType, SongTags, Songs);

        public Song GetCurrentSong() => CurrentSongPlayList.GetCurrentSong();

        public int GetCurrentLength() => CurrentSongPlayList.CurrentLength;

        public int GetCurrentSongIndex() => CurrentSongPlayList.CurrentSongIndex;

        public bool IsSongPlayListPlaying() => CurrentSongPlayList.IsReallyPlaying;

        public void PlaySong(Song song) => CurrentSongPlayList.PlaySong(song);

        public void PlayPreview(Song song) => CurrentSongPlayList.PlayPreview(song);

        public void CheckIsTimeForNext() => CurrentSongPlayList.CheckIsTimeForNext();

        public void Play() => CurrentSongPlayList.Play();

        public void Pause() => CurrentSongPlayList.Pause();

        public void Stop() => CurrentSongPlayList.Stop();

        public void Previous() => CurrentSongPlayList.Previous();

        public void Next() => CurrentSongPlayList.Next();

        public void First() => CurrentSongPlayList.First();

        public void Last() => CurrentSongPlayList.Last();
        #endregion

        #region Song's file changes...
        /// <summary>
        /// 
        /// </summary>
        /// <param name="song"></param>
        /// <param name="destinationPath"></param>
        public void MoveSongFile(Song song, string destinationPath)
        {
            try
            {
                song.Move(destinationPath);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Error occured while attempting to move song's file {0} to {1}.:\n{2}", song.FullPath, destinationPath, e.Message), e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="songs"></param>
        /// <param name="targetDir"></param>
        public void MoveSongsToDir(List<Song> songs, string targetDir)
        {
            try
            {
                foreach (var song in songs)
                    song.Move(targetDir + song.FileName);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Error occured while atempting to move selected songs to {0}.:\n{1}", targetDir, e.Message), e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="song"></param>
        /// <param name="deleteFromDrive"></param>
        public void RemoveSong(Song song, bool deleteFromDrive)
        {
            song.RemoveFromAllTags();
            ImportList.Remove(song);
            CurrentSongPlayList.RemoveSong(song);
            Songs.Remove(song);
            if (deleteFromDrive)
                File.Delete(song.FullPath);
        }
        #endregion

        #region ImportList delegation...
        public void AddIntoImport(List<string> filePaths) => CurrentImportList.AddIntoImport(filePaths);

        public void AddIntoImport(List<Song> songs) => CurrentImportList.AddIntoImport(songs);

        public void AssignTags(List<Song> songs, List<SongTag> tags, bool remove, bool overwrite) => CurrentImportList.AssignTags(songs, tags, remove, overwrite);

        public void ClearImport() => CurrentImportList.ClearImport();

        public void RemoveFromImport(List<Song> forRemoval)
        {
            CurrentImportList.RemoveFromImport(forRemoval);
            if (forRemoval.Contains(CurrentSongPlayList.previewSong))
                CurrentSongPlayList.Stop();
        }
        #endregion


        public int GetNextFreeTagID()
        {
            var id = -1;
            try
            {
                if (SongTags.Count > 0)
                {
                    foreach (var t in SongTags)
                        if (t.ID > id)
                            id = t.ID;
                    id++;
                }
                else
                    id = 0;
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Failed to retrieve next free tag ID. Error message:\n\n{0}", e.ToString()));
            }
            return id;
        }

        public void CreateTag(string name, string category)
        {
            try
            {
                if ((name.Length > 0) && (category.Length > 0))
                {
                    var id = GetNextFreeTagID();
                    var newTag = new SongTag() { ID = id, Name = name, Category = category };
                    SongTags.Add(newTag);
                }
                else
                    MessageBox.Show("Please, enter both name and category for new tag.");
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Could not create tag with name {0} and category {1}. Following error occured:\n\n{2}", name, category, e.ToString()));
            }
        }

        public void RemoveTag(SongTag tag)
        {
            try
            {
                if (tag != null)
                {
                    tag.RemoveFromSongs();
                    SongTags.Remove(tag);
                }
                else
                    MessageBox.Show("No tag was selected - nothing to remove.");
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Could not remove provided tag. Error message:\n\n{0}", e.ToString()));
            }
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
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Could not edit provided tag. Error message:\n\n{0}", e.ToString()));
            }
        }
    }
}