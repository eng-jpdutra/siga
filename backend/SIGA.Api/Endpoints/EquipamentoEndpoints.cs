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
        var grupo = app.MapGroup("/api/equipamentos").RequireAuthorization();

        grupo.MapGet("/", ListarAsync);
        grupo.MapGet("/{id:int}", ObterPorIdAsync);
        grupo.MapPost("/", CriarAsync);
        grupo.MapPut("/{id:int}", AtualizarAsync);
        grupo.MapPost("/{id:int}/baixar", BaixarAsync);
        grupo.MapPost("/{id:int}/reativar", ReativarAsync);

        grupo.MapGet("/{id:int}/historico", ListarHistoricoAsync);
        grupo.MapPost("/{id:int}/historico", CriarHistoricoAsync);
    }

    // ---------- Listagem (grade) ----------

    private static async Task<Ok<PagedResult<EquipamentoResumoResponse>>> ListarAsync(
        SigaDbContext db, string? termo, TipoEquipamento? tipo, StatusEquipamento? status,
        int? localId, int? responsavelId, int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, TamanhoPaginaMaximo);

        var query = db.Equipamentos.AsNoTracking().AsQueryable();

        // Lista fechada de filtros — todos batem em colunas indexadas
        // (Patrimonio, NumeroSerie, Marca, Modelo, Tipo, Status).
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
        if (responsavelId is not null) query = query.Where(e => e.ResponsavelId == responsavelId);

        var totalCount = await query.CountAsync();

        var entidades = await query
            .Include(e => e.Local)
            .Include(e => e.Responsavel)
            .OrderByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = entidades.Select(e => new EquipamentoResumoResponse(
            e.Id, e.Tipo.ToString(), e.Patrimonio, e.NumeroSerie, e.Marca, e.Modelo, e.Status.ToString(),
            e.LocalId, e.Local?.Nome, e.ResponsavelId, e.Responsavel?.Nome
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
        var equipamento = await CarregarCompletoAsync(db, id);
        if (equipamento is null) return TypedResults.NotFound();

        return TypedResults.Ok(ParaResponse(equipamento));
    }

    // ---------- Criar ----------

    private static async Task<Results<Created<EquipamentoResponse>, ValidationProblem>> CriarAsync(
        SigaDbContext db, EquipamentoRequest request)
    {
        var erros = await ValidarAsync(db, request);
        if (erros.Count > 0) return TypedResults.ValidationProblem(erros);

        Equipamento equipamento = request.Tipo switch
        {
            TipoEquipamento.Computador => new Computador
            {
                Subtipo = request.SubtipoComputador,
                SistemaOperacional = request.SistemaOperacional,
                RamGb = request.RamGb,
                ArmazenamentoGb = request.ArmazenamentoGb,
                TipoArmazenamento = request.TipoArmazenamento,
                Processador = request.Processador,
            },
            TipoEquipamento.Impressora => new Impressora
            {
                TipoImpressao = request.TipoImpressao,
                Colorida = request.Colorida,
                Conexao = request.Conexao,
                ContadorPaginas = request.ContadorPaginas,
            },
            TipoEquipamento.DispositivoRede => new DispositivoRede
            {
                Subtipo = request.SubtipoDispositivoRede,
                EnderecoIp = request.EnderecoIp,
                EnderecoMac = request.EnderecoMac,
                NumPortas = request.NumPortas,
                VersaoFirmware = request.VersaoFirmware,
            },
            _ => new Equipamento(),
        };

        PreencherCamposComuns(equipamento, request);
        equipamento.Tipo = request.Tipo;
        equipamento.Status = StatusEquipamento.Ativo;
        equipamento.CriadoEm = DateTime.UtcNow;

        db.Add(equipamento);
        await db.SaveChangesAsync();

        var criado = await CarregarCompletoAsync(db, equipamento.Id);
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

        var equipamento = await CarregarParaEdicaoAsync(db, id, request.Tipo);
        if (equipamento is null) return TypedResults.NotFound();

        var localAnteriorId = equipamento.LocalId;
        var responsavelAnteriorId = equipamento.ResponsavelId;

        PreencherCamposComuns(equipamento, request);
        AplicarCamposEspecificos(equipamento, request);
        equipamento.AtualizadoEm = DateTime.UtcNow;

        await RegistrarTrocasAsync(db, http, equipamento, localAnteriorId, responsavelAnteriorId);

        await db.SaveChangesAsync();

        var atualizado = await CarregarCompletoAsync(db, id);
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

        var atualizado = await CarregarCompletoAsync(db, id);
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

    // ---------- Helpers ----------

    private static async Task<Equipamento?> CarregarCompletoAsync(SigaDbContext db, int id)
    {
        var tipo = await db.Equipamentos.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => (TipoEquipamento?)e.Tipo)
            .FirstOrDefaultAsync();

        if (tipo is null) return null;

        return await CarregarParaEdicaoAsync(db, id, tipo.Value, semRastreamento: true);
    }

    // TPT: cada subtipo mora num DbSet próprio — pra ler os campos
    // específicos, é preciso consultar o conjunto certo, não o Equipamentos base.
    private static async Task<Equipamento?> CarregarParaEdicaoAsync(
        SigaDbContext db, int id, TipoEquipamento tipo, bool semRastreamento = false)
    {
        IQueryable<Equipamento> Preparar<T>(IQueryable<T> query) where T : Equipamento
        {
            var q = query.Include(e => e.Local).Include(e => e.Responsavel).Include(e => e.NotaFiscal);
            return semRastreamento ? q.AsNoTracking() : q;
        }

        return tipo switch
        {
            TipoEquipamento.Computador => await Preparar(db.Computadores).FirstOrDefaultAsync(e => e.Id == id),
            TipoEquipamento.Impressora => await Preparar(db.Impressoras).FirstOrDefaultAsync(e => e.Id == id),
            TipoEquipamento.DispositivoRede => await Preparar(db.DispositivosRede).FirstOrDefaultAsync(e => e.Id == id),
            _ => await Preparar(db.Equipamentos).FirstOrDefaultAsync(e => e.Id == id),
        };
    }

    private static void PreencherCamposComuns(Equipamento equipamento, EquipamentoRequest request)
    {
        equipamento.Patrimonio = string.IsNullOrWhiteSpace(request.Patrimonio) ? null : request.Patrimonio.Trim();
        equipamento.NumeroSerie = string.IsNullOrWhiteSpace(request.NumeroSerie) ? null : request.NumeroSerie.Trim();
        equipamento.Marca = request.Marca.Trim();
        equipamento.Modelo = request.Modelo.Trim();
        equipamento.LocalId = request.LocalId;
        equipamento.ResponsavelId = request.ResponsavelId;
        equipamento.NotaFiscalId = request.NotaFiscalId;
        equipamento.AnoAquisicao = request.AnoAquisicao;
        equipamento.GarantiaAte = request.GarantiaAte;
        equipamento.Observacao = request.Observacao;
    }

    private static void AplicarCamposEspecificos(Equipamento equipamento, EquipamentoRequest request)
    {
        switch (equipamento)
        {
            case Computador computador:
                computador.Subtipo = request.SubtipoComputador;
                computador.SistemaOperacional = request.SistemaOperacional;
                computador.RamGb = request.RamGb;
                computador.ArmazenamentoGb = request.ArmazenamentoGb;
                computador.TipoArmazenamento = request.TipoArmazenamento;
                computador.Processador = request.Processador;
                break;
            case Impressora impressora:
                impressora.TipoImpressao = request.TipoImpressao;
                impressora.Colorida = request.Colorida;
                impressora.Conexao = request.Conexao;
                impressora.ContadorPaginas = request.ContadorPaginas;
                break;
            case DispositivoRede dispositivoRede:
                dispositivoRede.Subtipo = request.SubtipoDispositivoRede;
                dispositivoRede.EnderecoIp = request.EnderecoIp;
                dispositivoRede.EnderecoMac = request.EnderecoMac;
                dispositivoRede.NumPortas = request.NumPortas;
                dispositivoRede.VersaoFirmware = request.VersaoFirmware;
                break;
        }
    }

    // O texto das trocas é gerado aqui, de forma padronizada — nunca digitado
    // livre pelo operador (ver regra do "historico" no CLAUDE.md).
    private static async Task RegistrarTrocasAsync(
        SigaDbContext db, HttpContext http, Equipamento equipamento, int? localAnteriorId, int? responsavelAnteriorId)
    {
        var registradoPor = NomeUsuarioLogado(http);
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        if (equipamento.LocalId != localAnteriorId)
        {
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

        if (equipamento.ResponsavelId != responsavelAnteriorId)
        {
            var nomeAnterior = responsavelAnteriorId is null ? "sem responsável definido" :
                (await db.Responsaveis.AsNoTracking().FirstOrDefaultAsync(r => r.Id == responsavelAnteriorId))?.Nome ?? "responsável removido";
            var nomeNovo = equipamento.ResponsavelId is null ? "sem responsável definido" :
                (await db.Responsaveis.AsNoTracking().FirstOrDefaultAsync(r => r.Id == equipamento.ResponsavelId))?.Nome ?? "responsável removido";

            db.Historicos.Add(new Historico
            {
                EquipamentoId = equipamento.Id,
                Tipo = TipoHistorico.TrocaResponsavel,
                Data = hoje,
                Descricao = $"Responsável alterado de \"{nomeAnterior}\" para \"{nomeNovo}\".",
                RegistradoPor = registradoPor,
                RegistradoEm = DateTime.UtcNow,
            });
        }
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

        if (request.LocalId is not null && !await db.Locais.AnyAsync(l => l.Id == request.LocalId))
            erros["localId"] = ["O local informado não existe."];

        if (request.ResponsavelId is not null && !await db.Responsaveis.AnyAsync(r => r.Id == request.ResponsavelId))
            erros["responsavelId"] = ["O responsável informado não existe."];

        if (request.NotaFiscalId is not null && !await db.NotasFiscais.AnyAsync(n => n.Id == request.NotaFiscalId))
            erros["notaFiscalId"] = ["A nota fiscal informada não existe."];

        if (request.Tipo == TipoEquipamento.DispositivoRede && !string.IsNullOrWhiteSpace(request.EnderecoMac))
        {
            var normalizado = request.EnderecoMac.Trim().ToLower();
            var emUso = await db.DispositivosRede.AnyAsync(d =>
                d.EnderecoMac != null && d.EnderecoMac.ToLower() == normalizado && (ignorarId == null || d.Id != ignorarId));
            if (emUso) erros["enderecoMac"] = ["Já existe um dispositivo com esse endereço MAC."];
        }

        return erros;
    }

    private static EquipamentoResponse ParaResponse(Equipamento e)
    {
        var computador = e as Computador;
        var impressora = e as Impressora;
        var dispositivoRede = e as DispositivoRede;

        return new EquipamentoResponse(
            e.Id, e.Tipo.ToString(), e.Patrimonio, e.NumeroSerie, e.Marca, e.Modelo,
            e.LocalId, e.Local?.Nome, e.ResponsavelId, e.Responsavel?.Nome, e.NotaFiscalId, e.NotaFiscal?.Numero,
            e.Status.ToString(), e.AnoAquisicao, e.GarantiaAte, e.Observacao, e.CriadoEm, e.AtualizadoEm,
            computador?.Subtipo?.ToString(), computador?.SistemaOperacional, computador?.RamGb,
            computador?.ArmazenamentoGb, computador?.TipoArmazenamento?.ToString(), computador?.Processador,
            impressora?.TipoImpressao?.ToString(), impressora?.Colorida, impressora?.Conexao?.ToString(),
            impressora?.ContadorPaginas,
            dispositivoRede?.Subtipo?.ToString(), dispositivoRede?.EnderecoIp, dispositivoRede?.EnderecoMac,
            dispositivoRede?.NumPortas, dispositivoRede?.VersaoFirmware
        );
    }
}
