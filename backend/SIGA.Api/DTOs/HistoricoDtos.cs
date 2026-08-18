using SIGA.Api.Domain;

namespace SIGA.Api.DTOs;

public record HistoricoResponse(
    int Id,
    string Tipo,
    DateOnly Data,
    string Descricao,
    string? RegistradoPor,
    DateTime RegistradoEm
);

// Lançamento manual (manutenção, formatação, outro) — trocas de local e de
// responsável são geradas automaticamente pela API quando o equipamento
// muda, não entram por aqui.
public record HistoricoRequest(TipoHistorico Tipo, DateOnly Data, string Descricao);
