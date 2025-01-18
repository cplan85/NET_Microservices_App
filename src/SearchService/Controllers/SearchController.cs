using System;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;
using SearchService.Models;
using SearchService.RequestHelpers;

namespace SearchService.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController: ControllerBase
{
[HttpGet]
public async Task<ActionResult<List<Item>>> SearchItems([FromQuery]SearchParams searchParams)
{
    var query = DB.PagedSearch<Item, Item>();

    query.Sort(x => x.Ascending(a => a.Make));

    if(!string.IsNullOrEmpty(searchParams.SearchTerm)) {
        query.Match(Search.Full, searchParams.SearchTerm).SortByTextScore();
    }
    
    query = searchParams.OrderBy switch
    {
        "make" => query.Sort(x => x.Ascending(a => a.Make)),
        "new" => query.Sort(x => x.Descending(a => a.CreatedAt)),
        _ => query.Sort(x => x.Ascending(a => a.AuctionEnd)),
    };

     query = searchParams.FilterBy switch
{
    "finished" => query.Match(x => x.AuctionEnd < DateTime.UtcNow),
    "ending soon" => query.Match(x => x.AuctionEnd < DateTime.UtcNow.AddHours(6) && x.AuctionEnd > DateTime.UtcNow),
    _ => query.Match(x => x.AuctionEnd > DateTime.UtcNow),
};


    if (!string.IsNullOrEmpty(searchParams.Seller)) {
        query.Match(x => x.Seller == searchParams.Seller);
    }

     if (!string.IsNullOrEmpty(searchParams.Winner)) {
        query.Match(x => x.Winner == searchParams.Winner);
    }

    query.PageNumber(searchParams.PageNumber);
    query.PageSize(searchParams.PageSize);  

    var result = await query.ExecuteAsync();

    return Ok(new {
        results = result.Results,
        pageCount = result.PageCount,
        totalCount = result.TotalCount
    });
}
}

// 2nd row Consistent
// 3rd row: Inconsistent / when search service goes up then search service will be updated
// 4th row: You cant access search..when search goes up inconsistent
// 5th row: Inconsistent 
//DATA inconsistency is the major issue with microservices