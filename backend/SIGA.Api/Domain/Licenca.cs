namespace SIGA.Api.Domain;

// Licença de software vinculada a um equipamento. `ChaveCriptografada` nunca
// guarda a chave em texto puro (ver Services/CriptografiaLicenca.cs) — regra
// inegociável do CLAUDE.md.
public class Licenca
{
    public int Id { get; set; }

    public int EquipamentoId { get; set; }

    public Equipamento Equipamento { get; set; } = null!;

    public string Produto { get; set; } = string.Empty;

    public string ChaveCriptografada { get; set; } = string.Empty;

    public TipoLicenca? Tipo { get; set; }

    public string? Observacao { get; set; }

    // Nota fiscal de compra da licença — independente da nota fiscal do
    // equipamento (às vezes a licença é comprada separadamente).
    public int? NotaFiscalId { get; set; }

    public NotaFiscal? NotaFiscal { get; set; }
}
