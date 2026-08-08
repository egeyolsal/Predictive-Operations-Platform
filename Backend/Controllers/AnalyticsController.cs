using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskInventoryApi.Dtos;
using TaskInventoryApi.Services;

namespace TaskInventoryApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IStockPredictionService _stockPredictionService;

    public AnalyticsController(IStockPredictionService stockPredictionService)
    {
        _stockPredictionService = stockPredictionService;
    }

    [HttpGet("stock-predictions")]
    public async Task<ActionResult<IEnumerable<StockPredictionDto>>> GetStockPredictions()
    {
        var predictions = await _stockPredictionService.CalculateAllPredictionsAsync();
        return Ok(predictions);
    }
}
