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
    public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();
    public DbSet<Licenca> Licencas => Set<Licenca>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Papel> Papeis => Set<Papel>();

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

            // Marca/Modelo entraram no filtro de busca (útil pra achar "todos os
            // Dell", por exemplo) — por isso ganham índice, seguindo a regra do
            // CLAUDE.md de toda coluna filtrável ter índice.
            entity.HasIndex(e => e.Marca);
            entity.HasIndex(e => e.Modelo);

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

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuario");

            entity.Property(u => u.Nome).HasMaxLength(120).IsRequired();
            entity.Property(u => u.NomeUsuario).HasMaxLength(60).IsRequired();
            entity.Property(u => u.SenhaHash).IsRequired();
            entity.Property(u => u.FotoPath).HasMaxLength(255);

            entity.HasIndex(u => u.NomeUsuario).IsUnique();

            // Usuário pode opcionalmente ser também um responsável por ativos —
            // FK opcional, sem exigir que toda conta tenha um responsável ligado.
            entity.HasOne(u => u.Responsavel)
                .WithMany()
                .HasForeignKey(u => u.ResponsavelId)
                .OnDelete(DeleteBehavior.SetNull);

            // N-para-N usuario_papel — nome de tabela explícito, como no CLAUDE.md.
            entity.HasMany(u => u.Papeis)
                .WithMany(p => p.Usuarios)
                .UsingEntity(j => j.ToTable("usuario_papel"));
        });

        modelBuilder.Entity<Papel>(entity =>
        {
            entity.ToTable("papel");
            entity.Property(p => p.Nome).HasMaxLength(50).IsRequired();
            entity.HasIndex(p => p.Nome).IsUnique();
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
    }
}
