using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SIGA.Api.Domain;

namespace SIGA.Api.Services;

public class TokenService(IOptions<JwtOptions> jwtOptions)
{
    private readonly JwtOptions _options = jwtOptions.Value;

    // Papéis do usuário viram claims `ClaimTypes.Role` — a autorização por
    // papel nas rotas lê isso direto do token, sem consultar o banco de novo.
    public (string Token, DateTime ExpiraEm) GerarToken(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, usuario.NomeUsuario),
            new(ClaimTypes.Name, usuario.Nome),
        };
        claims.AddRange(usuario.Papeis.Select(p => new Claim(ClaimTypes.Role, p.Nome)));

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);
        var expiraEm = DateTime.UtcNow.AddMinutes(_options.ExpiracaoMinutos);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiraEm,
            signingCredentials: credenciais);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
    }
}
