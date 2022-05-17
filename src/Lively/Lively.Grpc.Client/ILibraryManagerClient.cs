using Lively.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.Grpc.Client
{
    public interface ILibraryManagerClient : IDisposable
    {
        Task<ILibraryModel> AddWallpaper(string folderPath, bool processing = false);
        Task<ILibraryModel> AddWallpaperFile(string filePath);
        Task<ILibraryModel> AddWallpaperLink(string url);
        Task AddWallpapers(List<string> files, CancellationToken cancellationToken, IProgress<int> progress);

        Task DeleteWallpaper(ILibraryModel obj);

        Task ExportWallpaper(ILibraryModel libraryItem, string saveFile);

        Task ShowWallpaperOnDisk(ILibraryModel libraryItem);

        event EventHandler<List<ILibraryModel>> LibraryCollectionChanged;
    }
}