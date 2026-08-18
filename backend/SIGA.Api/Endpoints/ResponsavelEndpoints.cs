using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SIGA.Api.Data;
using SIGA.Api.Domain;
using SIGA.Api.DTOs;

namespace SIGA.Api.Endpoints;

public static class ResponsavelEndpoints
{
    private const int TamanhoPaginaMaximo = 100;

    public static void MapResponsavelEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/responsaveis").RequireAuthorization();

        grupo.MapGet("/", ListarAsync);
        grupo.MapGet("/{id:int}", ObterPorIdAsync);
        grupo.MapPost("/", CriarAsync);
        grupo.MapPut("/{id:int}", AtualizarAsync);
        grupo.MapPost("/{id:int}/desativar", DesativarAsync);
        grupo.MapPost("/{id:int}/ativar", AtivarAsync);
    }

    private static async Task<Ok<PagedResult<ResponsavelResponse>>> ListarAsync(
        SigaDbContext db, string? nome, StatusResponsavel? status, int? localId, int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, TamanhoPaginaMaximo);

        var query = db.Responsaveis.AsNoTracking().AsQueryable();

        // Lista fechada de filtros: nome, status e local — nada genérico.
        if (!string.IsNullOrWhiteSpace(nome))
        {
            var termo = nome.ToLower();
            query = query.Where(r => r.Nome.ToLower().Contains(termo));
        }

        if (status is not null)
            query = query.Where(r => r.Status == status);

        if (localId is not null)
            query = query.Where(r => r.LocalId == localId);

        var totalCount = await query.CountAsync();

        // Select com método C# não é traduzível para SQL — busca as entidades
        // (com o Local já incluído) e só então mapeia para DTO em memória.
        var entidades = await query
            .Include(r => r.Local)
            .OrderBy(r => r.Nome)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = entidades.Select(ParaResponse).ToList();

        return TypedResults.Ok(new PagedResult<ResponsavelResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    private static async Task<Results<Ok<ResponsavelResponse>, NotFound>> ObterPorIdAsync(SigaDbContext db, int id)
    {
        var responsavel = await db.Responsaveis.AsNoTracking()
            .Include(r => r.Local)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (responsavel is null) return TypedResults.NotFound();

        return TypedResults.Ok(ParaResponse(responsavel));
    }

    private static async Task<Results<Created<ResponsavelResponse>, ValidationProblem>> CriarAsync(
        SigaDbContext db, ResponsavelRequest request)
    {
        var erros = await ValidarAsync(db, request);
        if (erros.Count > 0) return TypedResults.ValidationProblem(erros);

        var responsavel = new Responsavel
        {
            Nome = request.Nome.Trim(),
            Cargo = request.Cargo,
            LocalId = request.LocalId,
            Contato = request.Contato,
            Observacao = request.Observacao,
            Status = StatusResponsavel.Ativo,
        };
        db.Responsaveis.Add(responsavel);
        await db.SaveChangesAsync();

        await db.Entry(responsavel).Reference(r => r.Local).LoadAsync();

        return TypedResults.Created($"/api/responsaveis/{responsavel.Id}", ParaResponse(responsavel));
    }

    private static async Task<Results<Ok<ResponsavelResponse>, NotFound, ValidationProblem>> AtualizarAsync(
        SigaDbContext db, int id, ResponsavelRequest request)
    {
        var erros = await ValidarAsync(db, request);
        if (erros.Count > 0) return TypedResults.ValidationProblem(erros);

        var responsavel = await db.Responsaveis.FirstOrDefaultAsync(r => r.Id == id);
        if (responsavel is null) return TypedResults.NotFound();

        responsavel.Nome = request.Nome.Trim();
        responsavel.Cargo = request.Cargo;
        responsavel.LocalId = request.LocalId;
        responsavel.Contato = request.Contato;
        responsavel.Observacao = request.Observacao;
        await db.SaveChangesAsync();

        await db.Entry(responsavel).Reference(r => r.Local).LoadAsync();

        return TypedResults.Ok(ParaResponse(responsavel));
    }

    private static async Task<Results<Ok<ResponsavelResponse>, NotFound>> DesativarAsync(SigaDbContext db, int id) =>
        await MudarStatusAsync(db, id, StatusResponsavel.Inativo);

    private static async Task<Results<Ok<ResponsavelResponse>, NotFound>> AtivarAsync(SigaDbContext db, int id) =>
        await MudarStatusAsync(db, id, StatusResponsavel.Ativo);

    private static async Task<Results<Ok<ResponsavelResponse>, NotFound>> MudarStatusAsync(
        SigaDbContext db, int id, StatusResponsavel novoStatus)
    {
        var responsavel = await db.Responsaveis.Include(r => r.Local).FirstOrDefaultAsync(r => r.Id == id);
        if (responsavel is null) return TypedResults.NotFound();

        responsavel.Status = novoStatus;
        await db.SaveChangesAsync();

        return TypedResults.Ok(ParaResponse(responsavel));
    }

    private static async Task<Dictionary<string, string[]>> ValidarAsync(SigaDbContext db, ResponsavelRequest request)
    {
        var erros = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Nome))
            erros["nome"] = ["Informe o nome do responsável."];
        else if (request.Nome.Trim().Length > 120)
            erros["nome"] = ["O nome pode ter no máximo 120 caracteres."];

        if (request.LocalId is not null && !await db.Locais.AnyAsync(l => l.Id == request.LocalId))
            erros["localId"] = ["O local informado não existe."];

        return erros;
    }

    private static ResponsavelResponse ParaResponse(Responsavel r) => new(
        r.Id, r.Nome, r.Cargo, r.LocalId, r.Local?.Nome, r.Contato, r.Status.ToString(), r.Observacao);
}
