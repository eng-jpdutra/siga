namespace SIGA.Api.Domain;

// Subtabela TPT de `equipamento` (PK = FK para equipamento.id).
public class Computador : Equipamento
{
    public SubtipoComputador? Subtipo { get; set; }

    public string? SistemaOperacional { get; set; }

    public short? RamGb { get; set; }

    public int? ArmazenamentoGb { get; set; }

    public TipoArmazenamento? TipoArmazenamento { get; set; }

    public string? Processador { get; set; }
}
