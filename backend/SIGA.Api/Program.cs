using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SIGA.Api.Data;
using SIGA.Api.Endpoints;
using SIGA.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Enums trafegam como texto ("Computador", não "0") — mais legível no corpo
// da requisição/resposta e nos logs, sem custo real de performance aqui.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Origens liberadas vêm da configuração (appsettings.Development.json em dev,
// variável de ambiente em produção) — nunca fixas no código. Sem nenhuma
// origem configurada, nada é liberado (CORS restritivo por padrão).
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origensPermitidas = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (origensPermitidas.Length > 0)
        {
            policy.WithOrigins(origensPermitidas).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

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

// JWT: quem emite o token é o Portal (login único do ecossistema — ver
// PORTAL/CLAUDE.md), não o SIGA. A chave de assinatura é a mesma dos dois
// lados (segredo — user-secrets em dev, variável de ambiente em produção).
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// Arquivos de nota fiscal ficam em disco (pasta configurável via
// Uploads:Diretorio) — o banco só guarda o caminho relativo.
builder.Services.AddSingleton<ArmazenamentoArquivos>();

// Chave de licença criptografada no banco — a chave de criptografia em si
// é segredo (user-secrets em dev, variável de ambiente em produção).
builder.Services.AddSingleton<CriptografiaLicenca>();

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Configuração 'Jwt' ausente.");

if (string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    throw new InvalidOperationException(
        "Jwt:Key não configurada. Precisa ser a MESMA chave do Portal — " +
        "dotnet user-secrets set \"Jwt:Key\" \"<mesma chave do Portal>\".");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
        };
    });

// Promove o claim "sistemaPapel" (ex.: "SIGA:Administrador") vindo do
// Portal pra uma role de verdade — ver Services/SistemaPapelClaimsTransformation.cs.
builder.Services.AddTransient<IClaimsTransformation, SistemaPapelClaimsTransformation>();

builder.Services.AddAuthorization(options =>
{
    // Só dois papéis existem: Administrador (lê e escreve) e Consulta (só
    // lê). Essa política gate-keeps toda escrita — criar/editar/baixar em
    // cada domínio do inventário (ver os grupos de rota em Endpoints/*.cs).
    options.AddPolicy("SomenteAdministrador", p => p.RequireRole("Administrador"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapLocalEndpoints();
app.MapNotaFiscalEndpoints();
app.MapEquipamentoEndpoints();
app.MapLicencaEndpoints();
app.MapVereadorEndpoints();
app.MapDashboardEndpoints();

app.Run();
