using Microsoft.EntityFrameworkCore;
using TestTask.Core.DTOs;
using TestTask.Core.Interfaces;

namespace TestTask.Infrastructure.Services;

public class ResultsService : IResultsService
{
    private readonly AppDbContext _db;

    public ResultsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ResultDto>> GetFilteredAsync(
        ResultFilterDto filter, 
        CancellationToken ct = default)
    {
        var query = _db.Results.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.FileName))
            query = query.Where(r => r.FileName.Contains(filter.FileName));

        if (filter.StartDateFrom is { } from)
            query = query.Where(r => r.StartDate >= from);

        if (filter.StartDateTo is { } to)
            query = query.Where(r => r.StartDate <= to);

        if (filter.AverageValueFrom is { } avgFrom)
            query = query.Where(r => r.AverageValue >= avgFrom);

        if (filter.AverageValueTo is { } avgTo)
            query = query.Where(r => r.AverageValue <= avgTo);

        if (filter.AverageExecutionTimeFrom is { } execFrom)
            query = query.Where(r => r.AverageExecutionTime >= execFrom);

        if (filter.AverageExecutionTimeTo is { } execTo)
            query = query.Where(r => r.AverageExecutionTime <= execTo);

        var results = await query
            .OrderByDescending(r => r.ProcessedAt)
            .Select(r => new ResultDto(
                r.FileName,
                r.DeltaTimeSeconds,
                r.StartDate,
                r.AverageExecutionTime,
                r.AverageValue,
                r.MedianValue,
                r.MaxValue,
                r.MinValue,
                r.RowCount,
                r.ProcessedAt))
            .ToListAsync(ct);

        return results;
    }

    public async Task<IReadOnlyList<ValueDto>> GetLastValuesAsync(
        string fileName, 
        int take = 10, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Array.Empty<ValueDto>();

        var values = await _db.Values.AsNoTracking()
            .Where(v => v.FileName == fileName)
            .OrderByDescending(v => v.Date)
            .Take(take)
            .Select(v => new ValueDto(v.Date, v.ExecutionTime, v.Value))
            .ToListAsync(ct);

        return values;
    }
}
