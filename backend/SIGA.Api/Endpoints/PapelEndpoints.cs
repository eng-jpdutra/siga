using Microsoft.EntityFrameworkCore;
using SIGA.Api.Data;
using SIGA.Api.DTOs;

namespace SIGA.Api.Endpoints;

public static class PapelEndpoints
{
    public static void MapPapelEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/papeis")
            .RequireAuthorization("SomenteAdministrador")
            .MapGet("/", async (SigaDbContext db) =>
                await db.Papeis.AsNoTracking()
                    .OrderBy(p => p.Nome)
                    .Select(p => new PapelResponse(p.Id, p.Nome))
                    .ToListAsync());
    }
}
