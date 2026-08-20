namespace SIGA.Api.Domain;

// Anotação técnica em texto livre sobre um equipamento (ex.: passo a passo de
// uma configuração de rede, senha de BIOS, checklist de instalação). Um
// equipamento pode ter várias; ao contrário do `historico`, o conteúdo pode
// ser editado/removido depois — não é um diário de eventos, é uma nota viva.
public class Configuracao
{
    public int Id { get; set; }

    public int EquipamentoId { get; set; }

    public Equipamento Equipamento { get; set; } = null!;

    public string Titulo { get; set; } = string.Empty;

    public string Conteudo { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; }

    public DateTime? AtualizadoEm { get; set; }
}
