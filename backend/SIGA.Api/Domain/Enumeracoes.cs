namespace SIGA.Api.Domain;

// Enums espelham as listas fechadas (`enum` do MySQL) documentadas em SIGA_SQL.mwb.
// Gravados como texto no banco (ver configuração no DbContext) para o valor
// continuar legível em consultas diretas, independente do provedor.

public enum StatusEquipamento
{
    Ativo,
    Manutencao,
    Baixado,
}

// Lista fechada só dos campos COMUNS (ver Equipamento.cs); os campos
// específicos de cada tipo ficam em `Detalhes` e o mapa "tipo -> campos"
// mora no frontend (EquipamentosPage.jsx) — ver CLAUDE.md. Adicionar um
// tipo novo é só acrescentar aqui e no mapa do frontend, sem migration.
public enum TipoEquipamento
{
    Computador,
    Impressora,
    Monitor,
    DispositivoRede,
    Nobreak,
    Camera,
    DvrNvr,
    TelefoneIp,
    Outro,
}

// Tipo de lançamento no diário do equipamento (ver regra "historico" no CLAUDE.md).
public enum TipoHistorico
{
    Manutencao,
    Formatacao,
    MudancaLocal,
    TrocaResponsavel, // não usado mais (ver CLAUDE.md) — mantido só p/ ler históricos antigos.
    Outro,
}

public enum TipoLicenca
{
    OEM,
    Volume,
    Retail,
}
