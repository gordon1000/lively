using Lively.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.Core.Library
{
    public interface ILibraryManager
    {
        ObservableCollection<ILibraryModel> LibraryItems { get; }

        Task<ILibraryModel> AddWallpaper(string folderPath, bool processing = false);

    }
}
