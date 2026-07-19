namespace TestTask.Core.Entities;

/// <summary>
/// Сводные (интегральные) результаты, рассчитанные для всего загруженного файла (таблица «Результаты»).
/// Одна строка на каждое уникальное имя файла (FileName);
/// повторная загрузка того же файла приводит к перезаписи строки.
/// </summary>
public class ResultRecord
{
    public long Id { get; set; }

    /// <summary>Уникальное имя исходного CSV файла.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Разница между max(Date) и min(Date), в секундах.</summary>
    public double DeltaTimeSeconds { get; set; }

    /// <summary>Минимальная дата — момент начала первой операции.</summary>
    public DateTimeOffset StartDate { get; set; }

    /// <summary>Среднее время выполнения по всем строкам.</summary>
    public double AverageExecutionTime { get; set; }

    /// <summary>Среднее значение по всем строкам.</summary>
    public double AverageValue { get; set; }

    /// <summary>Медианное значение по всем строкам.</summary>
    public double MedianValue { get; set; }

    /// <summary>Максимальное значение.</summary>
    public double MaxValue { get; set; }

    /// <summary>Минимальное значение.</summary>
    public double MinValue { get; set; }

    /// <summary>Количество строк которое содержит файл.</summary>
    public int RowCount { get; set; }

    /// <summary>Когда этот агрегат был (пере)вычислен.</summary>
    public DateTimeOffset ProcessedAt { get; set; }
}
