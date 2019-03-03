using System.Collections.Generic;
using System.Text;

namespace MusicTagger2.Core
{
    class SongTag
    {
        // Unique ID of tag (handled on creation by Core).
        public int ID { get; set; }
        // Display name of tag.
        public string Name { get; set; }
        // Category name for grouping in tag list view.
        public string Category { get; set; }
        // All songs which have this tag assigned.
        public HashSet<Song> songs = new HashSet<Song>();

        /// <summary>
        /// Create a new song with this tag assigned.
        /// </summary>
        /// <param name="filePath"></param>
        public void CreateSong(string filePath)
        {
            var newSong = new Song(filePath);
            newSong.tags.Add(this);
            songs.Add(newSong);
        }

        /// <summary>
        /// Assign this tag to provided song.
        /// </summary>
        /// <param name="song"></param>
        public void AddSong(Song song)
        {
            if (!songs.Contains(song))
                songs.Add(song);

            song.AddTag(this);
        }

        /// <summary>
        /// Remove this tag from all its songs.
        /// </summary>
        public void RemoveFromSongs()
        {
            foreach (var s in songs)
                s.tags.Remove(this);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("ID: {0},\nName: {1},\nCategory: {2},\nSongNames: ", ID, Name, Category);
            if (songs.Count > 0)
            {
                foreach (var s in songs)
                {
                    sb.Append(s.SongName);
                    sb.Append(", ");
                }
                sb.Length -= 2;
            }
            return sb.ToString();
        }
    }
}
