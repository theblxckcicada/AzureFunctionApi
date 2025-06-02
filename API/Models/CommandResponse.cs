namespace DMIX.API.Models;

public record CommandResponse<TModel>
{
    public string ValidationResult { get; init; } = string.Empty;
    public TModel[]? Entities
    {
        get; init;
    }
}
