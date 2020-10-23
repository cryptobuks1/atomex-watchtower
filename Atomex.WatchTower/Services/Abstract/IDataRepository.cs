using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Atomex.WatchTower.Entities;

namespace Atomex.WatchTower.Services.Abstract
{
    public interface IDataRepository
    {
        Task<bool> AddSwapAsync(
            Swap swap,
            CancellationToken cancellationToken = default);

        Task<Swap> GetSwapAsync(
            long id,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<Swap>> GetSwapsAsync(
            Expression<Func<Swap, bool>> predicate,
            int sort = -1,
            int offset = 0,
            int limit = 100,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<Swap>> GetSwapsAsync(
            List<Expression<Func<Swap, bool>>> predicates,
            int sort = -1,
            int offset = 0,
            int limit = 100,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateSwapAsync(
            Swap swap,
            CancellationToken cancellationToken = default);

        Task<bool> UpdatePartyAsync(
            Party party,
            CancellationToken cancellationToken = default);

        Task<bool> AddPartyTransactionAsync(
            PartyTransaction transaction,
            CancellationToken cancellationToken = default);

        Task<bool> UpdatePartyTransactionAsync(
            PartyTransaction transaction,
            CancellationToken cancellationToken = default);

        Task<bool> RemovePartyTransactionAsync(
            long id,
            CancellationToken cancellationToken = default);

        Task<Transaction> GetTransactionAsync(
            string txId,
            string currency,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateTransactionAsync(
            Transaction transaction,
            CancellationToken cancellationToken = default);

        Task<bool> RemoveTransactionAsync(
            long id,
            CancellationToken cancellationToken = default);
    }
}