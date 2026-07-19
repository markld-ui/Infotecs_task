using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TestTask.Core.DTOs;
using TestTask.Core.Entities;
using TestTask.Core.Exceptions;
using TestTask.Core.Interfaces;

namespace TestTask.Infrastructure.Services;

public class CsvProcessingService : ICsvProcessingService
{
    private const int MinRows = 1;
    private const int MaxRows = 10_000;
    private static readonly DateTimeOffset MinAllowedDate = new(
        2000, 1, 1, 
        0, 0, 0, 
        TimeSpan.Zero);

    // В части времени используется символ «-» вместо «:»
    // согласно спецификации: yyyy-MM-ddTHH-mm-ss.ffffZ
    private static readonly string[] DateFormats =
    {
        "yyyy-MM-dd'T'HH-mm-ss.ffff'Z'",
        "yyyy-MM-dd'T'HH-mm-ss.fff'Z'",
        "yyyy-MM-dd'T'HH-mm-ss.ff'Z'",
        "yyyy-MM-dd'T'HH-mm-ss.f'Z'",
        "yyyy-MM-dd'T'HH-mm-ss'Z'",
    };

    private readonly AppDbContext _db;

    public CsvProcessingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UploadResultDto> ProcessAsync(
        string fileName, 
        Stream csvContent, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new CsvValidationException("File name is required.");

        var rows = await ParseAndValidateAsync(csvContent, ct);

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var existing = await _db.Results.FirstOrDefaultAsync(
                    r => r.FileName == fileName, ct);
                var overwritten = existing != null;

                if (overwritten)
                {
                    // Удаление предыдущих значений для этого файла перед вставкой нового набора.
                    var oldValues = _db.Values.Where(v => v.FileName == fileName);
                    _db.Values.RemoveRange(oldValues);
                    await _db.SaveChangesAsync(ct);
                }

                var valueEntities = rows.Select(r => new ValueRecord
                {
                    FileName = fileName,
                    Date = r.Date,
                    ExecutionTime = r.ExecutionTime,
                    Value = r.Value
                }).ToList();

                await _db.Values.AddRangeAsync(valueEntities, ct);

                var aggregate = ComputeAggregate(fileName, rows);

                if (overwritten)
                {
                    existing!.DeltaTimeSeconds = aggregate.DeltaTimeSeconds;
                    existing.StartDate = aggregate.StartDate;
                    existing.AverageExecutionTime = aggregate.AverageExecutionTime;
                    existing.AverageValue = aggregate.AverageValue;
                    existing.MedianValue = aggregate.MedianValue;
                    existing.MaxValue = aggregate.MaxValue;
                    existing.MinValue = aggregate.MinValue;
                    existing.RowCount = aggregate.RowCount;
                    existing.ProcessedAt = DateTimeOffset.UtcNow;
                }
                else
                {
                    aggregate.ProcessedAt = DateTimeOffset.UtcNow;
                    await _db.Results.AddAsync(aggregate, ct);
                }

                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return new UploadResultDto(
                    fileName,
                    rows.Count,
                    aggregate.DeltaTimeSeconds,
                    aggregate.StartDate,
                    aggregate.AverageExecutionTime,
                    aggregate.AverageValue,
                    aggregate.MedianValue,
                    aggregate.MaxValue,
                    aggregate.MinValue,
                    overwritten);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }

    private static ResultRecord ComputeAggregate(string fileName, List<ParsedRow> rows)
    {
        var dates = rows.Select(r => r.Date).ToList();
        var executionTimes = rows.Select(r => r.ExecutionTime).ToList();
        var values = rows.Select(r => r.Value).OrderBy(v => v).ToList();

        var minDate = dates.Min();
        var maxDate = dates.Max();

        return new ResultRecord
        {
            FileName = fileName,
            DeltaTimeSeconds = (maxDate - minDate).TotalSeconds,
            StartDate = minDate,
            AverageExecutionTime = executionTimes.Average(),
            AverageValue = values.Average(),
            MedianValue = Median(values),
            MaxValue = values[^1],
            MinValue = values[0],
            RowCount = rows.Count
        };
    }

    private static double Median(List<double> sortedValues)
    {
        var n = sortedValues.Count;
        if (n == 0) return 0;
        var mid = n / 2;
        return n % 2 == 0
            ? (sortedValues[mid - 1] + sortedValues[mid]) / 2.0
            : sortedValues[mid];
    }

    private async Task<List<ParsedRow>> ParseAndValidateAsync(Stream csvContent, CancellationToken ct)
    {
        using var reader = new StreamReader(csvContent);

        var headerLine = await reader.ReadLineAsync(ct);
        if (headerLine == null)
            throw new CsvValidationException("The file is empty.");

        var header = headerLine.Trim();
        if (!header.Equals("Date;ExecutionTime;Value", StringComparison.OrdinalIgnoreCase))
            throw new CsvValidationException(
                "Invalid CSV header. Expected: Date;ExecutionTime;Value");

        var rows = new List<ParsedRow>();
        var errors = new List<string>();
        var now = DateTimeOffset.UtcNow;

        int lineNumber = 1;
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(';');
            if (parts.Length != 3)
            {
                errors.Add($"Line {lineNumber}: expected 3 fields separated by ';', got {parts.Length}.");
                continue;
            }

            var (dateRaw, execRaw, valueRaw) = (parts[0].Trim(), parts[1].Trim(), parts[2].Trim());

            if (string.IsNullOrEmpty(dateRaw) || string.IsNullOrEmpty(execRaw) || string.IsNullOrEmpty(valueRaw))
            {
                errors.Add($"Line {lineNumber}: all three fields (Date, ExecutionTime, Value) are required.");
                continue;
            }

            if (!TryParseDate(dateRaw, out var date))
            {
                errors.Add($"Line {lineNumber}: '{dateRaw}' is not a valid date in format yyyy-MM-ddTHH-mm-ss.ffffZ.");
                continue;
            }

            if (!double.TryParse(execRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var execTime))
            {
                errors.Add($"Line {lineNumber}: '{execRaw}' is not a valid floating point ExecutionTime.");
                continue;
            }

            if (!double.TryParse(valueRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                errors.Add($"Line {lineNumber}: '{valueRaw}' is not a valid floating point Value.");
                continue;
            }

            if (date > now)
                errors.Add($"Line {lineNumber}: Date '{dateRaw}' cannot be later than the current moment.");

            if (date < MinAllowedDate)
                errors.Add($"Line {lineNumber}: Date '{dateRaw}' cannot be earlier than 2000-01-01.");

            if (execTime < 0)
                errors.Add($"Line {lineNumber}: ExecutionTime cannot be negative.");

            if (value < 0)
                errors.Add($"Line {lineNumber}: Value cannot be negative.");

            rows.Add(new ParsedRow(date, execTime, value));
        }

        if (rows.Count < MinRows)
            errors.Add($"The file must contain at least {MinRows} data row(s).");

        if (rows.Count > MaxRows)
            errors.Add($"The file must not contain more than {MaxRows} data rows.");

        if (errors.Count > 0)
            throw new CsvValidationException(errors);

        return rows;
    }

    private static bool TryParseDate(string raw, out DateTimeOffset date)
    {
        foreach (var format in DateFormats)
        {
            if (DateTimeOffset.TryParseExact(
                    raw, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out date))
            {
                return true;
            }
        }

        // Переход к режиму мягкого разбора на случай,
        // если будет предоставлено значение, корректно оформленное по стандарту ISO 8601.
        return DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date);
    }

    private record ParsedRow(DateTimeOffset Date, double ExecutionTime, double Value);
}
