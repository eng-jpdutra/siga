namespace SIGA.Api.Domain;

// Perfil de acesso (Administrador, Operador, Consulta...). Vira claim de
// papel no JWT — a autorização por papel lê o token, sem consultar o banco
// a cada requisição (ver CLAUDE.md).
public class Papel
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
