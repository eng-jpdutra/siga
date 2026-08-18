namespace SIGA.Api.Domain;

// Pessoa responsável por um ou mais equipamentos. Desativação é soft delete
// (Status = Inativo), igual ao restante do inventário.
public class Responsavel
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Cargo { get; set; }

    public int? LocalId { get; set; }

    public Local? Local { get; set; }

    public string? Contato { get; set; }

    public StatusResponsavel Status { get; set; } = StatusResponsavel.Ativo;

    public string? Observacao { get; set; }

    public ICollection<Equipamento> Equipamentos { get; set; } = new List<Equipamento>();
}
