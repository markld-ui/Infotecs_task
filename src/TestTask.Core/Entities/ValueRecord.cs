namespace TestTask.Core.Entities;

/// <summary>
/// Одна строка, извлеченная из загруженного CSV-файла (таблица «Values»).
/// </summary>
public class ValueRecord
{
    public long Id { get; set; }

    /// <summary>Имя исходного CSV-файла (используется для группировки, перезаписи или фильтрации).</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Время начала операции.</summary>
    public DateTimeOffset Date { get; set; }

    /// <summary>Время выполнения в секундах</summary>
    public double ExecutionTime { get; set; }

    /// <summary>Измеренное значение (показатель с плавающей запятой).</summary>
    public double Value { get; set; }
}
