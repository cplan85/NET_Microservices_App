using System;
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

 [ApiController]
 [Route("api/[controller]")]
public class AuctionsController: ControllerBase
{
    private readonly AuctionDbContext _context;
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndpoint;

    public AuctionsController(AuctionDbContext context, IMapper mapper, 
    IPublishEndpoint publishEndpoint)

        {
        _publishEndpoint = publishEndpoint;
        _context = context;
        _mapper = mapper;
    }

     [HttpGet] //api/activiteis/{guid}
        public async Task<ActionResult<List<AuctionDto>>> GetAllAuction(string date) 
        {

              var query = _context.Auctions.OrderBy(x => x.Item.Make).AsQueryable();

              if (!string.IsNullOrEmpty(date))
              {
                query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
              }


              return await query.ProjectTo<AuctionDto>(_mapper.ConfigurationProvider).ToListAsync();

        }

        [HttpGet("{id}")] //api/activiteis/{guid}
        public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id) 
        {
              var auction = await _context.Auctions
              .Include(x => x.Item)
              .FirstOrDefaultAsync(x => x.Id == id);

              if (auction == null) return NotFound();

              return _mapper.Map<AuctionDto>(auction);
        }

      
    [HttpPost] 
        public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto createAuctionDto)  
        {

            var auction = _mapper.Map<Auction>(createAuctionDto);

            //TODO: add current user as seller
            auction.Seller = "test";

                
            _context.Auctions.Add(auction);
                

                var newAuction = _mapper.Map<AuctionDto>(auction);
                
                await _publishEndpoint.Publish(_mapper.Map<AuctionCreated>(newAuction));

                var result = await _context.SaveChangesAsync() > 0;

                if (!result) {
                    return BadRequest("Could not save changes to the DB");
                }

                return CreatedAtAction(nameof(GetAuctionById), new{auction.Id}, _mapper.Map<AuctionDto>(auction));

        }

         [HttpPut("{id}")]
        public async Task<IActionResult> EditAuction(Guid id, UpdateAuctionDto updateAuctionDto)
        {
          // return HandleResult( await Mediator.Send(new Edit.Command{Activity = activity}) );

             var auction = await _context.Auctions.Include(x => x.Item).FirstOrDefaultAsync(x => x.Id == id);

                if (auction == null) return NotFound();

                //TODO: Check seller is equal to seller name

                auction.Item.Make  = updateAuctionDto.Make ?? auction.Item.Make;
                auction.Item.Model  = updateAuctionDto.Model ?? auction.Item.Model;
                auction.Item.Color  = updateAuctionDto.Color ?? auction.Item.Color;
                auction.Item.Mileage  = updateAuctionDto.Mileage ?? auction.Item.Mileage;
                auction.Item.Year  = updateAuctionDto.Year ?? auction.Item.Year;


        await _publishEndpoint.Publish(_mapper.Map<AuctionUpdated>(auction));

        var result =  await _context.SaveChangesAsync() > 0;

                 if (!result) {
                    return BadRequest("Problem saving changes");
                }

               return Ok();
        }
        
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAuction(Guid id)
        {
                 var auction = await _context.Auctions.FindAsync(id);

                    if (auction == null) return NotFound();

                    //TODO: Check seller is equal to seller name

                _context.Auctions.Remove(auction);

        await _publishEndpoint.Publish<AuctionDeleted>(new {Id = auction.Id.ToString()});

        var result =  await _context.SaveChangesAsync() > 0;

               if(!result) return BadRequest("Failed to delete the Auction");

               return Ok(id);
        }
         
}
