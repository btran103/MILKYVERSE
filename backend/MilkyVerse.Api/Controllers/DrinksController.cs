using Microsoft.AspNetCore.Mvc;
using MilkyVerse.Api.DTOs;
using MilkyVerse.Api.Services;

namespace MilkyVerse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DrinksController : ControllerBase
{
    private readonly IDIYService _diyService;
    private readonly IProductService _productService;

    public DrinksController(IDIYService diyService, IProductService productService)
    {
        _diyService = diyService;
        _productService = productService;
    }

    [HttpGet("toppings")]
    public async Task<IActionResult> GetToppings()
    {
        var result = await _productService.GetToppingsAsync();
        return Ok(result);
    }

    [HttpGet("options")]
    public async Task<IActionResult> GetOptions()
    {
        var result = await _diyService.GetCatalogAsync();
        return Ok(result);
    }

    [HttpPost("quote")]
    public async Task<IActionResult> Quote([FromBody] DIYQuoteRequest request)
    {
        var result = await _diyService.QuoteDrinkAsync(request);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
