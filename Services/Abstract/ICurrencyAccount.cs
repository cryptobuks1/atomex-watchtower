﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Atomex.Blockchain.Abstract;
using Atomex.Core;
using Atomex.Entities;

namespace Atomex.Wallet.Abstract
{
    public interface ICurrencyAccount
    {
        event EventHandler<CurrencyEventArgs> BalanceUpdated;
        event EventHandler<TransactionEventArgs> UnconfirmedTransactionAdded;

        #region Common

        Task<Error> SendAsync(
            IEnumerable<WalletAddress> from,
            string to,
            decimal amount,
            decimal fee,
            decimal feePrice,
            bool useDefaultFee = false,
            CancellationToken cancellationToken = default);

        Task<Error> SendAsync(
            string to,
            decimal amount,
            decimal fee,
            decimal feePrice,
            bool useDefaultFee = false,
            CancellationToken cancellationToken = default);

        Task<decimal?> EstimateFeeAsync(
            string to,
            decimal amount,
            BlockchainTransactionType type,
            decimal fee = 0,
            decimal feePrice = 0,
            CancellationToken cancellationToken = default);

        Task<(decimal, decimal, decimal)> EstimateMaxAmountToSendAsync(
            string to,
            BlockchainTransactionType type,
            decimal fee = 0,
            decimal feePrice = 0,
            bool reserve = false,
            CancellationToken cancellationToken = default);

        Task<decimal> EstimateMaxFeeAsync(
            string to,
            decimal amount,
            BlockchainTransactionType type,
            CancellationToken cancellationToken = default);

        #endregion Common

        #region Balances

        Balance GetBalance();

        Task<Balance> GetAddressBalanceAsync(
            string address,
            CancellationToken cancellationToken = default);

        Task UpdateBalanceAsync(
            CancellationToken cancellationToken = default);

        Task UpdateBalanceAsync(
            string address,
            CancellationToken cancellationToken = default);

        #endregion Balances

        #region Addresses

        Task<WalletAddress> DivideAddressAsync(
            int chain,
            uint index,
            CancellationToken cancellationToken = default);

        Task<WalletAddress> GetAddressAsync(
            string address,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<WalletAddress>> GetUnspentAddressesAsync(
            CancellationToken cancellationToken = default);

        Task<IEnumerable<WalletAddress>> GetUnspentTokenAddressesAsync(
            CancellationToken cancellationToken = default);

        Task<IEnumerable<WalletAddress>> GetUnspentAddressesAsync(
            string toAddress,
            decimal amount,
            decimal fee,
            decimal feePrice,
            FeeUsagePolicy feeUsagePolicy,
            BlockchainTransactionType transactionType,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<SelectedWalletAddress>> SelectUnspentAddressesAsync(
            IList<WalletAddress> from,
            string to,
            decimal amount,
            decimal fee,
            decimal feePrice,
            FeeUsagePolicy feeUsagePolicy,
            BlockchainTransactionType transactionType,
            CancellationToken cancellationToken = default);
        
        Task<WalletAddress> GetFreeInternalAddressAsync(
            CancellationToken cancellationToken = default);

        Task<WalletAddress> GetFreeExternalAddressAsync(
            CancellationToken cancellationToken = default);

        Task<WalletAddress> GetRedeemAddressAsync(
            CancellationToken cancellationToken = default);

        Task<IEnumerable<WalletAddress>> GetAddressesAsync(
            CancellationToken cancellationToken = default);

        #endregion Addresses

        #region Transactions

        Task UpsertTransactionAsync(
            IBlockchainTransaction tx,
            bool updateBalance = false,
            bool notifyIfUnconfirmed = true,
            bool notifyIfBalanceUpdated = true,
            CancellationToken cancellationToken = default);

        Task<IBlockchainTransaction> GetTransactionByIdAsync(string txId);

        #endregion Transactions
    }
}