using System.ComponentModel;

namespace DMIX.API.Models;


public record QueryFilter
{
    public string? Filter
    {
        get; init;
    }
    public string? Sort
    {
        get; init;
    }
    public string? SortDirection
    {
        get; init;
    }
}
public class EntityQuery
{
    public List<QueryFilter> Query
    {
        get; init;
    } = [];
    public int PageSize { get; init; } = 10;
    public int PageIndex { get; init; } = 0;


}

public static class EntityKey
{
    public static TKey ParseId<TKey>(string value)
    {
        var converter = TypeDescriptor.GetConverter(typeof(TKey));
        return (TKey)converter.ConvertFromInvariantString(value)!;
    }
}

