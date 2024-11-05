using System;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using Microsoft.Extensions.Options;

namespace AuctionService.RequestHelpers;

public class MappingProfiles : Profile
{
    public MappingProfiles() {
        CreateMap<Auction, AuctionDto>().IncludeMembers(x=> x.Item);
        CreateMap<Item, AuctionDto>();
        CreateMap<CreateAuctionDto, Auction>()
        .ForMember(d => d.Item, options => options.MapFrom(s => s));
        CreateMap<CreateAuctionDto, Item>();
    }
}
