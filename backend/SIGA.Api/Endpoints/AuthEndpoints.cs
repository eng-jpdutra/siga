using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using SIGA.Api.Data;
using SIGA.Api.DTOs;
using SIGA.Api.Services;

namespace SIGA.Api.Endpoints;

public static class AuthEndpoints
{
    private const string SubpastaFotos = "fotos-usuarios";

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/auth");

        grupo.MapPost("/login", LoginAsync);
        // Qualquer usuário autenticado pode trocar a própria senha — não
        // exige o papel Administrador (isso é só pra gerenciar outras contas).
        grupo.MapPost("/alterar-senha", AlterarSenhaAsync).RequireAuthorization();

        grupo.MapGet("/minha-foto", ObterMinhaFotoAsync).RequireAuthorization();
        grupo.MapPost("/minha-foto", EnviarMinhaFotoAsync).RequireAuthorization().DisableAntiforgery();
    }

    private static async Task<Results<Ok<LoginResponse>, UnauthorizedHttpResult>> LoginAsync(
        SigaDbContext db, TokenService tokenService, LoginRequest request)
    {
        var usuarioNormalizado = request.NomeUsuario.Trim().ToLower();

        var usuario = await db.Usuarios
            .Include(u => u.Papeis)
            .FirstOrDefaultAsync(u => u.NomeUsuario.ToLower() == usuarioNormalizado && u.Ativo);

        // Mensagem genérica de propósito: não dá pra um invasor descobrir se
        // o usuário existe ou só a senha errou.
        if (usuario is null || !SenhaHasher.Confere(request.Senha, usuario.SenhaHash))
            return TypedResults.Unauthorized();

        var (token, expiraEm) = tokenService.GerarToken(usuario);
        var papeis = usuario.Papeis.Select(p => p.Nome).ToArray();

        return TypedResults.Ok(new LoginResponse(token, expiraEm, usuario.Nome, papeis));
    }

    private static async Task<Results<NoContent, ValidationProblem, UnauthorizedHttpResult>> AlterarSenhaAsync(
        SigaDbContext db, HttpContext http, AlterarSenhaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NovaSenha) || request.NovaSenha.Length < 8)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["novaSenha"] = ["A senha precisa ter pelo menos 8 caracteres."],
            });
        }

        var usuarioId = UsuarioIdLogado(http);
        if (usuarioId is null) return TypedResults.Unauthorized();

        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario is null || !SenhaHasher.Confere(request.SenhaAtual, usuario.SenhaHash))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["senhaAtual"] = ["Senha atual incorreta."],
            });
        }

        usuario.SenhaHash = SenhaHasher.Hash(request.NovaSenha);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static async Task<Results<PhysicalFileHttpResult, NotFound, UnauthorizedHttpResult>> ObterMinhaFotoAsync(
        SigaDbContext db, HttpContext http, ArmazenamentoArquivos armazenamento)
    {
        var usuarioId = UsuarioIdLogado(http);
        if (usuarioId is null) return TypedResults.Unauthorized();

        var usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario?.FotoPath is null || !armazenamento.Existe(usuario.FotoPath))
            return TypedResults.NotFound();

        var caminhoCompleto = armazenamento.ObterCaminhoCompleto(usuario.FotoPath);
        var provider = new FileExtensionContentTypeProvider();
        var contentType = provider.TryGetContentType(caminhoCompleto, out var tipo) ? tipo : "application/octet-stream";

        return TypedResults.PhysicalFile(caminhoCompleto, contentType);
    }

    private static async Task<Results<NoContent, ValidationProblem, UnauthorizedHttpResult>> EnviarMinhaFotoAsync(
        SigaDbContext db, HttpContext http, ArmazenamentoArquivos armazenamento, [FromForm] IFormFile foto)
    {
        if (!armazenamento.ImagemValida(foto, out var erro))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["foto"] = [erro!] });
        }

        var usuarioId = UsuarioIdLogado(http);
        if (usuarioId is null) return TypedResults.Unauthorized();

        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario is null) return TypedResults.Unauthorized();

        // Substitui: apaga o arquivo antigo e salva o novo com um nome
        // (GUID) novo — nunca reaproveita nem guarda o nome original do envio.
        armazenamento.Remover(usuario.FotoPath);
        usuario.FotoPath = await armazenamento.SalvarAsync(foto, SubpastaFotos);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static int? UsuarioIdLogado(HttpContext http)
    {
        var sub = http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? http.User.FindFirstValue("sub");
        return int.TryParse(sub, out var id) ? id : null;
    }
}
