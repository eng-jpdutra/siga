namespace SIGA.Api.Domain;

// Diário do equipamento (manutenção, formatação, mudança de local, troca de
// responsável). O texto de `Descricao` é gerado de forma padronizada pela
// aplicação no momento do lançamento — ver CLAUDE.md.
public class Historico
{
    public int Id { get; set; }

    public int EquipamentoId { get; set; }

    public Equipamento Equipamento { get; set; } = null!;

    public TipoHistorico Tipo { get; set; }

    public DateOnly Data { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public string? RegistradoPor { get; set; }

    public DateTime RegistradoEm { get; set; }
}
