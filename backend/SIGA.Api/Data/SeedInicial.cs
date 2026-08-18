using Microsoft.EntityFrameworkCore;
using SIGA.Api.Domain;
using SIGA.Api.Services;

namespace SIGA.Api.Data;

// Garante que os papéis básicos existam e, só em desenvolvimento, cria um
// usuário administrador padrão pra dar o primeiro login — sem isso não tem
// como logar e criar os demais usuários (nenhuma rota de cadastro de
// usuário existe ainda). Em produção esse bootstrap é responsabilidade de
// quem faz o primeiro deploy (fora do escopo por ora).
public static class SeedInicial
{
    public const string UsuarioAdminPadrao = "admin";
    public const string SenhaAdminPadrao = "Trocar@123";

    public static async Task ExecutarAsync(SigaDbContext db, bool criarAdminPadrao)
    {
        string[] papeisBasicos = ["Administrador", "Operador", "Consulta"];
        foreach (var nome in papeisBasicos)
        {
            if (!await db.Papeis.AnyAsync(p => p.Nome == nome))
                db.Papeis.Add(new Papel { Nome = nome });
        }
        await db.SaveChangesAsync();

        if (criarAdminPadrao && !await db.Usuarios.AnyAsync())
        {
            var papelAdmin = await db.Papeis.FirstAsync(p => p.Nome == "Administrador");
            db.Usuarios.Add(new Usuario
            {
                Nome = "Administrador",
                NomeUsuario = UsuarioAdminPadrao,
                SenhaHash = SenhaHasher.Hash(SenhaAdminPadrao),
                Ativo = true,
                Papeis = [papelAdmin],
            });
            await db.SaveChangesAsync();
        }
    }
}
