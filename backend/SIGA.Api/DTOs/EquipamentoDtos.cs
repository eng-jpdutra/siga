using SIGA.Api.Domain;

namespace SIGA.Api.DTOs;

// Versão "resumo" pra listagem em grade — só os campos comuns, sem os
// específicos de cada tipo (que só importam na tela de detalhe/edição).
public record EquipamentoResumoResponse(
    int Id,
    string Tipo,
    string? Patrimonio,
    string? NumeroSerie,
    string Marca,
    string Modelo,
    string Status,
    int? LocalId,
    string? LocalNome
);

// Versão completa — inclui `Detalhes` (campos específicos do tipo, livres).
public record EquipamentoResponse(
    int Id,
    string Tipo,
    string? Patrimonio,
    string? NumeroSerie,
    string Marca,
    string Modelo,
    int? LocalId,
    string? LocalNome,
    int? NotaFiscalId,
    string? NotaFiscalNumero,
    string? EnderecoMac,
    string? EnderecoIp,
    Dictionary<string, object?>? Detalhes,
    string Status,
    short? AnoAquisicao,
    DateOnly? GarantiaAte,
    string? Observacao,
    DateTime CriadoEm,
    DateTime? AtualizadoEm
);

public record EquipamentoRequest(
    TipoEquipamento Tipo,
    string? Patrimonio,
    string? NumeroSerie,
    string Marca,
    string Modelo,
    int? LocalId,
    int? NotaFiscalId,
    string? EnderecoMac,
    string? EnderecoIp,
    Dictionary<string, object?>? Detalhes,
    short? AnoAquisicao,
    DateOnly? GarantiaAte,
    string? Observacao
);
