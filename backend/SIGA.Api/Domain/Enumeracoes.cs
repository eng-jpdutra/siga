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

public enum StatusResponsavel
{
    Ativo,
    Inativo,
}

public enum TipoEquipamento
{
    Computador,
    Impressora,
    DispositivoRede,
    Outro,
}

public enum SubtipoComputador
{
    Desktop,
    Notebook,
}

public enum TipoArmazenamento
{
    HDD,
    SSD,
    NVMe,
}

public enum TipoImpressao
{
    Laser,
    JatoDeTinta,
    Matricial,
}

public enum ConexaoImpressora
{
    USB,
    Rede,
}

public enum SubtipoDispositivoRede
{
    Switch,
    Roteador,
    AccessPoint,
    Firewall,
}

// Tipo de lançamento no diário do equipamento (ver regra "historico" no CLAUDE.md).
public enum TipoHistorico
{
    Manutencao,
    Formatacao,
    MudancaLocal,
    TrocaResponsavel,
    Outro,
}

public enum TipoLicenca
{
    OEM,
    Volume,
    Retail,
}
