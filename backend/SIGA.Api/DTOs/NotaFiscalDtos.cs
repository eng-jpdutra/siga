namespace SIGA.Api.DTOs;

public record NotaFiscalResponse(
    int Id,
    string Numero,
    string? Fornecedor,
    DateOnly? DataEmissao,
    decimal? Valor,
    string? Observacao,
    bool TemArquivo
);

// Vem de multipart/form-data (tem um arquivo opcional junto) — por isso é
// uma classe simples em vez de record, para o model binding de formulário.
public class NotaFiscalFormulario
{
    public string Numero { get; set; } = string.Empty;
    public string? Fornecedor { get; set; }
    public DateOnly? DataEmissao { get; set; }
    public decimal? Valor { get; set; }
    public string? Observacao { get; set; }
    public IFormFile? Arquivo { get; set; }
}
