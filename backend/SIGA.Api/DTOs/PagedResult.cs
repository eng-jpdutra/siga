namespace SIGA.Api.DTOs;

// Envelope padrão para toda listagem paginada no banco (Skip/Take) — ver CLAUDE.md.
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
