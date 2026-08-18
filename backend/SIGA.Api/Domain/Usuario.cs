namespace SIGA.Api.Domain;

// Conta de login — separada do inventário (ver "identidade e segurança" no
// CLAUDE.md). `SenhaHash` nunca guarda a senha em texto puro (ver Services/SenhaHasher.cs).
public class Usuario
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string NomeUsuario { get; set; } = string.Empty;

    public string SenhaHash { get; set; } = string.Empty;

    // Desativação é soft delete: usuário barrado nunca é apagado, só Ativo = false.
    public bool Ativo { get; set; } = true;

    // Caminho relativo do arquivo em disco — mesmo padrão da nota fiscal
    // (ver Services/ArmazenamentoArquivos.cs). Nome em disco é sempre um
    // GUID novo a cada envio, nunca o nome original do arquivo.
    public string? FotoPath { get; set; }

    public int? ResponsavelId { get; set; }

    public Responsavel? Responsavel { get; set; }

    public ICollection<Papel> Papeis { get; set; } = new List<Papel>();
}
