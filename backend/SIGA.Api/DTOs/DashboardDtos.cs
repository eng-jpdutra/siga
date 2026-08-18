namespace SIGA.Api.DTOs;

public record ContagemPorChave(string Chave, int Quantidade);

public record AlertaGarantia(int EquipamentoId, string Descricao, DateOnly GarantiaAte);

public record EquipamentoSemResponsavel(int EquipamentoId, string Descricao);

public record AtividadeRecente(
    int EquipamentoId,
    string EquipamentoDescricao,
    string Tipo,
    DateOnly Data,
    string Descricao,
    DateTime RegistradoEm
);

public record DashboardResponse(
    int TotalEquipamentos,
    List<ContagemPorChave> PorStatus,
    List<ContagemPorChave> PorTipo,
    int TotalGarantiasVencendo,
    List<AlertaGarantia> GarantiasVencendo,
    int TotalSemResponsavel,
    List<EquipamentoSemResponsavel> SemResponsavel,
    List<AtividadeRecente> AtividadeRecente
);
