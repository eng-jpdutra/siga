using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SIGA.Api.Data;
using SIGA.Api.Domain;
using SIGA.Api.DTOs;

namespace SIGA.Api.Endpoints;

public static class EquipamentoEndpoints
{
    private const int TamanhoPaginaMaximo = 100;

    public static void MapEquipamentoEndpoints(this IEndpointRouteBuilder app)
    {
        // Grupo inteiro exige só estar logado (leitura, papel Consulta
        // inclusive); rotas que alteram dado pedem também "SomenteAdministrador"
        // — Consulta nunca escreve (ver CLAUDE.md).
        var grupo = app.MapGroup("/api/equipamentos").RequireAuthorization();

        grupo.MapGet("/", ListarAsync);
        grupo.MapGet("/{id:int}", ObterPorIdAsync);
        grupo.MapPost("/", CriarAsync).RequireAuthorization("SomenteAdministrador");
        grupo.MapPut("/{id:int}", AtualizarAsync).RequireAuthorization("SomenteAdministrador");
        grupo.MapPost("/{id:int}/baixar", BaixarAsync).RequireAuthorization("SomenteAdministrador");
        grupo.MapPost("/{id:int}/reativar", ReativarAsync).RequireAuthorization("SomenteAdministrador");

        grupo.MapGet("/{id:int}/historico", ListarHistoricoAsync);
        grupo.MapPost("/{id:int}/historico", CriarHistoricoAsync).RequireAuthorization("SomenteAdministrador");

        // Anotações técnicas — ao contrário do histórico, podem ser editadas
        // e removidas (não é um diário de eventos, ver Domain/Configuracao.cs).
        grupo.MapGet("/{id:int}/configuracoes", ListarConfiguracoesAsync);
        grupo.MapPost("/{id:int}/configuracoes", CriarConfiguracaoAsync).RequireAuthorization("SomenteAdministrador");
        grupo.MapPut("/{id:int}/configuracoes/{configuracaoId:int}", AtualizarConfiguracaoAsync).RequireAuthorization("SomenteAdministrador");
        grupo.MapDelete("/{id:int}/configuracoes/{configuracaoId:int}", RemoverConfiguracaoAsync).RequireAuthorization("SomenteAdministrador");
    }

    // ---------- Listagem (grade) ----------

    private static async Task<Ok<PagedResult<EquipamentoResumoResponse>>> ListarAsync(
        SigaDbContext db, string? termo, TipoEquipamento? tipo, StatusEquipamento? status,
        int? localId, int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, TamanhoPaginaMaximo);

        var query = db.Equipamentos.AsNoTracking().AsQueryable();

        // Lista fechada de filtros — todos batem em colunas indexadas
        // (Patrimonio, NumeroSerie, Marca, Modelo, Tipo, Status). Os campos
        // específicos de tipo (dentro de `Detalhes`) não são filtráveis.
        if (!string.IsNullOrWhiteSpace(termo))
        {
            var termoNormalizado = termo.ToLower();
            query = query.Where(e =>
                (e.Patrimonio != null && e.Patrimonio.ToLower().Contains(termoNormalizado)) ||
                (e.NumeroSerie != null && e.NumeroSerie.ToLower().Contains(termoNormalizado)) ||
                e.Marca.ToLower().Contains(termoNormalizado) ||
                e.Modelo.ToLower().Contains(termoNormalizado));
        }

        if (tipo is not null) query = query.Where(e => e.Tipo == tipo);
        if (status is not null) query = query.Where(e => e.Status == status);
        if (localId is not null) query = query.Where(e => e.LocalId == localId);

        var totalCount = await query.CountAsync();

        var entidades = await query
            .Include(e => e.Local)
            .OrderByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = entidades.Select(e => new EquipamentoResumoResponse(
            e.Id, e.Tipo.ToString(), e.Patrimonio, e.NumeroSerie, e.Marca, e.Modelo, e.Status.ToString(),
            e.LocalId, e.Local?.Nome
        )).ToList();

