using AutoMapper;
using GRPC = Lively.Grpc.Common.Proto.Library;
using LM = Lively.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper.Extensions.EnumMapping;

namespace Lively.Mappers
{
    public class GrpcModelsProfile : Profile
    {
        public GrpcModelsProfile()
        {
            CreateMap<GRPC.LibraryModel, LM.LibraryModel>();
            CreateMap<GRPC.LivelyInfoModel, LM.LivelyInfoModel>();
            CreateMap<GRPC.LibraryModel.Types.LibraryItemType, LM.LibraryItemType>()
                .ConvertUsingEnumMapping(opt => opt
                    .MapByValue()
                    .MapValue(GRPC.LibraryModel.Types.LibraryItemType.Cmdimport, LM.LibraryItemType.cmdImport)
                    .MapValue(GRPC.LibraryModel.Types.LibraryItemType.Edit, LM.LibraryItemType.edit)
                    .MapValue(GRPC.LibraryModel.Types.LibraryItemType.Multiimport, LM.LibraryItemType.multiImport)
                    .MapValue(GRPC.LibraryModel.Types.LibraryItemType.Processing, LM.LibraryItemType.processing)
                    .MapValue(GRPC.LibraryModel.Types.LibraryItemType.Ready, LM.LibraryItemType.ready)
                    .MapValue(GRPC.LibraryModel.Types.LibraryItemType.Unknown, LM.LibraryItemType.ready)
                );

            CreateMap<LM.LibraryModel, GRPC.LibraryModel>()
                .ForMember(destination => destination.ImagePath, opt => opt.NullSubstitute(string.Empty))
                .ForMember(destination => destination.PreviewClipPath, opt => opt.NullSubstitute(string.Empty))
                .ForMember(destination => destination.SrcWebsite, opt => opt.MapFrom(src => src.SrcWebsite != null ? src.SrcWebsite.ToString() : string.Empty))
                .ForMember(destination => destination.ThumbnailPath, opt => opt.NullSubstitute(string.Empty))
                ;
            CreateMap<LM.LivelyInfoModel, GRPC.LivelyInfoModel>()
                .ForMember(destination => destination.Author, opt => opt.NullSubstitute(string.Empty))
                ;
            CreateMap<LM.LibraryItemType, GRPC.LibraryModel.Types.LibraryItemType>()
                .ConvertUsingEnumMapping(opt => opt
                    .MapByValue()
                    .MapValue(LM.LibraryItemType.cmdImport, GRPC.LibraryModel.Types.LibraryItemType.Cmdimport)
                    .MapValue(LM.LibraryItemType.edit, GRPC.LibraryModel.Types.LibraryItemType.Edit)
                    .MapValue(LM.LibraryItemType.multiImport, GRPC.LibraryModel.Types.LibraryItemType.Multiimport)
                    .MapValue(LM.LibraryItemType.processing, GRPC.LibraryModel.Types.LibraryItemType.Processing)
                    .MapValue(LM.LibraryItemType.ready, GRPC.LibraryModel.Types.LibraryItemType.Ready)
                );
        }
    }
}
