namespace SIGA.Api.Services;

// Ligado à seção "Jwt" da configuração. `Key` é segredo — nunca no
// appsettings versionado, só em user-secrets (dev) ou variável de
// ambiente (produção). Issuer/Audience/ExpiracaoMinutos não são segredo.
public class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiracaoMinutos { get; set; } = 60;
}
