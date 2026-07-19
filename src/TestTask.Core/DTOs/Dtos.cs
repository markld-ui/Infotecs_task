namespace TestTask.Core.DTOs;

public record UploadResultDto(
    string FileName,
    int RowsProcessed,
    double DeltaTimeSeconds,
    DateTimeOffset StartDate,
    double AverageExecutionTime,
    double AverageValue,
    double MedianValue,
    double MaxValue,
    double MinValue,
    bool Overwritten
);

public record ResultDto(
    string FileName,
    double DeltaTimeSeconds,
    DateTimeOffset StartDate,
    double AverageExecutionTime,
    double AverageValue,
    double MedianValue,
    double MaxValue,
    double MinValue,
    int RowCount,
    DateTimeOffset ProcessedAt
);

public record ValueDto(
    DateTimeOffset Date,
    double ExecutionTime,
    double Value
);

/// <summary>Парамтеры фильтрации для GET /api/results.</summary>
public class ResultFilterDto
{
    public string? FileName { get; set; }

    public DateTimeOffset? StartDateFrom { get; set; }
    public DateTimeOffset? StartDateTo { get; set; }

    public double? AverageValueFrom { get; set; }
    public double? AverageValueTo { get; set; }

    public double? AverageExecutionTimeFrom { get; set; }
    public double? AverageExecutionTimeTo { get; set; }
}