        return TypedResults.Ok(new PagedResult<EquipamentoResumoResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    // ---------- Detalhe ----------

    private static async Task<Results<Ok<EquipamentoResponse>, NotFound>> ObterPorIdAsync(SigaDbContext db, int id)
    {
        var equipamento = await CarregarAsync(db, id, semRastreamento: true);
        if (equipamento is null) return TypedResults.NotFound();

        return TypedResults.Ok(ParaResponse(equipamento));
    }

    // ---------- Criar ----------

    private static async Task<Results<Created<EquipamentoResponse>, ValidationProblem>> CriarAsync(
        SigaDbContext db, EquipamentoRequest request)
    {
        var erros = await ValidarAsync(db, request);
        if (erros.Count > 0) return TypedResults.ValidationProblem(erros);

        var equipamento = new Equipamento();
        PreencherCamposComuns(equipamento, request);
        equipamento.Tipo = request.Tipo;
        equipamento.Status = StatusEquipamento.Ativo;
        equipamento.CriadoEm = DateTime.UtcNow;

        db.Equipamentos.Add(equipamento);
        await db.SaveChangesAsync();

        var criado = await CarregarAsync(db, equipamento.Id, semRastreamento: true);
        return TypedResults.Created($"/api/equipamentos/{equipamento.Id}", ParaResponse(criado!));
    }

    // ---------- Atualizar ----------

    private static async Task<Results<Ok<EquipamentoResponse>, NotFound, ValidationProblem>> AtualizarAsync(
        SigaDbContext db, HttpContext http, int id, EquipamentoRequest request)
    {
        var atual = await db.Equipamentos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (atual is null) return TypedResults.NotFound();

        if (atual.Tipo != request.Tipo)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["tipo"] = ["Não é possível mudar o tipo de um equipamento já cadastrado."],
            });
        }

        var erros = await ValidarAsync(db, request, ignorarId: id);
        if (erros.Count > 0) return TypedResults.ValidationProblem(erros);

        var equipamento = await CarregarAsync(db, id);
        if (equipamento is null) return TypedResults.NotFound();

        var localAnteriorId = equipamento.LocalId;

        PreencherCamposComuns(equipamento, request);
        equipamento.AtualizadoEm = DateTime.UtcNow;

        await RegistrarTrocaLocalAsync(db, http, equipamento, localAnteriorId);

        await db.SaveChangesAsync();

        var atualizado = await CarregarAsync(db, id, semRastreamento: true);
        return TypedResults.Ok(ParaResponse(atualizado!));
    }

    // ---------- Baixar / reativar (soft delete) ----------

    private static Task<Results<Ok<EquipamentoResponse>, NotFound>> BaixarAsync(
        SigaDbContext db, HttpContext http, int id) =>
        MudarStatusAsync(db, http, id, StatusEquipamento.Baixado, "Equipamento baixado.");

    private static Task<Results<Ok<EquipamentoResponse>, NotFound>> ReativarAsync(
        SigaDbContext db, HttpContext http, int id) =>
        MudarStatusAsync(db, http, id, StatusEquipamento.Ativo, "Equipamento reativado.");

    private static async Task<Results<Ok<EquipamentoResponse>, NotFound>> MudarStatusAsync(
        SigaDbContext db, HttpContext http, int id, StatusEquipamento novoStatus, string descricaoHistorico)
    {
        var equipamento = await db.Equipamentos.FirstOrDefaultAsync(e => e.Id == id);
        if (equipamento is null) return TypedResults.NotFound();

        equipamento.Status = novoStatus;
        equipamento.AtualizadoEm = DateTime.UtcNow;

        db.Historicos.Add(new Historico
        {
            EquipamentoId = id,
            Tipo = TipoHistorico.Outro,
            Data = DateOnly.FromDateTime(DateTime.UtcNow),
            Descricao = descricaoHistorico,
            RegistradoPor = NomeUsuarioLogado(http),
            RegistradoEm = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();

        var atualizado = await CarregarAsync(db, id, semRastreamento: true);
        return TypedResults.Ok(ParaResponse(atualizado!));
    }

    // ---------- Histórico ----------

    private static async Task<Results<Ok<List<HistoricoResponse>>, NotFound>> ListarHistoricoAsync(
        SigaDbContext db, int id)
    {
        if (!await db.Equipamentos.AnyAsync(e => e.Id == id)) return TypedResults.NotFound();

        var historicos = await db.Historicos.AsNoTracking()
            .Where(h => h.EquipamentoId == id)
            .OrderByDescending(h => h.Data).ThenByDescending(h => h.Id)
            .Select(h => new HistoricoResponse(h.Id, h.Tipo.ToString(), h.Data, h.Descricao, h.RegistradoPor, h.RegistradoEm))
            .ToListAsync();

        return TypedResults.Ok(historicos);
    }

    private static async Task<Results<Created<HistoricoResponse>, NotFound, ValidationProblem>> CriarHistoricoAsync(
        SigaDbContext db, HttpContext http, int id, HistoricoRequest request)
    {
        if (!await db.Equipamentos.AnyAsync(e => e.Id == id)) return TypedResults.NotFound();

        if (string.IsNullOrWhiteSpace(request.Descricao))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["descricao"] = ["Descreva o que foi feito."],
            });
        }

        var historico = new Historico
        {
            EquipamentoId = id,
            Tipo = request.Tipo,
            Data = request.Data,
            Descricao = request.Descricao.Trim(),
            RegistradoPor = NomeUsuarioLogado(http),
            RegistradoEm = DateTime.UtcNow,
        };
        db.Historicos.Add(historico);
        await db.SaveChangesAsync();

        var response = new HistoricoResponse(
            historico.Id, historico.Tipo.ToString(), historico.Data, historico.Descricao,
            historico.RegistradoPor, historico.RegistradoEm);

        return TypedResults.Created($"/api/equipamentos/{id}/historico", response);
    }

    // ---------- Configurações (anotações técnicas) ----------

    private static async Task<Results<Ok<List<ConfiguracaoResponse>>, NotFound>> ListarConfiguracoesAsync(
        SigaDbContext db, int id)
    {
        if (!await db.Equipamentos.AnyAsync(e => e.Id == id)) return TypedResults.NotFound();

        var configuracoes = await db.Configuracoes.AsNoTracking()
            .Where(c => c.EquipamentoId == id)
            .OrderByDescending(c => c.Id)
            .Select(c => new ConfiguracaoResponse(c.Id, c.Titulo, c.Conteudo, c.CriadoEm, c.AtualizadoEm))
            .ToListAsync();

        return TypedResults.Ok(configuracoes);
    }

    private static async Task<Results<Created<ConfiguracaoResponse>, NotFound, ValidationProblem>> CriarConfiguracaoAsync(
        SigaDbContext db, int id, ConfiguracaoRequest request)
    {
        if (!await db.Equipamentos.AnyAsync(e => e.Id == id)) return TypedResults.NotFound();

        var erros = ValidarConfiguracao(request);
        if (erros.Count > 0) return TypedResults.ValidationProblem(erros);

        var configuracao = new Configuracao
        {
            EquipamentoId = id,
            Titulo = request.Titulo.Trim(),
            Conteudo = request.Conteudo,
            CriadoEm = DateTime.UtcNow,
        };
        db.Configuracoes.Add(configuracao);
        await db.SaveChangesAsync();

        var response = new ConfiguracaoResponse(
            configuracao.Id, configuracao.Titulo, configuracao.Conteudo, configuracao.CriadoEm, configuracao.AtualizadoEm);

        return TypedResults.Created($"/api/equipamentos/{id}/configuracoes/{configuracao.Id}", response);
    }

    private static async Task<Results<Ok<ConfiguracaoResponse>, NotFound, ValidationProblem>> AtualizarConfiguracaoAsync(
        SigaDbContext db, int id, int configuracaoId, ConfiguracaoRequest request)
    {
        var erros = ValidarConfiguracao(request);
        if (erros.Count > 0) return TypedResults.ValidationProblem(erros);

        var configuracao = await db.Configuracoes.FirstOrDefaultAsync(c => c.Id == configuracaoId && c.EquipamentoId == id);
        if (configuracao is null) return TypedResults.NotFound();

        configuracao.Titulo = request.Titulo.Trim();
        configuracao.Conteudo = request.Conteudo;
        configuracao.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return TypedResults.Ok(new ConfiguracaoResponse(
            configuracao.Id, configuracao.Titulo, configuracao.Conteudo, configuracao.CriadoEm, configuracao.AtualizadoEm));
    }

    private static async Task<Results<NoContent, NotFound>> RemoverConfiguracaoAsync(SigaDbContext db, int id, int configuracaoId)
    {
        var configuracao = await db.Configuracoes.FirstOrDefaultAsync(c => c.Id == configuracaoId && c.EquipamentoId == id);
        if (configuracao is null) return TypedResults.NotFound();

        db.Configuracoes.Remove(configuracao);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static Dictionary<string, string[]> ValidarConfiguracao(ConfiguracaoRequest request)
    {
        var erros = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Titulo))
            erros["titulo"] = ["Informe um título."];
        else if (request.Titulo.Trim().Length > 150)
            erros["titulo"] = ["O título pode ter no máximo 150 caracteres."];

        if (string.IsNullOrWhiteSpace(request.Conteudo))
            erros["conteudo"] = ["Escreva o conteúdo da anotação."];

        return erros;
    }

    // ---------- Helpers ----------

    private static async Task<Equipamento?> CarregarAsync(SigaDbContext db, int id, bool semRastreamento = false)
    {
        var query = db.Equipamentos.Include(e => e.Local).Include(e => e.NotaFiscal).AsQueryable();
        if (semRastreamento) query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(e => e.Id == id);
    }

    private static void PreencherCamposComuns(Equipamento equipamento, EquipamentoRequest request)
    {
        equipamento.Patrimonio = string.IsNullOrWhiteSpace(request.Patrimonio) ? null : request.Patrimonio.Trim();
        equipamento.NumeroSerie = string.IsNullOrWhiteSpace(request.NumeroSerie) ? null : request.NumeroSerie.Trim();
        equipamento.Marca = request.Marca.Trim();
        equipamento.Modelo = request.Modelo.Trim();
        equipamento.LocalId = request.LocalId;
        equipamento.NotaFiscalId = request.NotaFiscalId;
        equipamento.EnderecoMac = string.IsNullOrWhiteSpace(request.EnderecoMac) ? null : request.EnderecoMac.Trim();
        equipamento.EnderecoIp = string.IsNullOrWhiteSpace(request.EnderecoIp) ? null : request.EnderecoIp.Trim();
        equipamento.Detalhes = request.Detalhes is { Count: > 0 } ? request.Detalhes : null;
        equipamento.AnoAquisicao = request.AnoAquisicao;
        equipamento.GarantiaAte = request.GarantiaAte;
        equipamento.Observacao = request.Observacao;
    }

    // O texto das trocas é gerado aqui, de forma padronizada — nunca digitado
    // livre pelo operador (ver regra do "historico" no CLAUDE.md).
    private static async Task RegistrarTrocaLocalAsync(
        SigaDbContext db, HttpContext http, Equipamento equipamento, int? localAnteriorId)
    {
        if (equipamento.LocalId == localAnteriorId) return;

        var registradoPor = NomeUsuarioLogado(http);
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        var nomeAnterior = localAnteriorId is null ? "sem local definido" :
            (await db.Locais.AsNoTracking().FirstOrDefaultAsync(l => l.Id == localAnteriorId))?.Nome ?? "local removido";
        var nomeNovo = equipamento.LocalId is null ? "sem local definido" :
            (await db.Locais.AsNoTracking().FirstOrDefaultAsync(l => l.Id == equipamento.LocalId))?.Nome ?? "local removido";

        db.Historicos.Add(new Historico
        {
            EquipamentoId = equipamento.Id,
            Tipo = TipoHistorico.MudancaLocal,
            Data = hoje,
            Descricao = $"Local alterado de \"{nomeAnterior}\" para \"{nomeNovo}\".",
            RegistradoPor = registradoPor,
            RegistradoEm = DateTime.UtcNow,
        });
    }

    private static string? NomeUsuarioLogado(HttpContext http) => http.User.FindFirstValue(ClaimTypes.Name);

    private static async Task<Dictionary<string, string[]>> ValidarAsync(
        SigaDbContext db, EquipamentoRequest request, int? ignorarId = null)
    {
        var erros = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Marca))
            erros["marca"] = ["Informe a marca."];
        else if (request.Marca.Trim().Length > 100)
            erros["marca"] = ["A marca pode ter no máximo 100 caracteres."];

        if (string.IsNullOrWhiteSpace(request.Modelo))
            erros["modelo"] = ["Informe o modelo."];
        else if (request.Modelo.Trim().Length > 100)
            erros["modelo"] = ["O modelo pode ter no máximo 100 caracteres."];

        if (!string.IsNullOrWhiteSpace(request.Patrimonio))
        {
            var normalizado = request.Patrimonio.Trim().ToLower();
            var emUso = await db.Equipamentos.AnyAsync(e =>
                e.Patrimonio != null && e.Patrimonio.ToLower() == normalizado && (ignorarId == null || e.Id != ignorarId));
            if (emUso) erros["patrimonio"] = ["Já existe um equipamento com esse patrimônio."];
        }

        if (!string.IsNullOrWhiteSpace(request.NumeroSerie))
        {
            var normalizado = request.NumeroSerie.Trim().ToLower();
            var emUso = await db.Equipamentos.AnyAsync(e =>
                e.NumeroSerie != null && e.NumeroSerie.ToLower() == normalizado && (ignorarId == null || e.Id != ignorarId));
            if (emUso) erros["numeroSerie"] = ["Já existe um equipamento com esse número de série."];
        }

        if (!string.IsNullOrWhiteSpace(request.EnderecoMac))
        {
            var normalizado = request.EnderecoMac.Trim().ToLower();
            var emUso = await db.Equipamentos.AnyAsync(e =>
                e.EnderecoMac != null && e.EnderecoMac.ToLower() == normalizado && (ignorarId == null || e.Id != ignorarId));
            if (emUso) erros["enderecoMac"] = ["Já existe um equipamento com esse endereço MAC."];
        }

        if (request.LocalId is not null && !await db.Locais.AnyAsync(l => l.Id == request.LocalId))
            erros["localId"] = ["O local informado não existe."];

        if (request.NotaFiscalId is not null && !await db.NotasFiscais.AnyAsync(n => n.Id == request.NotaFiscalId))
            erros["notaFiscalId"] = ["A nota fiscal informada não existe."];

        return erros;
    }

    private static EquipamentoResponse ParaResponse(Equipamento e) => new(
        e.Id, e.Tipo.ToString(), e.Patrimonio, e.NumeroSerie, e.Marca, e.Modelo,
        e.LocalId, e.Local?.Nome, e.NotaFiscalId, e.NotaFiscal?.Numero,
        e.EnderecoMac, e.EnderecoIp, e.Detalhes,
        e.Status.ToString(), e.AnoAquisicao, e.GarantiaAte, e.Observacao, e.CriadoEm, e.AtualizadoEm
    );
}
