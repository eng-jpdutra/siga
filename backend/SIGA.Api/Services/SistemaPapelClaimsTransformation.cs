using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace SIGA.Api.Services;

// O token vem do Portal (identidade única do ecossistema — ver PORTAL/CLAUDE.md)
// e carrega um claim "sistemaPapel" por sistema que o usuário acessa, no
// formato "NomeDoSistema:NomeDoPapel" (ex.: "SIGA:Administrador"). O SIGA só
// se importa com os que começam com "SIGA:" — os demais são de outros
// sistemas do ecossistema, ignorados aqui.
//
// Essa transformação promove o papel específico do SIGA pra um claim de
// role de verdade (ClaimTypes.Role), pra RequireRole/policy continuarem
// funcionando exatamente como antes, sem precisar mudar autorização em
// nenhum endpoint.
public class SistemaPapelClaimsTransformation : IClaimsTransformation
{
    private const string PrefixoSiga = "SIGA:";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity is null || !identity.IsAuthenticated) return Task.FromResult(principal);

        // .ToList() materializa a lista antes do foreach — sem isso,
        // AddClaim mexe na mesma coleção que o FindAll ainda está
        // percorrendo (enumeração + mutação simultânea derruba com
        // InvalidOperationException).
        var papeisSiga = identity.FindAll("sistemaPapel")
            .Where(c => c.Value.StartsWith(PrefixoSiga, StringComparison.Ordinal))
            .Select(c => c.Value[PrefixoSiga.Length..])
            .Where(papel => !identity.HasClaim(ClaimTypes.Role, papel))
            .ToList();

        foreach (var papel in papeisSiga)
            identity.AddClaim(new Claim(ClaimTypes.Role, papel));

        return Task.FromResult(principal);
    }
}
