namespace SIGA.Api.DTOs;

// `Chave` aqui já vem descriptografada — só chega a quem tem token válido
// (rota autenticada). Nunca é logada nem aparece na listagem resumida.
public record LicencaResponse(
    int Id,
    int EquipamentoId,
    string EquipamentoDescricao,
    string Produto,
    string Chave,
    string? Tipo,
    string? Observacao,
    int? NotaFiscalId,
    string? NotaFiscalNumero
);

public record LicencaRequest(
    int EquipamentoId,
    string Produto,
    string Chave,
    SIGA.Api.Domain.TipoLicenca? Tipo,
    string? Observacao,
    int? NotaFiscalId
);
