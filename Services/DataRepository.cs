using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using Atomex.Contexts;
using Atomex.Entities;
using Atomex.Services.Abstract;

namespace Atomex.Services
{
    public class DataRepository : IDataRepository
    {
        private readonly DbContextOptions<ExchangeContext> _contextOptions;

        public DataRepository(DbContextOptions<ExchangeContext> contextOptions)
        {
            _contextOptions = contextOptions ?? throw new ArgumentNullException(nameof(contextOptions));
        }

        public async Task<bool> AddSwapAsync(
            Swap swap,
            CancellationToken cancellationToken = default)
        {
            using var context = new ExchangeContext(_contextOptions);

            await context.AddAsync(swap, cancellationToken);

            var result = await context.SaveChangesAsync(cancellationToken);

            return result > 0;
        }

        public async Task<Swap> GetSwapAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            using var context = new ExchangeContext(_contextOptions);

            return await context.Swaps
                .Include(s => s.Initiator)
                    .ThenInclude(p => p.Transactions)
                        .ThenInclude(pt => pt.Transaction)
                .Include(s => s.Acceptor)
                    .ThenInclude(p => p.Transactions)
                        .ThenInclude(pt => pt.Transaction)
                .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public Task<IEnumerable<Swap>> GetSwapsAsync(
            Expression<Func<Swap, bool>> predicate,
            int sort = -1,
            int offset = 0,
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            return GetSwapsAsync(
                predicates: new List<Expression<Func<Swap, bool>>> { predicate },
                sort: sort,
                offset: offset,
                limit: limit,
                cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<Swap>> GetSwapsAsync(
            List<Expression<Func<Swap, bool>>> predicates,
            int sort = -1,
            int offset = 0,
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            using var context = new ExchangeContext(_contextOptions);

            IQueryable<Swap> query = context.Swaps
                .Include(s => s.Initiator)
                    .ThenInclude(p => p.Transactions)
                        .ThenInclude(pt => pt.Transaction)
                .Include(s => s.Acceptor)
                    .ThenInclude(p => p.Transactions)
                        .ThenInclude(pt => pt.Transaction);

            foreach (var predicate in predicates)
                query = query.Where(predicate);

            query = sort == -1
                ? query.OrderByDescending(o => o.Id)
                : query.OrderBy(o => o.Id);

            return await query
                .Skip(offset)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> UpdateSwapAsync(
            Swap swap,
            CancellationToken cancellationToken = default)
        {
            using var context = new ExchangeContext(_contextOptions);

            context.Update(swap);

            return await context.SaveChangesAsync(cancellationToken) > 0;
        }

        public async Task<bool> UpdatePartyAsync(
            Party party,
            CancellationToken cancellationToken = default)
        {
            using var context = new ExchangeContext(_contextOptions);

            context.Parties.Update(party);

            return await context.SaveChangesAsync(cancellationToken) > 0;
        }

        public async Task<bool> AddPartyTransactionAsync(
            PartyTransaction transaction,
            CancellationToken cancellationToken = default)
        {
            using var context = new ExchangeContext(_contextOptions);

            await context.PartyTransactions
                .AddAsync(transaction);

            if (transaction.TransactionId > 0 && transaction.Transaction != null)
                context.Transactions.Update(transaction.Transaction);

            return await context.SaveChangesAsync(cancellationToken) > 0;
        }

        public async Task<bool> UpdatePartyTransactionAsync(
            PartyTransaction transaction,
            CancellationToken cancellationToken = default)
        {
            using var context = new ExchangeContext(_contextOptions);

            context.PartyTransactions.Update(transaction);

            if (transaction.TransactionId > 0 && transaction.Transaction != null)
                context.Transactions.Update(transaction.Transaction);

            return await context.SaveChangesAsync(cancellationToken) > 0;
        }

        public async Task<bool> RemovePartyTransactionAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            using var context = new ExchangeContext(_contextOptions);

            context.PartyTransactions.Remove(new PartyTransaction { Id = id });

            return await context.SaveChangesAsync(cancellationToken) > 0;
        }

        public async Task<Transaction> GetTransactionAsync(
            string txId,
            string currency,
            CancellationToken cancellationToken = default)
        {
            using var context = new ExchangeContext(_contextOptions);

            return await context.Transactions
                .FirstOrDefaultAsync(
                    predicate: t => t.TxId.ToLower() == txId.ToLower() && t.Currency == currency,
                    cancellationToken: cancellationToken);
        }

        public async Task<bool> UpdateTransactionAsync(
            Transaction transaction,
            CancellationToken cancellationToken = default)
        {
            using var context = new ExchangeContext(_contextOptions);

            context.Transactions.Update(transaction);

            return await context.SaveChangesAsync(cancellationToken) > 0;
        }

        public async Task<bool> RemoveTransactionAsync(
            long id,
            CancellationToken cancellationToken = default)
        {
            using var context = new ExchangeContext(_contextOptions);

            context.Transactions.Remove(new Transaction { Id = id });

            return await context.SaveChangesAsync(cancellationToken) > 0;
        }
    }
}