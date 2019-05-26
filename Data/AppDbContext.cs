using dm.TanTipBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace dm.TanTipBot.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Deposit> Deposits { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Withdraw> Withdraws { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Wallet>()
                .HasAlternateKey(x => x.Address)
                .HasName("AlternateKey_Account_Address");
            modelBuilder.Entity<Deposit>()
                .HasAlternateKey(x => x.Hash)
                .HasName("AlternateKey_Deposit_Hash");
            modelBuilder.Entity<Withdraw>()
                .HasAlternateKey(x => x.Hash)
                .HasName("AlternateKey_Withdraw_Hash");
        }
    }

    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<AppDbContext>();
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("Config.json", optional: true, reloadOnChange: true)
                .AddJsonFile("Config.Local.json", optional: true, reloadOnChange: true)
                .Build();

            builder.UseSqlServer(configuration.GetConnectionString("Database"));
            return new AppDbContext(builder.Options);
        }
    }
}