namespace AnimalKingdom.API.Models;

public record EntityQuery
{
    public string? Filter { get; init; }
    public int PageSize { get; init; } = 10;
    public int PageIndex { get; init; } = 0;
    public string? Sort { get; init; }
    public string? SortDirection { get; init; }
}
