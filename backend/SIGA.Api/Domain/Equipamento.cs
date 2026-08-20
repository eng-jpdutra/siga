namespace SIGA.Api.Domain;

// Ativo de TI da Câmara — computador, impressora, monitor, dispositivo de
// rede, nobreak, câmera, DVR/NVR, telefone IP etc. Os campos comuns a todos
// os tipos são colunas; os específicos de cada tipo ficam em `Detalhes`
// (ver comentário na propriedade e CLAUDE.md, "Campos específicos por tipo").
public class Equipamento
{
    public int Id { get; set; }

    public TipoEquipamento Tipo { get; set; }

    public string? Patrimonio { get; set; }

    public string? NumeroSerie { get; set; }

    public string Marca { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public int? LocalId { get; set; }

    public Local? Local { get; set; }

    public int? NotaFiscalId { get; set; }

    public NotaFiscal? NotaFiscal { get; set; }

    // Endereço de rede promovido a coluna real (em vez de ficar dentro de
    // `Detalhes`) porque precisa de índice único — só é preenchido em tipos
    // com placa de rede (dispositivo de rede, DVR/NVR, câmera IP...).
    public string? EnderecoMac { get; set; }

    public string? EnderecoIp { get; set; }

    // Campos específicos do tipo (ex.: RAM/processador de um computador,
    // polegadas/resolução de um monitor) — ver o mapa de campos por tipo no
    // frontend (EquipamentosPage.jsx) e a explicação em CLAUDE.md. Gravado
    // como texto JSON simples (não `jsonb` do Postgres), pra funcionar igual
    // em SQLite (dev) e Postgres (produção) sem SQL específico de provedor.
    public Dictionary<string, object?>? Detalhes { get; set; }

    // Soft delete: "excluir" um equipamento é gravar Status = Baixado.
    public StatusEquipamento Status { get; set; } = StatusEquipamento.Ativo;

    public short? AnoAquisicao { get; set; }

    public DateOnly? GarantiaAte { get; set; }

    public string? Observacao { get; set; }

    public DateTime CriadoEm { get; set; }

    public DateTime? AtualizadoEm { get; set; }

    public ICollection<Historico> Historicos { get; set; } = new List<Historico>();
    public ICollection<Configuracao> Configuracoes { get; set; } = new List<Configuracao>();
}
