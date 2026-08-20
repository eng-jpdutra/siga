namespace SIGA.Api.Domain;

// Local físico onde um equipamento fica (sala, setor). Nome único.
public class Local
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    // Texto livre (gabinete, almoxarifado, plenário, setor...) — só ajuda a
    // organizar a listagem, não é uma lista fechada validada pelo backend.
    public string? Tipo { get; set; }

    public ICollection<Equipamento> Equipamentos { get; set; } = new List<Equipamento>();
    public ICollection<Vereador> Vereadores { get; set; } = new List<Vereador>();
}
