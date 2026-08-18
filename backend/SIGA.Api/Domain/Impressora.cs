namespace SIGA.Api.Domain;

// Subtabela TPT de `equipamento` (PK = FK para equipamento.id).
public class Impressora : Equipamento
{
    public TipoImpressao? TipoImpressao { get; set; }

    public bool Colorida { get; set; }

    public ConexaoImpressora? Conexao { get; set; }

    public int? ContadorPaginas { get; set; }
}
