using Microsoft.EntityFrameworkCore;
using Server.Core.Accounts;
using Server.Core.Accounts.Codes;
using Server.Core.Coins;
using Server.Core.Trading;
using Server.Core.Wallets;
using Server.Logging;

namespace Server.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public virtual DbSet<Account> Accounts { get; set; } = null!;

    public virtual DbSet<Wallet> Wallets { get; set; } = null!;
    public virtual DbSet<WalletBalance> WalletBalances { get; set; } = null!;


    public virtual DbSet<Coin> Coins { get; set; } = null!;

    // DEX
    public virtual DbSet<Pool> Pools { get; set; } = null!;

    // Logs
    public virtual DbSet<AuthLog> AuthLogs { get; set; } = null!;
    public virtual DbSet<FaucetLog> FaucetLogs { get; set; } = null!;
    public virtual DbSet<GeneratedCodeLog> GeneratedCodeLogs { get; set; } = null!;
    public virtual DbSet<PoolTradeLog> PoolTradeLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity(typeof(Pool))
            .HasOne(typeof(Coin), "PrimaryCoin")
            .WithMany()
            .HasForeignKey("PrimaryCoinId")
            .OnDelete(DeleteBehavior.NoAction); // no ON DELETE 
        modelBuilder.Entity(typeof(Pool))
            .HasOne(typeof(Coin), "SecondaryCoin")
            .WithMany()
            .HasForeignKey("SecondaryCoinId")
            .OnDelete(DeleteBehavior.NoAction); // no ON DELETE
        modelBuilder.Entity(typeof(Pool))
            .HasOne(typeof(Coin), "LiquidityCoin")
            .WithMany()
            .HasForeignKey("LiquidityCoinId")
            .OnDelete(DeleteBehavior.NoAction); // no ON DELETE

        var decimalProps = modelBuilder.Model
            .GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => (Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType) == typeof(decimal));

        foreach (var property in decimalProps)
        {
            var annotations = property.GetAnnotations();
            if (annotations.Any(x => x.Name is "Relational:ColumnType" or "Precision" or "Scale")) continue;
            property.SetPrecision(18);
            property.SetScale(8);
        }
    }
}