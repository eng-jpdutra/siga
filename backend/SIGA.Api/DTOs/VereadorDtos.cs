namespace SIGA.Api.DTOs;

public record VereadorResponse(
    int Id,
    string Nome,
    string? Partido,
    string? Contato,
    int? LocalId,
    string? LocalNome,
    bool Ativo
);

// Usado tanto para criar quanto para atualizar — o status muda por endpoint
// próprio (ativar/desativar), nunca por aqui.
public record VereadorRequest(
    string Nome,
    string? Partido,
    string? Contato,
    int? LocalId
);
