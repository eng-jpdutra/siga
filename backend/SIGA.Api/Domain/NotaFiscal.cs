namespace SIGA.Api.Domain;

// Tabela-base (sem FK pra fora) — equipamento referencia nota_fiscal, não o
// contrário. O arquivo em si fica em disco; aqui só o caminho relativo.
public class NotaFiscal
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public string? Fornecedor { get; set; }

    public DateOnly? DataEmissao { get; set; }

    public decimal? Valor { get; set; }

    public string? ArquivoPath { get; set; }

    public string? Observacao { get; set; }
}
