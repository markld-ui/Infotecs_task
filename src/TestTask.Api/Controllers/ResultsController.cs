using Microsoft.AspNetCore.Mvc;
using TestTask.Core.DTOs;
using TestTask.Core.Interfaces;

namespace TestTask.Api.Controllers;

[ApiController]
[Route("api/results")]
public class ResultsController : ControllerBase
{
    private readonly IResultsService _resultsService;

    public ResultsController(IResultsService resultsService)
    {
        _resultsService = resultsService;
    }

    /// <summary>
    /// Метод 2: возвращает строки результатов, соответствующие заданным фильтрам
    /// (имя файла, диапазон дат начала, диапазон средних значений, диапазон среднего времени выполнения).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ResultDto>>> GetFiltered(
        [FromQuery] ResultFilterDto filter, CancellationToken ct)
    {
        var results = await _resultsService.GetFilteredAsync(filter, ct);
        return Ok(results);
    }
}
