using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SIGA.Api.Data;
using SIGA.Api.Domain;
using SIGA.Api.DTOs;
using SIGA.Api.Services;

namespace SIGA.Api.Endpoints;

public static class LicencaEndpoints
{
    private const int TamanhoPaginaMaximo = 100;

    public static void MapLicencaEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/licencas").RequireAuthorization();

        grupo.MapGet("/", ListarAsync);
        grupo.MapGet("/{id:int}", ObterPorIdAsync);
        grupo.MapPost("/", CriarAsync);
        grupo.MapPut("/{id:int}", AtualizarAsync);
        grupo.MapDelete("/{id:int}", RemoverAsync);
    }

    private static async Task<Ok<PagedResult<LicencaResponse>>> ListarAsync(
        SigaDbContext db, CriptografiaLicenca criptografia, int? equipamentoId, int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, TamanhoPaginaMaximo);

        var query = db.Licencas.AsNoTracking().AsQueryable();

        if (equipamentoId is not null)
            query = query.Where(l => l.EquipamentoId == equipamentoId);

        var totalCount = await query.CountAsync();

        var entidades = await query
            .Include(l => l.Equipamento)
            .Include(l => l.NotaFiscal)
            .OrderByDescending(l => l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = entidades.Select(l => ParaResponse(l, criptografia)).ToList();

        return TypedResults.Ok(new PagedResult<LicencaResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    private static async Task<Results<Ok<LicencaResponse>, NotFound>> ObterPorIdAsync(
        SigaDbContext db, CriptografiaLicenca criptografia, int id)
    {
        var licenca = await db.Licencas.AsNoTracking()
            .Include(l => l.Equipamento)
            .Include(l => l.NotaFiscal)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (licenca is null) return TypedResults.NotFound();

        return TypedResults.Ok(ParaResponse(licenca, criptografia));
    }

    private static async Task<Results<Created<LicencaResponse>, ValidationProblem>> CriarAsync(
        SigaDbContext db, CriptografiaLicenca criptografia, LicencaRequest request)
    {
        var erros = await ValidarAsync(db, request);
        if (erros.Count > 0) return TypedResults.ValidationProblem(erros);

        var licenca = new Licenca
        {
            EquipamentoId = request.EquipamentoId,
            Produto = request.Produto.Trim(),
            ChaveCriptografada = criptografia.Criptografar(request.Chave),
            Tipo = request.Tipo,
            Observacao = request.Observacao,
            NotaFiscalId = request.NotaFiscalId,
        };
        db.Licencas.Add(licenca);
        await db.SaveChangesAsync();

        await db.Entry(licenca).Reference(l => l.Equipamento).LoadAsync();
        await db.Entry(licenca).Reference(l => l.NotaFiscal).LoadAsync();

        return TypedResults.Created($"/api/licencas/{licenca.Id}", ParaResponse(licenca, criptografia));
    }

    private static async Task<Results<Ok<LicencaResponse>, NotFound, ValidationProblem>> AtualizarAsync(
        SigaDbContext db, CriptografiaLicenca criptografia, int id, LicencaRequest request)
    {
        var licenca = await db.Licencas.FirstOrDefaultAsync(l => l.Id == id);
        if (licenca is null) return TypedResults.NotFound();

        var erros = await ValidarAsync(db, request);
        if (erros.Count > 0) return TypedResults.ValidationProblem(erros);

        licenca.EquipamentoId = request.EquipamentoId;
        licenca.Produto = request.Produto.Trim();
        licenca.Tipo = request.Tipo;
        licenca.Observacao = request.Observacao;
        licenca.NotaFiscalId = request.NotaFiscalId;

        // Só recriptografa se a chave realmente mudou — evita reescrever
        // o valor à toa (e gerar um nonce novo) numa edição que só mexeu
        // no produto ou na observação, por exemplo.
        var chaveAtual = criptografia.Descriptografar(licenca.ChaveCriptografada);
        if (chaveAtual != request.Chave)
            licenca.ChaveCriptografada = criptografia.Criptografar(request.Chave);

        await db.SaveChangesAsync();

        await db.Entry(licenca).Reference(l => l.Equipamento).LoadAsync();
        await db.Entry(licenca).Reference(l => l.NotaFiscal).LoadAsync();

        return TypedResults.Ok(ParaResponse(licenca, criptografia));
    }

    private static async Task<Results<NoContent, NotFound>> RemoverAsync(SigaDbContext db, int id)
    {
        var licenca = await db.Licencas.FirstOrDefaultAsync(l => l.Id == id);
        if (licenca is null) return TypedResults.NotFound();

        db.Licencas.Remove(licenca);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static async Task<Dictionary<string, string[]>> ValidarAsync(SigaDbContext db, LicencaRequest request)
    {
        var erros = new Dictionary<string, string[]>();

        if (!await db.Equipamentos.AnyAsync(e => e.Id == request.EquipamentoId))
            erros["equipamentoId"] = ["O equipamento informado não existe."];

        if (string.IsNullOrWhiteSpace(request.Produto))
            erros["produto"] = ["Informe o nome do produto."];
        else if (request.Produto.Trim().Length > 150)
            erros["produto"] = ["O produto pode ter no máximo 150 caracteres."];

        if (string.IsNullOrWhiteSpace(request.Chave))
            erros["chave"] = ["Informe a chave de licença."];

        if (request.NotaFiscalId is not null && !await db.NotasFiscais.AnyAsync(n => n.Id == request.NotaFiscalId))
            erros["notaFiscalId"] = ["A nota fiscal informada não existe."];

        return erros;
    }

    private static LicencaResponse ParaResponse(Licenca l, CriptografiaLicenca criptografia) => new(
        l.Id,
        l.EquipamentoId,
        $"{l.Equipamento.Marca} {l.Equipamento.Modelo}" + (l.Equipamento.Patrimonio is not null ? $" ({l.Equipamento.Patrimonio})" : ""),
        l.Produto,
        criptografia.Descriptografar(l.ChaveCriptografada),
        l.Tipo?.ToString(),
        l.Observacao,
        l.NotaFiscalId,
        l.NotaFiscal?.Numero
    );
}
