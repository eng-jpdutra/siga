namespace SIGA.Api.DTOs;

public record ConfiguracaoResponse(
    int Id,
    string Titulo,
    string Conteudo,
    DateTime CriadoEm,
    DateTime? AtualizadoEm
);

public record ConfiguracaoRequest(string Titulo, string Conteudo);
