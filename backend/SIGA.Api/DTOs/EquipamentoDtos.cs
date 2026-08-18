using SIGA.Api.Domain;

namespace SIGA.Api.DTOs;

// Versão "resumo" pra listagem em grade — só os campos comuns, sem os
// específicos de cada subtipo (que só importam na tela de detalhe/edição).
public record EquipamentoResumoResponse(
    int Id,
    string Tipo,
    string? Patrimonio,
    string? NumeroSerie,
    string Marca,
    string Modelo,
    string Status,
    int? LocalId,
    string? LocalNome,
    int? ResponsavelId,
    string? ResponsavelNome
);

// Versão completa — inclui os campos de todos os subtipos (só os do tipo
// certo vêm preenchidos; os demais ficam null). Usada na tela de detalhe/edição.
public record EquipamentoResponse(
    int Id,
    string Tipo,
    string? Patrimonio,
    string? NumeroSerie,
    string Marca,
    string Modelo,
    int? LocalId,
    string? LocalNome,
    int? ResponsavelId,
    string? ResponsavelNome,
    int? NotaFiscalId,
    string? NotaFiscalNumero,
    string Status,
    short? AnoAquisicao,
    DateOnly? GarantiaAte,
    string? Observacao,
    DateTime CriadoEm,
    DateTime? AtualizadoEm,
    // Computador
    string? SubtipoComputador,
    string? SistemaOperacional,
    short? RamGb,
    int? ArmazenamentoGb,
    string? TipoArmazenamento,
    string? Processador,
    // Impressora
    string? TipoImpressao,
    bool? Colorida,
    string? Conexao,
    int? ContadorPaginas,
    // DispositivoRede
    string? SubtipoDispositivoRede,
    string? EnderecoIp,
    string? EnderecoMac,
    short? NumPortas,
    string? VersaoFirmware
);

// Mesmo formato pra criar e atualizar — o Tipo não muda depois de criado
// (trocar o tipo de um equipamento não faz sentido no mundo real; se
// precisar, dá baixa e cria outro).
public record EquipamentoRequest(
    TipoEquipamento Tipo,
    string? Patrimonio,
    string? NumeroSerie,
    string Marca,
    string Modelo,
    int? LocalId,
    int? ResponsavelId,
    int? NotaFiscalId,
    short? AnoAquisicao,
    DateOnly? GarantiaAte,
    string? Observacao,
    // Computador
    SubtipoComputador? SubtipoComputador,
    string? SistemaOperacional,
    short? RamGb,
    int? ArmazenamentoGb,
    TipoArmazenamento? TipoArmazenamento,
    string? Processador,
    // Impressora
    TipoImpressao? TipoImpressao,
    bool Colorida,
    ConexaoImpressora? Conexao,
    int? ContadorPaginas,
    // DispositivoRede
    SubtipoDispositivoRede? SubtipoDispositivoRede,
    string? EnderecoIp,
    string? EnderecoMac,
    short? NumPortas,
    string? VersaoFirmware
);
