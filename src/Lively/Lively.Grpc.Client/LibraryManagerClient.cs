using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Lively.Grpc.Common.Proto.Display;
using GrpcDotNetNamedPipes;
using Lively.Common;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System.Threading;
using Lively.Grpc.Common.Proto.Library;
using LM = Lively.Models;
using AutoMapper;
using static Lively.Grpc.Common.Proto.Library.LibraryModel.Types;

namespace Lively.Grpc.Client
{
    public class LibraryManagerClient : ILibraryManagerClient
    {
        public event EventHandler<List<LM.ILibraryModel>> LibraryCollectionChanged;

        private readonly LibraryService.LibraryServiceClient _client;
        private readonly SemaphoreSlim libraryCollectionLock = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource cancellationTokeneLibraryCollectionChanged;
        private readonly Task libraryCollectionChangedTask;
        private readonly IMapper _mapper;
        private bool disposedValue;

        public LibraryManagerClient(IMapper mapper)
        {
            _client = new LibraryService.LibraryServiceClient(new NamedPipeChannel(".", Constants.SingleInstance.GrpcPipeServerName));
            this._mapper = mapper;

            Task.Run(async () =>
            {
                //displayMonitors.AddRange(await GetScreens().ConfigureAwait(false));
                //VirtulScreenBounds = await GetVirtualScreenBounds().ConfigureAwait(false);
            }).Wait();

            cancellationTokeneLibraryCollectionChanged = new CancellationTokenSource();
            libraryCollectionChangedTask = Task.Run(() => SubscribeLibraryCollectionChangedStream(cancellationTokeneLibraryCollectionChanged.Token));
        }


        public Task<LM.ILibraryModel> AddWallpaper(string folderPath, bool processing = false)
        {
            throw new NotImplementedException();
        }

        public async Task<LM.ILibraryModel> AddWallpaperFile(string filePath)
        {
            var response = await _client.AddWallpaperFileAsync(new AddWallpaperFileRequest
            {
                FilePath = filePath
            });

            var livelyInfo = _mapper.Map<LM.LivelyInfoModel>(response.LivelyInfo);
            var itemType = _mapper.Map<LM.LibraryItemType>(response.DataType);
            var libraryItem = new LM.LibraryModel(livelyInfo, response.LivelyInfoFolderPath, itemType);
            return libraryItem;
        }

        public Task<LM.ILibraryModel> AddWallpaperLink(string url)
        {
            throw new NotImplementedException();
        }

        public Task AddWallpapers(List<string> files, CancellationToken cancellationToken, IProgress<int> progress)
        {
            throw new NotImplementedException();
        }

        public Task DeleteWallpaper(LM.ILibraryModel obj)
        {
            throw new NotImplementedException();
        }

        public Task ExportWallpaper(LM.ILibraryModel libraryItem, string saveFile)
        {
            throw new NotImplementedException();
        }

        public Task ShowWallpaperOnDisk(LM.ILibraryModel libraryItem)
        {
            throw new NotImplementedException();
        }


        private async Task SubscribeLibraryCollectionChangedStream(CancellationToken token)
        {
            try
            {
                using var call = _client.SubscribeLibraryCollectionChanged(new Empty());
                while (await call.ResponseStream.MoveNext(token))
                {
                    var resp = call.ResponseStream.Current;
                    var data = new List<LM.ILibraryModel>();
                    foreach (var item in resp.LibraryItem)
                    {
                        var livelyInfo = _mapper.Map<LM.LivelyInfoModel>(item.LivelyInfo);
                        var itemType = _mapper.Map<LM.LibraryItemType>(item.DataType);
                        var libraryItem = new LM.LibraryModel(livelyInfo, item.LivelyInfoFolderPath, itemType);
                        data.Add(libraryItem);
                    }
                    LibraryCollectionChanged?.Invoke(this, data);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

    #region  dispose

    protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    cancellationTokeneLibraryCollectionChanged?.Cancel();
                    libraryCollectionChangedTask?.Wait();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DisplayManagerClient()
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

        #endregion //dispose
    }
}
