namespace SIGA.Api.Domain;

// Tabela base da herança TPT (equipamento / computador / impressora /
// dispositivo_rede) — ver "Modelo de dados — inventário" no CLAUDE.md.
// `NotaFiscalId` ainda é só a FK (sem entidade nem navegação): nota_fiscal
// entra numa etapa futura.
public class Equipamento
{
    public int Id { get; set; }

    public TipoEquipamento Tipo { get; set; }

    public string? Patrimonio { get; set; }

    public string? NumeroSerie { get; set; }

    public string Marca { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public int? LocalId { get; set; }

    public Local? Local { get; set; }

    public int? ResponsavelId { get; set; }

    public Responsavel? Responsavel { get; set; }

    public int? NotaFiscalId { get; set; }

    // Soft delete: "excluir" um equipamento é gravar Status = Baixado.
    public StatusEquipamento Status { get; set; } = StatusEquipamento.Ativo;

    public short? AnoAquisicao { get; set; }

    public DateOnly? GarantiaAte { get; set; }

    public string? Observacao { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime? AtualizadoEm { get; set; }

    public ICollection<Historico> Historicos { get; set; } = new List<Historico>();
}
