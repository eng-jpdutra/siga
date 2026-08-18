namespace SIGA.Api.DTOs;

public record LocalResponse(int Id, string Nome, string? Descricao);

public record LocalRequest(string Nome, string? Descricao);
