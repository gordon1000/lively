using Lively.Common;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Archive;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.Storage;
using Lively.Helpers;
using Lively.Models;
using Lively.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.Core.Library
{
    public class LibraryManager : ILibraryManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly SemaphoreSlim semaphoreSlimInstallLock = new SemaphoreSlim(1, 1);
        private readonly IUserSettingsService userSettings;
        // TODO: GK - should not depend on IDesktopCore
        private readonly IDesktopCore desktopCore;
        private bool disposedValue;

        private ObservableCollection<ILibraryModel> _libraryItems = new ObservableCollection<ILibraryModel>();
        public ObservableCollection<ILibraryModel> LibraryItems
        {
            get { return _libraryItems; }
            set
            {
                if (value != _libraryItems)
                {
                    _libraryItems = value;
                    //OnPropertyChanged();
                }
            }
        }

        public LibraryManager(IUserSettingsService userSettings, IDesktopCore desktopCore)
        {
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
        }

        public async Task<ILibraryModel> AddWallpaper(string folderPath, bool processing = false)
        {
            try
            {
                var libItem = await ScanWallpaperFolder(folderPath);
                var index = processing ? 0 : BinarySearch(_libraryItems, libItem.Title);
                //libItem.DataType = processing ? LibraryItemType.processing : LibraryItemType.ready;
                _libraryItems.Insert(index, libItem);
                return libItem;
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            return null;
        }

        private async Task<ILibraryModel> ScanWallpaperFolder(string folderPath)
        {
            if (File.Exists(Path.Combine(folderPath, "LivelyInfo.json")))
            {
                LivelyInfoModel info = JsonStorage<LivelyInfoModel>.LoadData(Path.Combine(folderPath, "LivelyInfo.json"));
                return info != null ?
                    new LibraryModel(info, folderPath, LibraryItemType.ready, userSettings.Settings.UIMode != LivelyGUIState.lite) :
                    throw new Exception("Corrupted wallpaper metadata");
            }
            throw new Exception("Wallpaper not found.");
        }


        private int BinarySearch(ObservableCollection<ILibraryModel> item, string x)
        {
            if (x is null)
            {
                throw new ArgumentNullException(nameof(x));
            }

            int l = 0, r = item.Count - 1, m, res;
            while (l <= r)
            {
                m = (l + r) / 2;

                res = String.Compare(x, item[m].Title);

                if (res == 0)
                    return m;

                if (res > 0)
                    l = m + 1;

                else
                    r = m - 1;
            }
            return l;//(l - 1);
        }

        public async Task<ILibraryModel> AddWallpaperFile(string filePath)
        {
            WallpaperType type;
            if ((type = FileFilter.GetLivelyFileType(filePath)) != (WallpaperType)(-1))
            {
                if (type == (WallpaperType)100)
                {
                    //lively .zip is not a wallpaper type.
                    if (ZipExtract.IsLivelyZip(filePath))
                    {
                        await semaphoreSlimInstallLock.WaitAsync();
                        string installDir = null;
                        try
                        {
                            installDir = Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallDir, Path.GetRandomFileName());
                            await Task.Run(() => ZipExtract.ZipExtractFile(filePath, installDir, false));
                            return await AddWallpaper(installDir);
                        }
                        catch (Exception)
                        {
                            try
                            {
                                Directory.Delete(installDir, true);
                            }
                            catch { }
                        }
                        finally
                        {
                            semaphoreSlimInstallLock.Release();
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Not Lively .zip");
                    }
                }
                else
                {
                    var dir = Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallTempDir, Path.GetRandomFileName());
                    Directory.CreateDirectory(dir);
                    var data = new LivelyInfoModel()
                    {
                        Title = Path.GetFileNameWithoutExtension(filePath),
                        Type = type,
                        IsAbsolutePath = true,
                        FileName = filePath,
                        Contact = string.Empty,
                        Preview = string.Empty,
                        Thumbnail = string.Empty,
                        Arguments = string.Empty,
                        AppVersion = string.Empty,
                        Author = string.Empty,
                        Desc = string.Empty,
                        License = string.Empty
                    };

                    //TODO generate livelyproperty for gif etc..
                    JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(dir, "LivelyInfo.json"), data);
                    return await AddWallpaper(dir, true);
                }
            }
            throw new InvalidOperationException($"Unsupported file ({Path.GetExtension(filePath)})");
        }

        public async Task<ILibraryModel> AddWallpaperLink(string url)
        {
            var dir = Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallTempDir, Path.GetRandomFileName());
            Directory.CreateDirectory(dir);
            var data = new LivelyInfoModel()
            {
                Title = LinkHandler.GetLastSegmentUrl(url),
                Type = (userSettings.Settings.AutoDetectOnlineStreams && StreamUtil.IsSupportedStream(url)) ? WallpaperType.videostream : WallpaperType.url,
                IsAbsolutePath = true,
                FileName = url,
                Contact = url,
                Preview = string.Empty,
                Thumbnail = string.Empty,
                Arguments = string.Empty,
                AppVersion = string.Empty,
                Author = string.Empty,
                Desc = string.Empty,
                License = string.Empty
            };

            //TODO generate livelyproperty for gif etc..
            JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(dir, "LivelyInfo.json"), data);
            return await AddWallpaper(dir, true);
        }

        //public ILibraryModel AddWallpaperLink(Uri uri) => AddWallpaperLink(uri.OriginalString);

        public async Task AddWallpapers(List<string> files, CancellationToken cancellationToken, IProgress<int> progress)
        {
            //display all Lively zip files first since its the first items to get processed.
            files = files.OrderByDescending(x => Path.GetExtension(x).Equals(".zip", StringComparison.OrdinalIgnoreCase)).ToList();
            var tcs = new TaskCompletionSource<bool>();
            desktopCore.WallpaperChanged += WallpaperChanged;
            void WallpaperChanged(object sender, EventArgs e)
            {
                tcs.SetResult(true);
            }

            for (int i = 0; i < files.Count; i++)
            {
                try
                {
                    var wallpaper = await AddWallpaperFile(files[i]);
                    //Skipping .zip files already processed..
                    if (wallpaper.DataType == LibraryItemType.processing)
                    {
                        wallpaper.DataType = LibraryItemType.multiImport;
                        desktopCore.SetWallpaper(wallpaper, userSettings.Settings.SelectedDisplay);
                        await tcs.Task;
                        tcs = new TaskCompletionSource<bool>();
                    }

                    if (cancellationToken.IsCancellationRequested)
                        break;
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
                progress.Report(100 * (i + 1) / files.Count);
            }

            desktopCore.WallpaperChanged -= WallpaperChanged;
        }


        /// <summary>
        /// Stop if running and delete wallpaper from library and disk.<br>
        /// (To be called from UI thread.)</br>
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task DeleteWallpaper(ILibraryModel obj)
        {
            //close if running.
            desktopCore.CloseWallpaper(obj, true);
            //delete wp folder.      
            var success = await FileOperations.DeleteDirectoryAsync(obj.LivelyInfoFolderPath, 1000, 4000);

            if (success)
            {
                //remove from library.
                LibraryItems.Remove((LibraryModel)obj);
                try
                {
                    if (string.IsNullOrEmpty(obj.LivelyInfoFolderPath))
                        return;

                    //Delete LivelyProperties.json backup folder.
                    string[] wpdataDir = Directory.GetDirectories(Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperSettingsDir));
                    var wpFolderName = new DirectoryInfo(obj.LivelyInfoFolderPath).Name;
                    for (int i = 0; i < wpdataDir.Length; i++)
                    {
                        var item = new DirectoryInfo(wpdataDir[i]).Name;
                        if (wpFolderName.Equals(item, StringComparison.Ordinal))
                        {
                            _ = FileOperations.DeleteDirectoryAsync(wpdataDir[i], 1000, 4000);
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                }
            }
        }

        public Task ExportWallpaper(ILibraryModel libraryItem, string saveFile)
        {
            return Task.Run(async () =>
            {
                //title ending with '.' can have diff extension (example: parallax.js) or
                //user made a custom filename with diff extension.
                if (Path.GetExtension(saveFile) != ".zip")
                {
                    saveFile += ".zip";
                }

                if (libraryItem.LivelyInfo.Type == WallpaperType.videostream
                    || libraryItem.LivelyInfo.Type == WallpaperType.url)
                {
                    //no wallpaper file on disk, only wallpaper metadata.
                    var tmpDir = Path.Combine(Constants.CommonPaths.TempDir, Path.GetRandomFileName());
                    try
                    {
                        Directory.CreateDirectory(tmpDir);
                        LivelyInfoModel info = new LivelyInfoModel(libraryItem.LivelyInfo)
                        {
                            IsAbsolutePath = false
                        };

                        //..changing absolute filepaths to relative, FileName is not modified since its url.
                        if (libraryItem.ThumbnailPath != null)
                        {
                            File.Copy(libraryItem.ThumbnailPath, Path.Combine(tmpDir, Path.GetFileName(libraryItem.ThumbnailPath)));
                            info.Thumbnail = Path.GetFileName(libraryItem.ThumbnailPath);
                        }
                        if (libraryItem.PreviewClipPath != null)
                        {
                            File.Copy(libraryItem.PreviewClipPath, Path.Combine(tmpDir, Path.GetFileName(libraryItem.PreviewClipPath)));
                            info.Preview = Path.GetFileName(libraryItem.PreviewClipPath);
                        }

                        JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(tmpDir, "LivelyInfo.json"), info);
                        ZipCreate.CreateZip(saveFile, new List<string>() { tmpDir });
                    }
                    finally
                    {
                        await FileOperations.DeleteDirectoryAsync(tmpDir, 1000, 2000);
                    }
                }
                else if (libraryItem.LivelyInfo.IsAbsolutePath)
                {
                    //livelyinfo.json only contains the absolute filepath of the file; file is in different location.
                    var tmpDir = Path.Combine(Constants.CommonPaths.TempDir, Path.GetRandomFileName());
                    try
                    {
                        Directory.CreateDirectory(tmpDir);
                        List<string> files = new List<string>();
                        if (libraryItem.LivelyInfo.Type == WallpaperType.video ||
                        libraryItem.LivelyInfo.Type == WallpaperType.gif ||
                        libraryItem.LivelyInfo.Type == WallpaperType.picture)
                        {
                            files.Add(libraryItem.FilePath);
                        }
                        else
                        {
                            files.AddRange(Directory.GetFiles(Directory.GetParent(libraryItem.FilePath).ToString(), "*.*", SearchOption.AllDirectories));
                        }

                        LivelyInfoModel info = new LivelyInfoModel(libraryItem.LivelyInfo)
                        {
                            IsAbsolutePath = false
                        };
                        info.FileName = Path.GetFileName(info.FileName);

                        //..changing absolute filepaths to relative.
                        if (libraryItem.ThumbnailPath != null)
                        {
                            File.Copy(libraryItem.ThumbnailPath, Path.Combine(tmpDir, Path.GetFileName(libraryItem.ThumbnailPath)));
                            info.Thumbnail = Path.GetFileName(libraryItem.ThumbnailPath);
                        }
                        if (libraryItem.PreviewClipPath != null)
                        {
                            File.Copy(libraryItem.PreviewClipPath, Path.Combine(tmpDir, Path.GetFileName(libraryItem.PreviewClipPath)));
                            info.Preview = Path.GetFileName(libraryItem.PreviewClipPath);
                        }

                        JsonStorage<LivelyInfoModel>.StoreData(Path.Combine(tmpDir, "LivelyInfo.json"), info);
                        List<string> metaData = new List<string>();
                        metaData.AddRange(Directory.GetFiles(tmpDir, "*.*", SearchOption.TopDirectoryOnly));
                        var fileData = new List<ZipCreate.FileData>
                            {
                                new ZipCreate.FileData() { Files = metaData, ParentDirectory = tmpDir },
                                new ZipCreate.FileData() { Files = files, ParentDirectory = Directory.GetParent(libraryItem.FilePath).ToString() }
                            };

                        ZipCreate.CreateZip(saveFile, fileData);
                    }
                    finally
                    {
                        await FileOperations.DeleteDirectoryAsync(tmpDir, 1000, 2000);
                    }
                }
                else
                {
                    //installed lively wallpaper.
                    ZipCreate.CreateZip(saveFile, new List<string>() { Path.GetDirectoryName(libraryItem.FilePath) });
                }
                FileOperations.OpenFolder(saveFile);
            });
        }

        public async Task ShowWallpaperOnDisk(ILibraryModel libraryItem)
        {
            string folderPath =
                libraryItem.LivelyInfo.Type == WallpaperType.url || libraryItem.LivelyInfo.Type == WallpaperType.videostream
                ? libraryItem.LivelyInfoFolderPath : libraryItem.FilePath;
            DesktopBridgeUtil.OpenFolder(folderPath);
        }


        //public void SortWallpaper(ILibraryModel obj) => libraryVm.SortWallpaper((LibraryModel)obj);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    semaphoreSlimInstallLock?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~LibraryUtil()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
