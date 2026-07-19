using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TestTask.Core.DTOs;
using TestTask.Core.Entities;
using TestTask.Infrastructure;
using TestTask.Infrastructure.Services;
using Xunit;

namespace TestTask.Tests;

public class ResultsServiceTests
{
    private static AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static async Task SeedAsync(AppDbContext db)
    {
        db.Results.AddRange(
            new ResultRecord
            {
                FileName = "a.csv", StartDate = new DateTimeOffset(
                    2024, 1, 1, 
                    0, 0, 0, 
                    TimeSpan.Zero),
                AverageValue = 10, AverageExecutionTime = 1, MedianValue = 10, MaxValue = 10, MinValue = 10,
                DeltaTimeSeconds = 0, RowCount = 1, ProcessedAt = DateTimeOffset.UtcNow
            },
            new ResultRecord
            {
                FileName = "b.csv", StartDate = new DateTimeOffset(
                    2024, 6, 1, 
                    0, 0, 0, 
                    TimeSpan.Zero),
                AverageValue = 50, AverageExecutionTime = 5, MedianValue = 50, MaxValue = 50, MinValue = 50,
                DeltaTimeSeconds = 0, RowCount = 1, ProcessedAt = DateTimeOffset.UtcNow
            });

        db.Values.AddRange(
            new ValueRecord { FileName = "a.csv", Date = new DateTimeOffset(
                2024, 1, 1,
                0, 0, 0, 
                TimeSpan.Zero), ExecutionTime = 1, Value = 1 },
            new ValueRecord { FileName = "a.csv", Date = new DateTimeOffset(
                2024, 1, 2,
                0, 0, 0, 
                TimeSpan.Zero), ExecutionTime = 1, Value = 2 },
            new ValueRecord { FileName = "a.csv", Date = new DateTimeOffset(
                2024, 1, 3, 
                0, 0, 0, 
                TimeSpan.Zero), ExecutionTime = 1, Value = 3 });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetFilteredAsync_ByFileName_ReturnsMatchingRows()
    {
        await using var db = CreateContext(nameof(GetFilteredAsync_ByFileName_ReturnsMatchingRows));
        await SeedAsync(db);
        var sut = new ResultsService(db);

        var results = await sut.GetFilteredAsync(new ResultFilterDto { FileName = "a" });

        results.Should().ContainSingle(r => r.FileName == "a.csv");
    }

    [Fact]
    public async Task GetFilteredAsync_ByAverageValueRange_ReturnsMatchingRows()
    {
        await using var db = CreateContext(nameof(GetFilteredAsync_ByAverageValueRange_ReturnsMatchingRows));
        await SeedAsync(db);
        var sut = new ResultsService(db);

        var results = await sut.GetFilteredAsync(new ResultFilterDto 
            { AverageValueFrom = 20, AverageValueTo = 100 });

        results.Should().ContainSingle(r => r.FileName == "b.csv");
    }

    [Fact]
    public async Task GetFilteredAsync_ByStartDateRange_ReturnsMatchingRows()
    {
        await using var db = CreateContext(nameof(GetFilteredAsync_ByStartDateRange_ReturnsMatchingRows));
        await SeedAsync(db);
        var sut = new ResultsService(db);

        var results = await sut.GetFilteredAsync(new ResultFilterDto
        {
            StartDateFrom = new DateTimeOffset(
                2024, 3, 1, 
                0, 0, 0,
                TimeSpan.Zero)
        });

        results.Should().ContainSingle(r => r.FileName == "b.csv");
    }

    [Fact]
    public async Task GetLastValuesAsync_ReturnsMostRecentFirst_LimitedByTake()
    {
        await using var db = CreateContext(nameof(GetLastValuesAsync_ReturnsMostRecentFirst_LimitedByTake));
        await SeedAsync(db);
        var sut = new ResultsService(db);

        var values = await sut.GetLastValuesAsync("a.csv", take: 2);

        values.Should().HaveCount(2);
        values[0].Date.Should().Be(new DateTimeOffset(
            2024, 1, 3, 
            0, 0, 0, 
            TimeSpan.Zero));
        values[1].Date.Should().Be(new DateTimeOffset(
            2024, 1, 2, 
            0, 0, 0, 
            TimeSpan.Zero));
    }

    [Fact]
    public async Task GetLastValuesAsync_UnknownFile_ReturnsEmpty()
    {
        await using var db = CreateContext(nameof(GetLastValuesAsync_UnknownFile_ReturnsEmpty));
        await SeedAsync(db);
        var sut = new ResultsService(db);

        var values = await sut.GetLastValuesAsync("missing.csv");

        values.Should().BeEmpty();
    }
}
