using System.Collections.Generic;
using System.Text;

namespace MusicTagger2.Core
{
    class SongTag
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }

        public HashSet<Song> songs = new HashSet<Song>();

        public void CreateSong(string filePath)
        {
            var newSong = new Song(filePath);
            newSong.tags.Add(this);
            songs.Add(newSong);
        }

        public void AddSong(Song song)
        {
            if (!songs.Contains(song))
                songs.Add(song);

            song.AddTag(this);
        }

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
