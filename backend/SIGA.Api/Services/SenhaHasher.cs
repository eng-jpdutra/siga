namespace SIGA.Api.Services;

// BCrypt: hash com salt embutido, gerado de novo a cada chamada (por isso o
// mesmo hash nunca se repete para a mesma senha) — nunca gravar a senha em
// texto puro (regra inegociável do CLAUDE.md).
public static class SenhaHasher
{
    public static string Hash(string senha) => BCrypt.Net.BCrypt.HashPassword(senha);

    public static bool Confere(string senha, string hash) => BCrypt.Net.BCrypt.Verify(senha, hash);
}
