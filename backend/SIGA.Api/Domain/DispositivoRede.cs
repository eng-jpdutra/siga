namespace SIGA.Api.Domain;

// Subtabela TPT de `equipamento` (PK = FK para equipamento.id).
public class DispositivoRede : Equipamento
{
    public SubtipoDispositivoRede? Subtipo { get; set; }

    public string? EnderecoIp { get; set; }

    public string? EnderecoMac { get; set; }

    public short? NumPortas { get; set; }

    public string? VersaoFirmware { get; set; }
}
