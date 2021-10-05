#region

using HyPlayer.Classes;
using HyPlayer.HyPlayControl;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

#endregion

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace HyPlayer.Pages
{
    /// <summary>
    ///     可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class LocalMusicPage : Page
    {
        private bool isLoading = false;
        private readonly List<HyPlayItem> _localHyItems = new List<HyPlayItem>();
        private readonly ObservableCollection<HyPlayItem> _visualHyItems = new ObservableCollection<HyPlayItem>();
        private Task _fileScanTask;
        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        public LocalMusicPage()
        {
            InitializeComponent();
        }

        private void SyncItemToView()
        {
            if (isLoading)
            {
                for (int i = _visualHyItems.Count; i < _localHyItems.Count; i++)
                {
                    _visualHyItems.Add(_localHyItems[i]);
                }
            }
        }


        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            HyPlayList.OnTimerTicked -= SyncItemToView;
            _visualHyItems.Clear();
            _localHyItems.Clear();
            _fileScanTask?.Dispose();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            DownloadPageFrame.Navigate(typeof(DownloadPage));
        }

        private void Playall_Click(object sender, RoutedEventArgs e)
        {
            HyPlayList.RemoveAllSong();
            HyPlayList.List.AddRange(_localHyItems); // 可能可以更改下
            HyPlayList.SongAppendDone();
            HyPlayList.SongMoveTo(0);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            ListBoxLocalMusicContainer.SelectionChanged -= ListBoxLocalMusicContainer_SelectionChanged;
            _localHyItems.Clear();
            _visualHyItems.Clear();
            LoadLocalMusic();
            ListBoxLocalMusicContainer.SelectionChanged += ListBoxLocalMusicContainer_SelectionChanged;
        }

        private async void LoadLocalMusic()
        {
            if (_fileScanTask != null)
            {
                if (_fileScanTask.IsCompleted)
                    _fileScanTask.Dispose();
                else
                    tokenSource.Cancel();
            }

            var tmp = await KnownFolders.MusicLibrary.GetItemsAsync();

            _fileScanTask = Task.Run((() =>
            {
                Common.ShowTeachingTip("正在扫描本地文件", "可能会耗时一段时间");
                isLoading = true;
                _localHyItems.Clear();
                foreach (var item in tmp)
                {
                    if (item.Path.Contains("iTunes")) continue;
                    StorageApplicationPermissions.FutureAccessList.AddOrReplace(Guid.NewGuid().ToString("N"),
                        item);
                    /*
                    if (item.IsOfType(StorageItemTypes.Folder))
                    {
                        await FastFileEnum.FindFilesWithWin32(item.Path, fileList);
                    }
                    else
                    {
                        fileList.Add(item.Path);
                    }
                    */
                    GetSubFiles(item);
                }

                //Common.ShowTeachingTip("文件扫描完成", "共" + fileList.Count + " 个");
                Common.ShowTeachingTip("文件扫描完成", "共" + _localHyItems.Count + " 个");
                isLoading = false;
            }), tokenSource.Token);
        }

        private async void GetSubFiles(IStorageItem item)
        {
            try
            {
                if (item is StorageFile)
                {
                    var file = item as StorageFile;
                    var hyPlayItem = await HyPlayList.LoadStorageFile(file);
                    _localHyItems.Add(hyPlayItem);
                }
                else if (item is StorageFolder)
                {
                    foreach (var subitems in await ((StorageFolder)item).GetItemsAsync())
                        GetSubFiles(subitems);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void ListBoxLocalMusicContainer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            HyPlayList.RemoveAllSong();
            HyPlayList.List.AddRange(_localHyItems);
            HyPlayList.SongAppendDone();
            HyPlayList.SongMoveTo(ListBoxLocalMusicContainer.SelectedIndex);
        }

        private async void UploadCloud_Click(object sender, RoutedEventArgs e)
        {
            var sf = await StorageFile.GetFileFromPathAsync(((HyPlayItem)((Button)sender).Tag).PlayItem.Url);
            await CloudUpload.UploadMusic(sf);
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((Pivot)sender).SelectedIndex == 1)
            {
                _visualHyItems.Clear();
                LoadLocalMusic();
                HyPlayList.OnTimerTicked += SyncItemToView;
            }
            else
            {
                HyPlayList.OnTimerTicked -= SyncItemToView;
            }
        }
    }
}