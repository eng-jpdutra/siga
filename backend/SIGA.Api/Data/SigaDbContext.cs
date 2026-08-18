using Microsoft.EntityFrameworkCore;
using SIGA.Api.Domain;

namespace SIGA.Api.Data;

public class SigaDbContext : DbContext
{
    public SigaDbContext(DbContextOptions<SigaDbContext> options) : base(options)
    {
    }

    public DbSet<Equipamento> Equipamentos => Set<Equipamento>();
    public DbSet<Computador> Computadores => Set<Computador>();
    public DbSet<Impressora> Impressoras => Set<Impressora>();
    public DbSet<DispositivoRede> DispositivosRede => Set<DispositivoRede>();
    public DbSet<Historico> Historicos => Set<Historico>();
    public DbSet<Local> Locais => Set<Local>();
    public DbSet<Responsavel> Responsaveis => Set<Responsavel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Herança em tabelas (TPT): cada subtipo em sua própria tabela, com a
        // PK também sendo FK para equipamento.id — ver CLAUDE.md.
        modelBuilder.Entity<Equipamento>(entity =>
        {
            entity.ToTable("equipamento");

            entity.Property(e => e.Marca).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Modelo).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Patrimonio).HasMaxLength(50);
            entity.Property(e => e.NumeroSerie).HasMaxLength(100);
            entity.Property(e => e.CriadoEm).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

            // Patrimônio e número de série são únicos apenas quando preenchidos.
            // Sem colchetes no filtro: colchete é sintaxe do SQL Server e o Postgres
            // não entende — identificador simples funciona em SQLite e Postgres.
            entity.HasIndex(e => e.Patrimonio).IsUnique().HasFilter("Patrimonio IS NOT NULL");
            entity.HasIndex(e => e.NumeroSerie).IsUnique().HasFilter("NumeroSerie IS NOT NULL");

            entity.HasIndex(e => e.Tipo);
            entity.HasIndex(e => e.Status);

            // Local/responsável não somem: um equipamento em uso impede a remoção
            // (equivalente ao "sem ON DELETE" do modelo original == restrict).
            entity.HasOne(e => e.Local)
                .WithMany(l => l.Equipamentos)
                .HasForeignKey(e => e.LocalId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Responsavel)
                .WithMany(r => r.Equipamentos)
                .HasForeignKey(e => e.ResponsavelId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Local>(entity =>
        {
            entity.ToTable("local");

            entity.Property(l => l.Nome).HasMaxLength(100).IsRequired();
            entity.Property(l => l.Descricao).HasMaxLength(255);

            entity.HasIndex(l => l.Nome).IsUnique();
        });

        modelBuilder.Entity<Responsavel>(entity =>
        {
            entity.ToTable("responsavel");

            entity.Property(r => r.Nome).HasMaxLength(120).IsRequired();
            entity.Property(r => r.Cargo).HasMaxLength(100);
            entity.Property(r => r.Contato).HasMaxLength(120);
            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

            // Se o local for removido, o responsável não leva junto — só perde o vínculo.
            entity.HasOne(r => r.Local)
                .WithMany(l => l.Responsaveis)
                .HasForeignKey(r => r.LocalId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Computador>(entity =>
        {
            entity.ToTable("computador");
            entity.Property(e => e.SistemaOperacional).HasMaxLength(100);
            entity.Property(e => e.Processador).HasMaxLength(100);
            entity.Property(e => e.Subtipo).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.TipoArmazenamento).HasConversion<string>().HasMaxLength(10);
        });

        modelBuilder.Entity<Impressora>(entity =>
        {
            entity.ToTable("impressora");
            entity.Property(e => e.TipoImpressao).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Conexao).HasConversion<string>().HasMaxLength(10);
        });

        modelBuilder.Entity<DispositivoRede>(entity =>
        {
            entity.ToTable("dispositivo_rede");
            entity.Property(e => e.EnderecoIp).HasMaxLength(45);
            entity.Property(e => e.EnderecoMac).HasMaxLength(17);
            entity.Property(e => e.VersaoFirmware).HasMaxLength(50);
            entity.Property(e => e.Subtipo).HasConversion<string>().HasMaxLength(20);

            entity.HasIndex(e => e.EnderecoMac).IsUnique().HasFilter("EnderecoMac IS NOT NULL");
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
    }
}
