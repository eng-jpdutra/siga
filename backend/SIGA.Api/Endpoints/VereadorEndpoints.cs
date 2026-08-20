using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SIGA.Api.Data;
using SIGA.Api.Domain;
using SIGA.Api.DTOs;

namespace SIGA.Api.Endpoints;

public static class VereadorEndpoints
{
    private const int TamanhoPaginaMaximo = 100;

    public static void MapVereadorEndpoints(this IEndpointRouteBuilder app)
    {
        // Leitura pra quem estiver logado; escrita só pra Administrador —
        // Consulta nunca escreve (ver CLAUDE.md).
        var grupo = app.MapGroup("/api/vereadores").RequireAuthorization();

        grupo.MapGet("/", ListarAsync);
        grupo.MapGet("/{id:int}", ObterPorIdAsync);
        grupo.MapPost("/", CriarAsync).RequireAuthorization("SomenteAdministrador");
        grupo.MapPut("/{id:int}", AtualizarAsync).RequireAuthorization("SomenteAdministrador");
        grupo.MapPost("/{id:int}/desativar", DesativarAsync).RequireAuthorization("SomenteAdministrador");
        grupo.MapPost("/{id:int}/ativar", AtivarAsync).RequireAuthorization("SomenteAdministrador");
    }

    private static async Task<Ok<PagedResult<VereadorResponse>>> ListarAsync(
        SigaDbContext db, string? nome, bool? ativo, int? localId, int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, TamanhoPaginaMaximo);

        var query = db.Vereadores.AsNoTracking().AsQueryable();

        // Lista fechada de filtros: nome, ativo e local — nada genérico.
        if (!string.IsNullOrWhiteSpace(nome))
        {
            var termo = nome.ToLower();
            query = query.Where(v => v.Nome.ToLower().Contains(termo));
        }

        if (ativo is not null)
            query = query.Where(v => v.Ativo == ativo);

        if (localId is not null)
            query = query.Where(v => v.LocalId == localId);

        var totalCount = await query.CountAsync();

        var entidades = await query
            .Include(v => v.Local)
            .OrderBy(v => v.Nome)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = entidades.Select(ParaResponse).ToList();

        return TypedResults.Ok(new PagedResult<VereadorResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    private static async Task<Results<Ok<VereadorResponse>, NotFound>> ObterPorIdAsync(SigaDbContext db, int id)
    {
        var vereador = await db.Vereadores.AsNoTracking().Include(v => v.Local).FirstOrDefaultAsync(v => v.Id == id);
        if (vereador is null) return TypedResults.NotFound();

        return TypedResults.Ok(ParaResponse(vereador));
    }

    private static async Task<Results<Created<VereadorResponse>, ValidationProblem>> CriarAsync(
        SigaDbContext db, VereadorRequest request)
    {
        var erros = await ValidarAsync(db, request);
        if (erros.Count > 0) return TypedResults.ValidationProblem(erros);

        var vereador = new Vereador
        {
            Nome = request.Nome.Trim(),
            Partido = request.Partido,
            Contato = request.Contato,
            LocalId = request.LocalId,
            Ativo = true,
        };
        db.Vereadores.Add(vereador);
        await db.SaveChangesAsync();

        await db.Entry(vereador).Reference(v => v.Local).LoadAsync();

        return TypedResults.Created($"/api/vereadores/{vereador.Id}", ParaResponse(vereador));
    }

    private static async Task<Results<Ok<VereadorResponse>, NotFound, ValidationProblem>> AtualizarAsync(
        SigaDbContext db, int id, VereadorRequest request)
    {
        var erros = await ValidarAsync(db, request);
        if (erros.Count > 0) return TypedResults.ValidationProblem(erros);

        var vereador = await db.Vereadores.FirstOrDefaultAsync(v => v.Id == id);
        if (vereador is null) return TypedResults.NotFound();

        vereador.Nome = request.Nome.Trim();
        vereador.Partido = request.Partido;
        vereador.Contato = request.Contato;
        vereador.LocalId = request.LocalId;
        await db.SaveChangesAsync();

        await db.Entry(vereador).Reference(v => v.Local).LoadAsync();

        return TypedResults.Ok(ParaResponse(vereador));
    }

    private static async Task<Results<Ok<VereadorResponse>, NotFound>> DesativarAsync(SigaDbContext db, int id) =>
        await MudarStatusAsync(db, id, ativo: false);

    private static async Task<Results<Ok<VereadorResponse>, NotFound>> AtivarAsync(SigaDbContext db, int id) =>
        await MudarStatusAsync(db, id, ativo: true);

    private static async Task<Results<Ok<VereadorResponse>, NotFound>> MudarStatusAsync(
        SigaDbContext db, int id, bool ativo)
    {
        var vereador = await db.Vereadores.Include(v => v.Local).FirstOrDefaultAsync(v => v.Id == id);
        if (vereador is null) return TypedResults.NotFound();

        vereador.Ativo = ativo;
        await db.SaveChangesAsync();

        return TypedResults.Ok(ParaResponse(vereador));
    }

    private static async Task<Dictionary<string, string[]>> ValidarAsync(SigaDbContext db, VereadorRequest request)
    {
        var erros = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Nome))
            erros["nome"] = ["Informe o nome do vereador."];
        else if (request.Nome.Trim().Length > 120)
            erros["nome"] = ["O nome pode ter no máximo 120 caracteres."];

        if (request.LocalId is not null && !await db.Locais.AnyAsync(l => l.Id == request.LocalId))
            erros["localId"] = ["O local informado não existe."];

        return erros;
    }

    private static VereadorResponse ParaResponse(Vereador v) => new(
        v.Id, v.Nome, v.Partido, v.Contato, v.LocalId, v.Local?.Nome, v.Ativo);
}
