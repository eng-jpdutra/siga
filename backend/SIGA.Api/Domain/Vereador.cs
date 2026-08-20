namespace SIGA.Api.Domain;

// Cadastro de vereadores mantido diretamente no SIGA. Isso é provisório: o
// destino final é um sistema "Legislativo" próprio, com o SIGA apenas
// consumindo uma cópia sincronizada (ver CLAUDE.md, seção "Domínio
// Legislativo"). Até esse sistema existir, o cadastro fica aqui.
public class Vereador
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string? Partido { get; set; }

    public string? Contato { get; set; }

    // Gabinete que o vereador ocupa. Fica na "vereador" (não na "local"),
    // porque nem todo local é gabinete — colocar aqui evita poluir "local"
    // com uma referência que só faz sentido para parte dos registros.
    public int? LocalId { get; set; }

    public Local? Local { get; set; }

    public bool Ativo { get; set; } = true;
}
