namespace TestTask.Core.Exceptions;

/// <summary>
/// Выбрасывается, если загруженный CSV-файл не проходит проверку. Вся транзакция
/// должна быть отменена, а эта ошибка — возвращена вызывающей стороне с кодом 400 Bad Request.
/// </summary>
public class CsvValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public CsvValidationException(string error) : base(error)
    {
        Errors = new[] { error };
    }

    public CsvValidationException(IReadOnlyList<string> errors)
        : base(string.Join("; ", errors))
    {
        Errors = errors;
    }
}
