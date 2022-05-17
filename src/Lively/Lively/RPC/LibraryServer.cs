using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Lively.Core.Library;
using Lively.Grpc.Common.Proto.Library;
using LM = Lively.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using System.Diagnostics;
using System.Collections.Specialized;

namespace Lively.RPC
{
    internal class LibraryServer : LibraryService.LibraryServiceBase
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IMapper _mapper;

        public LibraryServer(ILibraryManager libraryManager, IMapper mapper)
        {
            _libraryManager = libraryManager;
            this._mapper = mapper;
        }

        public override async Task<LibraryModel> AddWallpaperFile(AddWallpaperFileRequest request, ServerCallContext context)
        {
            var libraryItem = await _libraryManager.AddWallpaperFile(request.FilePath);
            var respons = _mapper.Map<LibraryModel>(libraryItem);
            return respons;
        }

        public override Task<LibraryModel> AddWallpaperLink(AddWallpaperLinkRequest request, ServerCallContext context)
        {
            return base.AddWallpaperLink(request, context);
        }

        public override Task AddWallpapers(AddWallpapersRequest request, IServerStreamWriter<Progress> responseStream, ServerCallContext context)
        {
            return base.AddWallpapers(request, responseStream, context);
        }

        public override Task<Empty> CancelAddWallpaper(Empty request, ServerCallContext context)
        {
            return base.CancelAddWallpaper(request, context);
        }

        public override Task<Empty> DeleteWallpaper(LibraryModel request, ServerCallContext context)
        {
            return base.DeleteWallpaper(request, context);
        }

        public override Task<Empty> ExportWallpaper(ExportWallpaperRequest request, ServerCallContext context)
        {
            return base.ExportWallpaper(request, context);
        }

        public override Task SearchWallpapers(SearchWallpapersRequest request, IServerStreamWriter<SearchWallpapersResponse> responseStream, ServerCallContext context)
        {
            return base.SearchWallpapers(request, responseStream, context);
        }

        public override async Task<Empty> ShowWallpaperOnDisk(LibraryModel request, ServerCallContext context)
        {
            var livelyInfo = _mapper.Map<LM.LivelyInfoModel>(request.LivelyInfo);
            var itemType = _mapper.Map<LM.LibraryItemType>(request.DataType);
            var libraryItem = new LM.LibraryModel(livelyInfo, request.LivelyInfoFolderPath, itemType);
            await _libraryManager.ShowWallpaperOnDisk(libraryItem);
            return new Empty();
        }

        public override async Task SubscribeLibraryCollectionChanged(Empty _, IServerStreamWriter<LibraryCollectionChangedResponse> responseStream, ServerCallContext context)
        {
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    var resp = new LibraryCollectionChangedResponse();
                    var tcs = new TaskCompletionSource<bool>();
                    _libraryManager.LibraryItems.CollectionChanged += LibraryItemsChanged;
                    void LibraryItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
                    {
                        foreach (var item in e.NewItems)
                        {
                            var libraryModel = _mapper.Map<LibraryModel>((LM.ILibraryModel)item);
                            resp.LibraryItem.Add(libraryModel);
                        }
                        _libraryManager.LibraryItems.CollectionChanged -= LibraryItemsChanged;
                        tcs.SetResult(true);
                    }
                    await tcs.Task;
                    await responseStream.WriteAsync(resp);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }
    }
}
