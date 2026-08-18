namespace SIGA.Api.DTOs;

public record UsuarioResponse(
    int Id,
    string Nome,
    string NomeUsuario,
    bool Ativo,
    string[] Papeis,
    int? ResponsavelId,
    string? ResponsavelNome
);

public record UsuarioCreateRequest(
    string Nome,
    string NomeUsuario,
    string Senha,
    int[] PapeisIds,
    int? ResponsavelId
);

public record UsuarioUpdateRequest(
    string Nome,
    string NomeUsuario,
    int[] PapeisIds,
    int? ResponsavelId
);

public record RedefinirSenhaRequest(string NovaSenha);

public record PapelResponse(int Id, string Nome);
