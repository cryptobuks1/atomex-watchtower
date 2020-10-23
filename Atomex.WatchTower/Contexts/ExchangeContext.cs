using System.Linq;
using Microsoft.EntityFrameworkCore;
using Atomex.Common;
using Atomex.WatchTower.Entities;

namespace Atomex.Contexts
{
    public class ExchangeContext : DbContext
    {
        private const string DecimalType = "decimal(18,9)";

        public DbSet<Swap> Swaps { get; set; }
        public DbSet<Party> Parties { get; set; }
        public DbSet<PartyTransaction> PartyTransactions { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        private string _connectionString;

        public ExchangeContext()
            : base()
        {
        }

        public ExchangeContext(string connectionString)
            : base()
        {
            _connectionString = connectionString;
        }

        public ExchangeContext(DbContextOptions<ExchangeContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //_connectionString = "Host=localhost;Port=5432;Username=postgres;Password=;Database=atomex";

            if (_connectionString != null)
                optionsBuilder.UseNpgsql(_connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // swap & party relationships
            modelBuilder.Entity<Swap>()
                .HasOne(s => s.Initiator)
                .WithOne(p => p.InitiatorSwap)
                .HasForeignKey<Swap>(s => s.InitiatorId);

            modelBuilder.Entity<Swap>()
                .HasOne(s => s.Acceptor)
                .WithOne(p => p.AcceptorSwap)
                .HasForeignKey<Swap>(s => s.AcceptorId);

            // party & transaction many-to-many
            modelBuilder.Entity<PartyTransaction>()
                .HasOne(pt => pt.Party)
                .WithMany(p => p.Transactions)
                .HasForeignKey(pt => pt.PartyId);

            modelBuilder.Entity<PartyTransaction>()
                .HasOne(pt => pt.Transaction)
                .WithMany(t => t.Parties)
                .HasForeignKey(pt => pt.TransactionId);

            // set decimal precision and scale
            modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal))
                .ToList()
                .ForEach(p => p.SetColumnType(DecimalType));

            modelBuilder.UseUtc();

            base.OnModelCreating(modelBuilder);
        }
    }
}