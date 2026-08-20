using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SIGA.Api.Domain;

namespace SIGA.Api.Data;

public class SigaDbContext : DbContext
{
    public SigaDbContext(DbContextOptions<SigaDbContext> options) : base(options)
    {
    }

    public DbSet<Equipamento> Equipamentos => Set<Equipamento>();
    public DbSet<Historico> Historicos => Set<Historico>();
    public DbSet<Configuracao> Configuracoes => Set<Configuracao>();
    public DbSet<Local> Locais => Set<Local>();
    public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();
    public DbSet<Licenca> Licencas => Set<Licenca>();
    public DbSet<Vereador> Vereadores => Set<Vereador>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Equipamento>(entity =>
        {
            entity.ToTable("equipamento");

            entity.Property(e => e.Marca).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Modelo).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Patrimonio).HasMaxLength(50);
            entity.Property(e => e.NumeroSerie).HasMaxLength(100);
            entity.Property(e => e.EnderecoMac).HasMaxLength(17);
            entity.Property(e => e.EnderecoIp).HasMaxLength(45);
            entity.Property(e => e.CriadoEm).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

            // Campos específicos de cada tipo (RAM de um computador, polegadas
            // de um monitor etc.) — gravados como texto JSON simples (coluna
            // comum de texto, não `jsonb` do Postgres) pra funcionar igual em
            // SQLite (dev) e Postgres (produção), sem SQL específico de
            // provedor (ver CLAUDE.md, "Código 100% agnóstico de banco"). O
            // ValueComparer é necessário porque Dictionary não tem igualdade
            // por valor — sem ele o EF não percebe edição em `Detalhes`.
            entity.Property(e => e.Detalhes)
                .HasConversion(
                    d => d == null ? null : JsonSerializer.Serialize(d, (JsonSerializerOptions?)null),
                    json => string.IsNullOrWhiteSpace(json)
                        ? null
                        : JsonSerializer.Deserialize<Dictionary<string, object?>>(json, (JsonSerializerOptions?)null))
                .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, object?>?>(
                    (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
                    d => d == null ? 0 : JsonSerializer.Serialize(d, (JsonSerializerOptions?)null).GetHashCode(),
                    d => d == null ? null : JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(d, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null)));

            // Patrimônio, número de série e MAC são únicos apenas quando
            // preenchidos. Sem colchetes no filtro: colchete é sintaxe do SQL
            // Server e o Postgres não entende — identificador simples
            // funciona em SQLite e Postgres.
            entity.HasIndex(e => e.Patrimonio).IsUnique().HasFilter("Patrimonio IS NOT NULL");
            entity.HasIndex(e => e.NumeroSerie).IsUnique().HasFilter("NumeroSerie IS NOT NULL");
            entity.HasIndex(e => e.EnderecoMac).IsUnique().HasFilter("EnderecoMac IS NOT NULL");

            // Marca/Modelo entraram no filtro de busca (útil pra achar "todos os
            // Dell", por exemplo) — por isso ganham índice, seguindo a regra do
            // CLAUDE.md de toda coluna filtrável ter índice.
            entity.HasIndex(e => e.Marca);
            entity.HasIndex(e => e.Modelo);

            entity.HasIndex(e => e.Tipo);
            entity.HasIndex(e => e.Status);

            // Local não some: um equipamento em uso impede a remoção
            // (equivalente ao "sem ON DELETE" do modelo original == restrict).
            entity.HasOne(e => e.Local)
                .WithMany(l => l.Equipamentos)
                .HasForeignKey(e => e.LocalId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.NotaFiscal)
                .WithMany()
                .HasForeignKey(e => e.NotaFiscalId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NotaFiscal>(entity =>
        {
            entity.ToTable("nota_fiscal");

            entity.Property(n => n.Numero).HasMaxLength(50).IsRequired();
            entity.Property(n => n.Fornecedor).HasMaxLength(150);
            entity.Property(n => n.Valor).HasPrecision(12, 2);
            entity.Property(n => n.ArquivoPath).HasMaxLength(255);
        });

        modelBuilder.Entity<Local>(entity =>
        {
            entity.ToTable("local");

            entity.Property(l => l.Nome).HasMaxLength(100).IsRequired();
            entity.Property(l => l.Descricao).HasMaxLength(255);
            entity.Property(l => l.Tipo).HasMaxLength(50);

            entity.HasIndex(l => l.Nome).IsUnique();
        });

        modelBuilder.Entity<Historico>(entity =>
        {
            entity.ToTable("historico");

            entity.Property(e => e.Descricao).IsRequired();
            entity.Property(e => e.RegistradoPor).HasMaxLength(120);
            entity.Property(e => e.RegistradoEm).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(h => h.Equipamento)
                .WithMany(e => e.Historicos)
                .HasForeignKey(h => h.EquipamentoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(h => h.EquipamentoId);
        });

        modelBuilder.Entity<Configuracao>(entity =>
        {
            entity.ToTable("configuracao");

            entity.Property(c => c.Titulo).HasMaxLength(150).IsRequired();
            entity.Property(c => c.Conteudo).IsRequired();
            entity.Property(c => c.CriadoEm).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(c => c.Equipamento)
                .WithMany(e => e.Configuracoes)
                .HasForeignKey(c => c.EquipamentoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => c.EquipamentoId);
        });

        modelBuilder.Entity<Licenca>(entity =>
        {
            entity.ToTable("licenca");

            entity.Property(l => l.Produto).HasMaxLength(150).IsRequired();
            entity.Property(l => l.ChaveCriptografada).IsRequired();
            entity.Property(l => l.Tipo).HasConversion<string>().HasMaxLength(10);

            entity.HasOne(l => l.Equipamento)
                .WithMany()
                .HasForeignKey(l => l.EquipamentoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Independente da nota fiscal do próprio equipamento — Restrict
            // pelo mesmo motivo das outras FKs pra nota_fiscal.
            entity.HasOne(l => l.NotaFiscal)
                .WithMany()
                .HasForeignKey(l => l.NotaFiscalId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(l => l.EquipamentoId);
        });

        modelBuilder.Entity<Vereador>(entity =>
        {
            entity.ToTable("vereador");

            entity.Property(v => v.Nome).HasMaxLength(120).IsRequired();
            entity.Property(v => v.Partido).HasMaxLength(50);
            entity.Property(v => v.Contato).HasMaxLength(120);

            // Se o gabinete (local) for removido, o vereador não leva junto —
            // só perde o vínculo (nem todo local é gabinete, ver Domain/Vereador.cs).
            entity.HasOne(v => v.Local)
                .WithMany(l => l.Vereadores)
                .HasForeignKey(v => v.LocalId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
