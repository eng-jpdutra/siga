namespace SIGA.Api.DTOs;

public record ResponsavelResponse(
    int Id,
    string Nome,
    string? Cargo,
    int? LocalId,
    string? LocalNome,
    string? Contato,
    string Status,
    string? Observacao
);

// Usado tanto para criar quanto para atualizar — o status muda por endpoint
// próprio (ativar/desativar), nunca por aqui.
public record ResponsavelRequest(
    string Nome,
    string? Cargo,
    int? LocalId,
    string? Contato,
    string? Observacao
);
