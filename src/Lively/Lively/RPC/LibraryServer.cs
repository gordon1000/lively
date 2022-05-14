using Grpc.Core;
using Lively.Core.Library;
using Lively.Grpc.Common.Proto.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.RPC
{
    internal class LibraryServer : LibraryService.LibraryServiceBase
    {
        private readonly ILibraryManager _libraryManager;

        public LibraryServer(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        public override async Task<LibraryModel> AddWallpaperFile(AddWallpaperFileRequest request, ServerCallContext context)
        {
            return await base.AddWallpaperFile(request, context);
        }
    }
}
