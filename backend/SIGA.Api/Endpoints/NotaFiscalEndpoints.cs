using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using SIGA.Api.Data;
using SIGA.Api.Domain;
using SIGA.Api.DTOs;
using SIGA.Api.Services;

namespace SIGA.Api.Endpoints;

public static class NotaFiscalEndpoints
{
    private const int TamanhoPaginaMaximo = 100;
    private const string Subpasta = "notas-fiscais";

    public static void MapNotaFiscalEndpoints(this IEndpointRouteBuilder app)
    {
        // Leitura pra quem estiver logado; escrita só pra Administrador —
        // Consulta nunca escreve (ver CLAUDE.md).
        var grupo = app.MapGroup("/api/notas-fiscais").RequireAuthorization();

        grupo.MapGet("/", ListarAsync);
        grupo.MapGet("/{id:int}", ObterPorIdAsync);
        grupo.MapPost("/", CriarAsync).DisableAntiforgery().RequireAuthorization("SomenteAdministrador");
        grupo.MapPut("/{id:int}", AtualizarAsync).DisableAntiforgery().RequireAuthorization("SomenteAdministrador");
        grupo.MapDelete("/{id:int}", RemoverAsync).RequireAuthorization("SomenteAdministrador");
        grupo.MapGet("/{id:int}/arquivo", BaixarArquivoAsync);
    }

    private static async Task<Ok<PagedResult<NotaFiscalResponse>>> ListarAsync(
        SigaDbContext db, string? numero, int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, TamanhoPaginaMaximo);

        var query = db.NotasFiscais.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(numero))
        {
            var termo = numero.ToLower();
            query = query.Where(n =>
                n.Numero.ToLower().Contains(termo) ||
                (n.Fornecedor != null && n.Fornecedor.ToLower().Contains(termo)));
        }

        var totalCount = await query.CountAsync();

        var entidades = await query
            .OrderByDescending(n => n.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = entidades.Select(ParaResponse).ToList();

        return TypedResults.Ok(new PagedResult<NotaFiscalResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    private static async Task<Results<Ok<NotaFiscalResponse>, NotFound>> ObterPorIdAsync(SigaDbContext db, int id)
    {
        var notaFiscal = await db.NotasFiscais.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
        if (notaFiscal is null) return TypedResults.NotFound();

        return TypedResults.Ok(ParaResponse(notaFiscal));
    }

    private static async Task<Results<Created<NotaFiscalResponse>, ValidationProblem>> CriarAsync(
        SigaDbContext db, ArmazenamentoArquivos armazenamento, [FromForm] NotaFiscalFormulario form)
    {
        var erros = Validar(form, armazenamento);
        if (erros.Count > 0) return TypedResults.ValidationProblem(erros);

        var notaFiscal = new NotaFiscal
        {
            Numero = form.Numero.Trim(),
            Fornecedor = form.Fornecedor,
            DataEmissao = form.DataEmissao,
            Valor = form.Valor,
            Observacao = form.Observacao,
        };

        if (form.Arquivo is not null)
            notaFiscal.ArquivoPath = await armazenamento.SalvarAsync(form.Arquivo, Subpasta);

        db.NotasFiscais.Add(notaFiscal);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/notas-fiscais/{notaFiscal.Id}", ParaResponse(notaFiscal));
    }

    private static async Task<Results<Ok<NotaFiscalResponse>, NotFound, ValidationProblem>> AtualizarAsync(
        SigaDbContext db, ArmazenamentoArquivos armazenamento, int id, [FromForm] NotaFiscalFormulario form)
    {
        var notaFiscal = await db.NotasFiscais.FirstOrDefaultAsync(n => n.Id == id);
        if (notaFiscal is null) return TypedResults.NotFound();

        var erros = Validar(form, armazenamento);
        if (erros.Count > 0) return TypedResults.ValidationProblem(erros);

        notaFiscal.Numero = form.Numero.Trim();
        notaFiscal.Fornecedor = form.Fornecedor;
        notaFiscal.DataEmissao = form.DataEmissao;
        notaFiscal.Valor = form.Valor;
        notaFiscal.Observacao = form.Observacao;

        // Só troca o arquivo se um novo foi enviado — sem isso, mantém o atual.
        if (form.Arquivo is not null)
        {
            armazenamento.Remover(notaFiscal.ArquivoPath);
            notaFiscal.ArquivoPath = await armazenamento.SalvarAsync(form.Arquivo, Subpasta);
        }

        await db.SaveChangesAsync();

        return TypedResults.Ok(ParaResponse(notaFiscal));
    }

    private static async Task<Results<NoContent, NotFound, Conflict<string>>> RemoverAsync(
        SigaDbContext db, ArmazenamentoArquivos armazenamento, int id)
    {
        var notaFiscal = await db.NotasFiscais.FirstOrDefaultAsync(n => n.Id == id);
        if (notaFiscal is null) return TypedResults.NotFound();

        db.NotasFiscais.Remove(notaFiscal);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return TypedResults.Conflict("Não é possível remover: há equipamentos vinculados a esta nota fiscal.");
        }

        armazenamento.Remover(notaFiscal.ArquivoPath);

        return TypedResults.NoContent();
    }

    private static async Task<Results<PhysicalFileHttpResult, NotFound>> BaixarArquivoAsync(
        SigaDbContext db, ArmazenamentoArquivos armazenamento, int id)
    {
        var notaFiscal = await db.NotasFiscais.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
        if (notaFiscal?.ArquivoPath is null || !armazenamento.Existe(notaFiscal.ArquivoPath))
            return TypedResults.NotFound();

        var caminhoCompleto = armazenamento.ObterCaminhoCompleto(notaFiscal.ArquivoPath);
        var contentType = ObterContentType(caminhoCompleto);
        var nomeParaDownload = $"nota-fiscal-{notaFiscal.Numero}{Path.GetExtension(caminhoCompleto)}";

        return TypedResults.PhysicalFile(caminhoCompleto, contentType, nomeParaDownload);
    }

    private static string ObterContentType(string caminho)
    {
        var provider = new FileExtensionContentTypeProvider();
        return provider.TryGetContentType(caminho, out var contentType) ? contentType : "application/octet-stream";
    }

    private static Dictionary<string, string[]> Validar(NotaFiscalFormulario form, ArmazenamentoArquivos armazenamento)
    {
        var erros = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(form.Numero))
            erros["numero"] = ["Informe o número da nota fiscal."];
        else if (form.Numero.Trim().Length > 50)
            erros["numero"] = ["O número pode ter no máximo 50 caracteres."];

        if (form.Arquivo is not null && !armazenamento.ArquivoValido(form.Arquivo, out var erroArquivo))
            erros["arquivo"] = [erroArquivo!];

        return erros;
    }

    private static NotaFiscalResponse ParaResponse(NotaFiscal n) => new(
        n.Id, n.Numero, n.Fornecedor, n.DataEmissao, n.Valor, n.Observacao, n.ArquivoPath is not null);
}
