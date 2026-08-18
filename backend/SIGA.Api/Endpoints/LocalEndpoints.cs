using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SIGA.Api.Data;
using SIGA.Api.Domain;
using SIGA.Api.DTOs;

namespace SIGA.Api.Endpoints;

public static class LocalEndpoints
{
    private const int TamanhoPaginaMaximo = 100;

    public static void MapLocalEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/locais").RequireAuthorization();

        grupo.MapGet("/", ListarAsync);
        grupo.MapGet("/{id:int}", ObterPorIdAsync);
        grupo.MapPost("/", CriarAsync);
        grupo.MapPut("/{id:int}", AtualizarAsync);
        grupo.MapDelete("/{id:int}", RemoverAsync);
    }

    private static async Task<Ok<PagedResult<LocalResponse>>> ListarAsync(
        SigaDbContext db, string? nome, int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, TamanhoPaginaMaximo);

        var query = db.Locais.AsNoTracking().AsQueryable();

        // Filtro tipado e condicional: só entra na query se foi informado.
        if (!string.IsNullOrWhiteSpace(nome))
        {
            var termo = nome.ToLower();
            query = query.Where(l => l.Nome.ToLower().Contains(termo));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(l => l.Nome)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LocalResponse(l.Id, l.Nome, l.Descricao))
            .ToListAsync();

        return TypedResults.Ok(new PagedResult<LocalResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    private static async Task<Results<Ok<LocalResponse>, NotFound>> ObterPorIdAsync(SigaDbContext db, int id)
    {
        var local = await db.Locais.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
        if (local is null) return TypedResults.NotFound();

        return TypedResults.Ok(new LocalResponse(local.Id, local.Nome, local.Descricao));
    }

    private static async Task<Results<Created<LocalResponse>, ValidationProblem, Conflict<string>>> CriarAsync(
        SigaDbContext db, LocalRequest request)
    {
        var erros = Validar(request);
        if (erros.Count > 0) return TypedResults.ValidationProblem(erros);

        if (await NomeEmUsoAsync(db, request.Nome))
            return TypedResults.Conflict($"Já existe um local chamado \"{request.Nome}\".");

        var local = new Local { Nome = request.Nome.Trim(), Descricao = request.Descricao };
        db.Locais.Add(local);
        await db.SaveChangesAsync();

        var response = new LocalResponse(local.Id, local.Nome, local.Descricao);
        return TypedResults.Created($"/api/locais/{local.Id}", response);
    }

    private static async Task<Results<Ok<LocalResponse>, NotFound, ValidationProblem, Conflict<string>>> AtualizarAsync(
        SigaDbContext db, int id, LocalRequest request)
    {
        var erros = Validar(request);
        if (erros.Count > 0) return TypedResults.ValidationProblem(erros);

        var local = await db.Locais.FirstOrDefaultAsync(l => l.Id == id);
        if (local is null) return TypedResults.NotFound();

        if (await NomeEmUsoAsync(db, request.Nome, ignorarId: id))
            return TypedResults.Conflict($"Já existe um local chamado \"{request.Nome}\".");

        local.Nome = request.Nome.Trim();
        local.Descricao = request.Descricao;
        await db.SaveChangesAsync();

        return TypedResults.Ok(new LocalResponse(local.Id, local.Nome, local.Descricao));
    }

    private static async Task<Results<NoContent, NotFound, Conflict<string>>> RemoverAsync(SigaDbContext db, int id)
    {
        var local = await db.Locais.FirstOrDefaultAsync(l => l.Id == id);
        if (local is null) return TypedResults.NotFound();

        db.Locais.Remove(local);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // `local` não tem soft delete no modelo — a restrição de FK (Restrict)
            // em equipamento.local_id é quem impede a remoção nesse caso.
            return TypedResults.Conflict("Não é possível remover: há equipamentos vinculados a este local.");
        }

        return TypedResults.NoContent();
    }

    private static Task<bool> NomeEmUsoAsync(SigaDbContext db, string nome, int? ignorarId = null)
    {
        var nomeNormalizado = nome.Trim().ToLower();
        return db.Locais.AnyAsync(l =>
            l.Nome.ToLower() == nomeNormalizado && (ignorarId == null || l.Id != ignorarId));
    }

    private static Dictionary<string, string[]> Validar(LocalRequest request)
    {
        var erros = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Nome))
            erros["nome"] = ["Informe o nome do local."];
        else if (request.Nome.Trim().Length > 100)
            erros["nome"] = ["O nome pode ter no máximo 100 caracteres."];

        if (request.Descricao?.Length > 255)
            erros["descricao"] = ["A descrição pode ter no máximo 255 caracteres."];

        return erros;
    }
}
