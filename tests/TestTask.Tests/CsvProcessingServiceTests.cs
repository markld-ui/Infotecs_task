using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TestTask.Core.DTOs;
using TestTask.Core.Exceptions;
using TestTask.Infrastructure;
using TestTask.Infrastructure.Services;
using Xunit;

namespace TestTask.Tests;

public class CsvProcessingServiceTests
{
    private static AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private static Stream ToStream(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public async Task ProcessAsync_ValidFile_PersistsValuesAndComputesResults()
    {
        await using var db = CreateContext(nameof(ProcessAsync_ValidFile_PersistsValuesAndComputesResults));
        var sut = new CsvProcessingService(db);

        var csv =
            "Date;ExecutionTime;Value\n" +
            "2024-01-01T10-00-00.0000Z;1.5;10\n" +
            "2024-01-01T10-05-00.0000Z;2.5;20\n" +
            "2024-01-01T10-10-00.0000Z;3.5;30\n";

        var result = await sut.ProcessAsync("test.csv", ToStream(csv));

        result.RowsProcessed.Should().Be(3);
        result.Overwritten.Should().BeFalse();
        result.AverageValue.Should().Be(20);
        result.MedianValue.Should().Be(20);
        result.MaxValue.Should().Be(30);
        result.MinValue.Should().Be(10);
        result.DeltaTimeSeconds.Should().Be(600); // 10 минут

        (await db.Values.CountAsync()).Should().Be(3);
        (await db.Results.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_SameFileNameTwice_OverwritesPreviousData()
    {
        await using var db = CreateContext(nameof(ProcessAsync_SameFileNameTwice_OverwritesPreviousData));
        var sut = new CsvProcessingService(db);

        var firstCsv =
            "Date;ExecutionTime;Value\n" +
            "2024-01-01T10-00-00.0000Z;1;10\n" +
            "2024-01-01T10-01-00.0000Z;1;20\n";
        await sut.ProcessAsync("dup.csv", ToStream(firstCsv));

        var secondCsv =
            "Date;ExecutionTime;Value\n" +
            "2024-02-01T10-00-00.0000Z;5;100\n";
        var second = await sut.ProcessAsync("dup.csv", ToStream(secondCsv));

        second.Overwritten.Should().BeTrue();
        (await db.Values.CountAsync(v => v.FileName == "dup.csv")).Should().Be(1);
        (await db.Results.CountAsync(r => r.FileName == "dup.csv")).Should().Be(1);
        (await db.Values.SingleAsync(v => v.FileName == "dup.csv")).Value.Should().Be(100);
    }

    [Fact]
    public async Task ProcessAsync_DateInFuture_ThrowsValidationExceptionAndPersistsNothing()
    {
        await using var db = CreateContext(nameof(ProcessAsync_DateInFuture_ThrowsValidationExceptionAndPersistsNothing));
        var sut = new CsvProcessingService(db);

        var futureDate = DateTimeOffset.UtcNow.AddYears(1).ToString("yyyy-MM-dd'T'HH-mm-ss.ffff'Z'");
        var csv = $"Date;ExecutionTime;Value\n{futureDate};1;10\n";

        var act = async () => await sut.ProcessAsync("future.csv", ToStream(csv));

        await act.Should().ThrowAsync<CsvValidationException>();
        (await db.Values.CountAsync()).Should().Be(0);
        (await db.Results.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ProcessAsync_DateBeforeYear2000_Throws()
    {
        await using var db = CreateContext(nameof(ProcessAsync_DateBeforeYear2000_Throws));
        var sut = new CsvProcessingService(db);

        var csv = "Date;ExecutionTime;Value\n1999-12-31T23-59-59.0000Z;1;10\n";

        var act = async () => await sut.ProcessAsync("old.csv", ToStream(csv));

        await act.Should().ThrowAsync<CsvValidationException>();
    }

    [Theory]
    [InlineData("Date;ExecutionTime;Value\n2024-01-01T10-00-00.0000Z;-1;10\n")] // отрицательное время выполнения
    [InlineData("Date;ExecutionTime;Value\n2024-01-01T10-00-00.0000Z;1;-10\n")] // отрицательное значение
    [InlineData("Date;ExecutionTime;Value\n2024-01-01T10-00-00.0000Z;1;\n")]     // пропущенное значение
    [InlineData("Date;ExecutionTime;Value\n")]                                    // отсутствие строк
    public async Task ProcessAsync_InvalidRows_ThrowsValidationException(string csv)
    {
        await using var db = CreateContext(Guid.NewGuid().ToString());
        var sut = new CsvProcessingService(db);

        var act = async () => await sut.ProcessAsync("invalid.csv", ToStream(csv));

        await act.Should().ThrowAsync<CsvValidationException>();
    }

    [Fact]
    public async Task ProcessAsync_TooManyRows_Throws()
    {
        await using var db = CreateContext(nameof(ProcessAsync_TooManyRows_Throws));
        var sut = new CsvProcessingService(db);

        var sb = new StringBuilder("Date;ExecutionTime;Value\n");
        for (var i = 0; i < 10_001; i++)
            sb.AppendLine($"2024-01-01T10-00-00.0000Z;1;{i}");

        var act = async () => await sut.ProcessAsync("toomany.csv", 
            ToStream(sb.ToString()));

        await act.Should().ThrowAsync<CsvValidationException>();
    }

    [Fact]
    public async Task ProcessAsync_WrongHeader_Throws()
    {
        await using var db = CreateContext(nameof(ProcessAsync_WrongHeader_Throws));
        var sut = new CsvProcessingService(db);

        var csv = "Foo;Bar;Baz\n2024-01-01T10-00-00.0000Z;1;10\n";

        var act = async () => await sut.ProcessAsync("badheader.csv", ToStream(csv));

        await act.Should().ThrowAsync<CsvValidationException>();
    }
}
