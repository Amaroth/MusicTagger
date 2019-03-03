using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml;

namespace MusicTagger2.Core
{
    class SettingsWriter
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
            try
            {
                CreateDirectory(filePath);
                CreateHeader();
                WriteTagData(songTags);
                WriteSongData(songs);
                WriteSettingsToFile(filePath);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error occured while attempting to save current settings to provided file. Error message:\n" + e.ToString());
            }
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
                throw new Exception("Could not create directory path to provided output file.", e);
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
                throw new Exception("Could not prepare header and/or root of XML file.", e);
            }
        }

        /// <summary>
        /// Adds song tag data into output XML.
        /// </summary>
        /// <param name="songTags"></param>
        private void WriteTagData(ObservableCollection<SongTag> songTags)
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
                throw new Exception("Could not save tags into output file.", e);
            }
        }

        /// <summary>
        /// Adds song data into output XML.
        /// </summary>
        /// <param name="songs"></param>
        private void WriteSongData(ObservableCollection<Song> songs)
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
                            tag.SetAttribute("ID", t.ToString());
                            newSong.AppendChild(tag);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Could not save songs into output fule.", e);
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
            catch (Exception e) { throw new Exception("Could not save output XML as file.", e); }
        }
    }
}
