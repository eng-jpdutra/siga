using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SIGA.Api.Data;
using SIGA.Api.Domain;
using SIGA.Api.DTOs;
using SIGA.Api.Services;

namespace SIGA.Api.Endpoints;

public static class UsuarioEndpoints
{
    private const int TamanhoPaginaMaximo = 100;

    public static void MapUsuarioEndpoints(this IEndpointRouteBuilder app)
    {
        // Gerenciar contas é ação sensível — só quem tem papel Administrador.
        var grupo = app.MapGroup("/api/usuarios").RequireAuthorization("SomenteAdministrador");

        grupo.MapGet("/", ListarAsync);
        grupo.MapGet("/{id:int}", ObterPorIdAsync);
        grupo.MapPost("/", CriarAsync);
        grupo.MapPut("/{id:int}", AtualizarAsync);
        grupo.MapPost("/{id:int}/desativar", DesativarAsync);
        grupo.MapPost("/{id:int}/ativar", AtivarAsync);
        grupo.MapPost("/{id:int}/redefinir-senha", RedefinirSenhaAsync);
    }

    private static async Task<Ok<PagedResult<UsuarioResponse>>> ListarAsync(
        SigaDbContext db, string? nome, bool? ativo, int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, TamanhoPaginaMaximo);

        var query = db.Usuarios.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(nome))
        {
            var termo = nome.ToLower();
            query = query.Where(u => u.Nome.ToLower().Contains(termo) || u.NomeUsuario.ToLower().Contains(termo));
        }

        if (ativo is not null)
            query = query.Where(u => u.Ativo == ativo);

        var totalCount = await query.CountAsync();

        var entidades = await query
            .Include(u => u.Papeis)
            .Include(u => u.Responsavel)
            .OrderBy(u => u.Nome)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = entidades.Select(ParaResponse).ToList();

        return TypedResults.Ok(new PagedResult<UsuarioResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    private static async Task<Results<Ok<UsuarioResponse>, NotFound>> ObterPorIdAsync(SigaDbContext db, int id)
    {
        var usuario = await db.Usuarios.AsNoTracking()
            .Include(u => u.Papeis)
            .Include(u => u.Responsavel)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuario is null) return TypedResults.NotFound();

        return TypedResults.Ok(ParaResponse(usuario));
    }

    private static async Task<Results<Created<UsuarioResponse>, ValidationProblem>> CriarAsync(
        SigaDbContext db, UsuarioCreateRequest request)
    {
        var erros = await ValidarAsync(db, request.Nome, request.NomeUsuario, request.PapeisIds, request.ResponsavelId);
        if (!string.IsNullOrWhiteSpace(request.Senha) && request.Senha.Length < 8)
            erros["senha"] = ["A senha precisa ter pelo menos 8 caracteres."];
        else if (string.IsNullOrWhiteSpace(request.Senha))
            erros["senha"] = ["Informe uma senha."];

        if (erros.Count > 0) return TypedResults.ValidationProblem(erros);

        var papeis = await db.Papeis.Where(p => request.PapeisIds.Contains(p.Id)).ToListAsync();

        var usuario = new Usuario
        {
            Nome = request.Nome.Trim(),
            NomeUsuario = request.NomeUsuario.Trim(),
            SenhaHash = SenhaHasher.Hash(request.Senha),
            Ativo = true,
            ResponsavelId = request.ResponsavelId,
            Papeis = papeis,
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        await db.Entry(usuario).Reference(u => u.Responsavel).LoadAsync();

        return TypedResults.Created($"/api/usuarios/{usuario.Id}", ParaResponse(usuario));
    }

    private static async Task<Results<Ok<UsuarioResponse>, NotFound, ValidationProblem>> AtualizarAsync(
        SigaDbContext db, int id, UsuarioUpdateRequest request)
    {
        var usuario = await db.Usuarios.Include(u => u.Papeis).FirstOrDefaultAsync(u => u.Id == id);
        if (usuario is null) return TypedResults.NotFound();

        var erros = await ValidarAsync(db, request.Nome, request.NomeUsuario, request.PapeisIds, request.ResponsavelId, ignorarId: id);
        if (erros.Count > 0) return TypedResults.ValidationProblem(erros);

        var papeis = await db.Papeis.Where(p => request.PapeisIds.Contains(p.Id)).ToListAsync();

        usuario.Nome = request.Nome.Trim();
        usuario.NomeUsuario = request.NomeUsuario.Trim();
        usuario.ResponsavelId = request.ResponsavelId;
        usuario.Papeis = papeis;
        await db.SaveChangesAsync();

        await db.Entry(usuario).Reference(u => u.Responsavel).LoadAsync();

        return TypedResults.Ok(ParaResponse(usuario));
    }

    private static async Task<Results<Ok<UsuarioResponse>, NotFound, BadRequest<string>>> DesativarAsync(
        SigaDbContext db, HttpContext http, int id)
    {
        if (UsuarioLogadoId(http) == id)
            return TypedResults.BadRequest("Você não pode desativar a própria conta.");

        return await MudarStatusAsync(db, id, ativo: false);
    }

    private static Task<Results<Ok<UsuarioResponse>, NotFound, BadRequest<string>>> AtivarAsync(SigaDbContext db, int id) =>
        MudarStatusAsync(db, id, ativo: true);

    private static async Task<Results<Ok<UsuarioResponse>, NotFound, BadRequest<string>>> MudarStatusAsync(
        SigaDbContext db, int id, bool ativo)
    {
        var usuario = await db.Usuarios.Include(u => u.Papeis).Include(u => u.Responsavel)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (usuario is null) return TypedResults.NotFound();

        usuario.Ativo = ativo;
        await db.SaveChangesAsync();

        return TypedResults.Ok(ParaResponse(usuario));
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> RedefinirSenhaAsync(
        SigaDbContext db, int id, RedefinirSenhaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NovaSenha) || request.NovaSenha.Length < 8)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["novaSenha"] = ["A senha precisa ter pelo menos 8 caracteres."],
            });
        }

        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
        if (usuario is null) return TypedResults.NotFound();

        usuario.SenhaHash = SenhaHasher.Hash(request.NovaSenha);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static int? UsuarioLogadoId(HttpContext http)
    {
        var sub = http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? http.User.FindFirstValue("sub");
        return int.TryParse(sub, out var id) ? id : null;
    }

    private static async Task<Dictionary<string, string[]>> ValidarAsync(
        SigaDbContext db, string nome, string nomeUsuario, int[] papeisIds, int? responsavelId, int? ignorarId = null)
    {
        var erros = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(nome))
            erros["nome"] = ["Informe o nome."];
        else if (nome.Trim().Length > 120)
            erros["nome"] = ["O nome pode ter no máximo 120 caracteres."];

        if (string.IsNullOrWhiteSpace(nomeUsuario))
        {
            erros["nomeUsuario"] = ["Informe o nome de usuário."];
        }
        else
        {
            var normalizado = nomeUsuario.Trim().ToLower();
            var emUso = await db.Usuarios.AnyAsync(u =>
                u.NomeUsuario.ToLower() == normalizado && (ignorarId == null || u.Id != ignorarId));
            if (emUso)
                erros["nomeUsuario"] = ["Esse nome de usuário já está em uso."];
        }

        if (papeisIds is null || papeisIds.Length == 0)
        {
            erros["papeisIds"] = ["Selecione pelo menos um papel."];
        }
        else
        {
            var existentes = await db.Papeis.CountAsync(p => papeisIds.Contains(p.Id));
            if (existentes != papeisIds.Distinct().Count())
                erros["papeisIds"] = ["Um ou mais papéis informados não existem."];
        }

        if (responsavelId is not null && !await db.Responsaveis.AnyAsync(r => r.Id == responsavelId))
            erros["responsavelId"] = ["O responsável informado não existe."];

        return erros;
    }

    private static UsuarioResponse ParaResponse(Usuario u) => new(
        u.Id,
        u.Nome,
        u.NomeUsuario,
        u.Ativo,
        u.Papeis.Select(p => p.Nome).OrderBy(n => n).ToArray(),
        u.ResponsavelId,
        u.Responsavel?.Nome
    );
}
