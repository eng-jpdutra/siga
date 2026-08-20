using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SIGA.Api.Data;
using SIGA.Api.Domain;
using SIGA.Api.DTOs;

namespace SIGA.Api.Endpoints;

public static class DashboardEndpoints
{
    private const int DiasAlertaGarantia = 60;
    private const int LimiteItensPorLista = 10;

    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard", ObterAsync).RequireAuthorization();
    }

    private static async Task<Ok<DashboardResponse>> ObterAsync(SigaDbContext db)
    {
        var totalEquipamentos = await db.Equipamentos.CountAsync();

        var porStatusRaw = await db.Equipamentos.AsNoTracking()
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Quantidade = g.Count() })
            .ToListAsync();
        var porStatus = porStatusRaw.Select(x => new ContagemPorChave(x.Status.ToString(), x.Quantidade)).ToList();

        // Contagem por tipo só considera o parque ativo/em manutenção —
        // equipamento baixado não é "inventário em uso".
        var porTipoRaw = await db.Equipamentos.AsNoTracking()
            .Where(e => e.Status != StatusEquipamento.Baixado)
            .GroupBy(e => e.Tipo)
            .Select(g => new { Tipo = g.Key, Quantidade = g.Count() })
            .ToListAsync();
        var porTipo = porTipoRaw.Select(x => new ContagemPorChave(x.Tipo.ToString(), x.Quantidade)).ToList();

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var limiteGarantia = hoje.AddDays(DiasAlertaGarantia);

        // Inclui garantias já vencidas (sem limite inferior) — são o alerta
        // mais urgente, não faz sentido escondê-las da lista.
        var equipamentosGarantia = await db.Equipamentos.AsNoTracking()
            .Where(e => e.Status != StatusEquipamento.Baixado && e.GarantiaAte != null && e.GarantiaAte <= limiteGarantia)
            .OrderBy(e => e.GarantiaAte)
            .ToListAsync();
        var garantiasVencendo = equipamentosGarantia
            .Take(LimiteItensPorLista)
            .Select(e => new AlertaGarantia(e.Id, DescricaoEquipamento(e), e.GarantiaAte!.Value))
            .ToList();

        var atividades = await db.Historicos.AsNoTracking()
            .Include(h => h.Equipamento)
            .OrderByDescending(h => h.RegistradoEm)
            .Take(LimiteItensPorLista)
            .ToListAsync();
        var atividadeRecente = atividades
            .Select(h => new AtividadeRecente(
                h.EquipamentoId, DescricaoEquipamento(h.Equipamento), h.Tipo.ToString(), h.Data, h.Descricao, h.RegistradoEm))
            .ToList();

        return TypedResults.Ok(new DashboardResponse(
            totalEquipamentos,
            porStatus,
            porTipo,
            equipamentosGarantia.Count,
            garantiasVencendo,
            atividadeRecente));
    }

    private static string DescricaoEquipamento(Equipamento e) =>
        $"{e.Marca} {e.Modelo}" + (e.Patrimonio is not null ? $" ({e.Patrimonio})" : "");
}
