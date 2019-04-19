using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Xml;

namespace MusicTagger.Core
{
    class ProjectWriter
    {
        XmlDocument outputDocument;
        XmlElement rootElement;

        /// <summary>
        /// Saves provided data to provided file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="songs"></param>
        /// <param name="songTags"></param>
        public void WriteSettings(string filePath, ObservableCollection<Song> songs, ObservableCollection<SongTag> songTags)
        {
            CreateDirectory(filePath);
            CreateHeader();
            WriteSongTagData(songTags);
            WriteSongData(songs, Path.GetDirectoryName(filePath));
            WriteSettingsToFile(filePath);
        }

        /// <summary>
        /// Creates directory to give path in case it doesn't exist.
        /// </summary>
        /// <param name="filePath"></param>
        private void CreateDirectory(string filePath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }
            catch (Exception e)
            {
                throw new Exception("Could not create directory path to provided output Project file.", e);
            }
        }

        /// <summary>
        /// Creates output XML's header.
        /// </summary>
        private void CreateHeader()
        {
            try
            {
                outputDocument = new XmlDocument();
                XmlDeclaration xmlDeclaration = outputDocument.CreateXmlDeclaration("1.0", "UTF-8", null);
                XmlElement root = outputDocument.DocumentElement;
                outputDocument.InsertBefore(xmlDeclaration, root);
                rootElement = outputDocument.CreateElement(string.Empty, "Config", string.Empty);
                outputDocument.AppendChild(rootElement);
            }
            catch (Exception e)
            {
                throw new Exception("Could not prepare header and/or root of Project file.", e);
            }
        }

        /// <summary>
        /// Adds song tag data into output XML.
        /// </summary>
        /// <param name="songTags"></param>
        private void WriteSongTagData(ObservableCollection<SongTag> songTags)
        {
            try
            {
                XmlElement tagsElement = outputDocument.CreateElement(string.Empty, "SongTags", string.Empty);
                rootElement.AppendChild(tagsElement);
                foreach (var t in songTags)
                {
                    XmlElement newTag = outputDocument.CreateElement(string.Empty, "SongTag", string.Empty);
                    newTag.SetAttribute("Name", t.Name);
                    newTag.SetAttribute("ID", t.ID.ToString());
                    newTag.SetAttribute("Category", t.Category);
                    tagsElement.AppendChild(newTag);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Could not save tags into Project file.", e);
            }
        }

        /// <summary>
        /// Adds song data into output XML.
        /// </summary>
        /// <param name="songs"></param>
        private void WriteSongData(ObservableCollection<Song> songs, string rootPath)
        {
            try
            {
                XmlElement songsElement = outputDocument.CreateElement(string.Empty, "Songs", string.Empty);
                rootElement.AppendChild(songsElement);
                foreach (var s in songs)
                {
                    if (s.WasTagged)
                    {
                        XmlElement newSong = outputDocument.CreateElement(string.Empty, "Song", string.Empty);
                        newSong.SetAttribute("FilePath", s.FullPath);
                        songsElement.AppendChild(newSong);

                        foreach (var t in s.tags)
                        {
                            XmlElement tag = outputDocument.CreateElement(string.Empty, "SongTag", string.Empty);
                            tag.SetAttribute("ID", t.ID.ToString());
                            newSong.AppendChild(tag);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Could not save songs into Project file.", e);
            }
        }

        /// <summary>
        /// Saves output XML to file.
        /// </summary>
        /// <param name="filePath"></param>
        private void WriteSettingsToFile(string filePath)
        {
            try
            {
                using (TextWriter tw = new StreamWriter(filePath, false, Encoding.UTF8))
                    outputDocument.Save(tw);
            }
            catch (Exception e)
            {
                throw new Exception("Could not save Project into file.", e);
            }
        }
    }
}
