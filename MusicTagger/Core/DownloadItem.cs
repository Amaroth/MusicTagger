using System;
using System.ComponentModel;
using System.IO;
using MediaToolkit;
using MediaToolkit.Model;
using VideoLibrary;

namespace MusicTagger.Core
{
    class DownloadItem : INotifyPropertyChanged
    {
        private string url;
        public string URL
        {
            get { return this.url; }
            set
            {
                if (this.url != value)
                {
                    this.url = value;
                    NotifyPropertyChanged("URL");
                }
            }
        }
        private string state;
        public string State
        {
            get { return this.state; }
            set
            {
                if (this.state != value)
                {
                    this.state = value;
                    NotifyPropertyChanged("State");
                }
            }
        }
        private string filePath;
        public string FilePath
        {
            get { return this.filePath; }
            set
            {
                if (this.filePath != value)
                {
                    this.filePath = value;
                    NotifyPropertyChanged("FilePath");
                    NotifyPropertyChanged("FileName");
                }
            }
        }
        public string FileName => Path.GetFileName(FilePath);

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        public DownloadItem()
        {
            State = "Waiting";
        }

        public void Download()
        {
            var mp4Path = FilePath.Substring(0, FilePath.Length - 1) + "4";
            var youtube = YouTube.Default;
            var vid = youtube.GetVideo(URL);

            try
            {
                State = "Downloading";
                File.WriteAllBytes(mp4Path, vid.GetBytes());
            }
            catch (Exception e)
            {
                State = "Download failed";
                throw e;
            }

            using (var engine = new Engine())
            {
                try
                {
                    State = "Converting";
                    var mp4File = new MediaFile { Filename = mp4Path };
                    var mp3File = new MediaFile { Filename = FilePath };
                    engine.GetMetadata(mp4File);
                    engine.Convert(mp4File, mp3File);
                }
                catch (Exception e)
                {
                    State = "Conversion failed";
                    throw e;
                }
            }

            try
            {
                State = "Deleting MP4";
                File.Delete(mp4Path);
            }
            catch (Exception e)
            {
                State = "MP4 cleanup failed";
                throw e;
            }

            State = "Done";
        }
    }
}
