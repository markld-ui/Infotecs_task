using Microsoft.AspNetCore.Mvc;
using TestTask.Core.DTOs;
using TestTask.Core.Interfaces;

namespace TestTask.Api.Controllers;

[ApiController]
[Route("api/values")]
public class ValuesController : ControllerBase
{
    private readonly ICsvProcessingService _csvProcessingService;
    private readonly IResultsService _resultsService;

    public ValuesController(ICsvProcessingService csvProcessingService, IResultsService resultsService)
    {
        _csvProcessingService = csvProcessingService;
        _resultsService = resultsService;
    }

    /// <summary>
    /// Метод 1: загружает CSV-файл, проверяет и сохраняет его строки в Values,
    /// а также (пере)вычисляет сводную строку результатов для этого файла.
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(UploadResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<UploadResultDto>> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { errors = new[] { "No file was provided." } });

        await using var stream = file.OpenReadStream();
        var result = await _csvProcessingService.ProcessAsync(file.FileName, stream, ct);
        return Ok(result);
    }

    /// <summary>
    /// Метод 3: последние 10 значений для заданного файла,
    /// отсортированные по дате в порядке убывания (сначала самые свежие).
    /// </summary>
    [HttpGet("last")]
    [ProducesResponseType(typeof(IReadOnlyList<ValueDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ValueDto>>> GetLast(
        [FromQuery] string fileName, [FromQuery] int take = 10, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest(new { errors = new[] { "fileName query parameter is required." } });

        var result = await _resultsService.GetLastValuesAsync(fileName, take <= 0 ? 10 : take, ct);
        return Ok(result);
    }
}
