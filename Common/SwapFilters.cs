using System;
using System.Linq.Expressions;

using Atomex.Entities;

namespace Atomex.Common
{
    public static class SwapFilters
    {
        public static Expression<Func<Swap, bool>> Completed = s =>
            ((s.Initiator.Status == PartyStatus.Redeemed ||
                s.Initiator.Status == PartyStatus.Refunded ||
                s.Initiator.Status == PartyStatus.Lost ||
                s.Initiator.Status == PartyStatus.Jackpot) &&
            (s.Acceptor.Status == PartyStatus.Redeemed ||
                s.Acceptor.Status == PartyStatus.Refunded ||
                s.Acceptor.Status == PartyStatus.Lost ||
                s.Acceptor.Status == PartyStatus.Jackpot)) ||
            (s.Initiator.Status == PartyStatus.Refunded &&
                s.Acceptor.Status == PartyStatus.Created);

        public static Expression<Func<Swap, bool>> Active = s =>
            ((s.Initiator.Status != PartyStatus.Redeemed &&
                s.Initiator.Status != PartyStatus.Refunded &&
                s.Initiator.Status != PartyStatus.Lost &&
                s.Initiator.Status != PartyStatus.Jackpot) ||
            (s.Acceptor.Status != PartyStatus.Redeemed &&
                s.Acceptor.Status != PartyStatus.Refunded &&
                s.Acceptor.Status != PartyStatus.Lost &&
                s.Acceptor.Status != PartyStatus.Jackpot)) &&
            (s.Initiator.Status != PartyStatus.Refunded ||
                s.Acceptor.Status != PartyStatus.Created);
    }
}