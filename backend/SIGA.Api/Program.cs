using Microsoft.EntityFrameworkCore;
using SIGA.Api.Data;
using SIGA.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Provedor do EF Core trocado por ambiente: SQLite acelera o dia a dia local
// (sem precisar de um servidor de banco rodando); Postgres é o banco real de produção.
builder.Services.AddDbContext<SigaDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (builder.Environment.IsDevelopment())
    {
        options.UseSqlite(connectionString ?? "Data Source=banco.db");
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapHealthEndpoints();

app.Run();
