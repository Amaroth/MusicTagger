using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace MusicTagger.Core
{
    class DownloadList
    {
        public ObservableCollection<DownloadItem> DownloadItems = new ObservableCollection<DownloadItem>();

        public void AddToDownloadList(string videoURL, string filePath)
        {
            bool conflict = false;
            foreach (var i in DownloadItems)
                if (i.URL == videoURL || i.FilePath.ToLower() == filePath.ToLower())
                    conflict = true;

            if (!conflict)
                DownloadItems.Add(new DownloadItem() { URL = videoURL, FilePath = filePath });
            else
                MessageBox.Show("Either the URL or the file path seems to already exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void RemoveFromDownloadList(List<DownloadItem> forRemoval)
        {
            var removeList = new List<DownloadItem>();
            foreach (var s in forRemoval)
                removeList.Add(s);

            foreach (var s in removeList)
                DownloadItems.Remove(s);
        }
    }
}
