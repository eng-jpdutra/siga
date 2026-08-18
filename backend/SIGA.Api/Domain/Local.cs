namespace SIGA.Api.Domain;

// Local físico onde um equipamento fica (sala, setor). Nome único.
public class Local
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Descricao { get; set; }

    public ICollection<Equipamento> Equipamentos { get; set; } = new List<Equipamento>();
    public ICollection<Responsavel> Responsaveis { get; set; } = new List<Responsavel>();
}
