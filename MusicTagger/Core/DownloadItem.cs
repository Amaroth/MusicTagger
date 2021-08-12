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
        public enum DownloadState { Waiting, Downloading, DownloadFailed, Converting, ConversionFailed, DeletingMP4, MP4CleanupFailed, Done }
        // URL of the YT video for the file to be downloaded from.
        private string _url;
        public string URL
        {
            get => _url;
            set
            {
                if (_url != value)
                {
                    _url = value;
                    NotifyPropertyChanged("URL");
                }
            }
        }
        // Current state the process is in.
        private DownloadState _state;
        public DownloadState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    NotifyPropertyChanged("State");
                }
            }
        }
        // Desired file path of the resulting mp3.
        private string _filePath;
        public string FilePath
        {
            get => _filePath;
            set
            {
                if (_filePath != value)
                {
                    _filePath = value;
                    NotifyPropertyChanged("FilePath");
                    NotifyPropertyChanged("FileName");
                }
            }
        }
        // Desired file name of the resulting mp3, derived from the file path.
        public string FileName => Path.GetFileName(FilePath);

        // Observable properties event handling.
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        public DownloadItem()
        {
            State = DownloadState.Waiting;
        }

        /// <summary>
        /// Downloads an MP4 file from the URL path and then converts it to an MP4, while changing State on the way.
        /// </summary>
        public void Download()
        {
            if (State == DownloadState.Done)
                return;

            if (Path.GetExtension(FilePath).ToLower() != ".mp3")
                FilePath = Path.GetDirectoryName(FilePath) + "\\" + Path.GetFileNameWithoutExtension(FileName) + ".mp3";
            var mp4Path = FilePath.Substring(0, FilePath.Length - 1) + "4";
            var youtube = YouTube.Default;
            var vid = youtube.GetVideo(URL);

            try
            {
                State = DownloadState.Downloading;
                File.WriteAllBytes(mp4Path, vid.GetBytes());
            }
            catch (Exception e)
            {
                State = DownloadState.DownloadFailed;
                throw e;
            }

            using (var engine = new Engine())
            {
                try
                {
                    State = DownloadState.Converting;
                    var mp4File = new MediaFile { Filename = mp4Path };
                    var mp3File = new MediaFile { Filename = FilePath };
                    engine.GetMetadata(mp4File);
                    engine.Convert(mp4File, mp3File);
                }
                catch (Exception e)
                {
                    State = DownloadState.ConversionFailed;
                    throw e;
                }
            }

            try
            {
                State = DownloadState.DeletingMP4;
                File.Delete(mp4Path);
            }
            catch (Exception e)
            {
                State = DownloadState.MP4CleanupFailed;
                throw e;
            }

            State = DownloadState.Done;
        }
    }
}
