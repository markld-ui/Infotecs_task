using TestTask.Core.DTOs;

namespace TestTask.Core.Interfaces;

public interface ICsvProcessingService
{
    /// <summary>
    /// Выполняет синтаксический анализ, проверку и сохранение содержимого CSV-файла.
    /// Если файл с таким именем уже существует, его данные перезаписываются.
    /// Вызывает <see cref="Exceptions.CsvValidationException"/> при обнаружении недопустимого содержимого.
    /// </summary>
    Task<UploadResultDto> ProcessAsync(string fileName, Stream csvContent, CancellationToken ct = default);
}

public interface IResultsService
{
    Task<IReadOnlyList<ResultDto>> GetFilteredAsync(ResultFilterDto filter, CancellationToken ct = default);

    /// <summary>Последние 10 значений для указанного имени файла,
    /// отсортированные по дате в порядке возрастания... (порядок см. в реализации).</summary>
    Task<IReadOnlyList<ValueDto>> GetLastValuesAsync(string fileName, int take = 10, CancellationToken ct = default);
}
