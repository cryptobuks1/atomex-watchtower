﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atomex.Blockchain.Abstract;
using Atomex.Entities;
using Atomex.Guard.Blockchain.Abstract;

namespace Atomex.Services.Abstract
{
    public interface IAccountDataRepository
    {
        #region Addresses

        Task<bool> UpsertAddressAsync(
            WalletAddress walletAddress);
        Task<int> UpsertAddressesAsync(
            IEnumerable<WalletAddress> walletAddresses);
        Task<bool> TryInsertAddressAsync(
            WalletAddress walletAddress);
        Task<WalletAddress> GetWalletAddressAsync(
            string currency,
            string address);
        Task<WalletAddress> GetLastActiveWalletAddressAsync(
            string currency,
            int chain);
        Task<IEnumerable<WalletAddress>> GetUnspentAddressesAsync(
            string currency,
            bool includeUnconfirmed = true);

        Task<IEnumerable<WalletAddress>> GetAddressesAsync(
            string currency);

        #endregion Addresses

        #region Transactions

        Task<bool> UpsertTransactionAsync(
            IBlockchainTransaction tx);
        Task<IBlockchainTransaction> GetTransactionByIdAsync(
            string currency,
            string txId,
            Type transactionType);
        Task<IEnumerable<IBlockchainTransaction>> GetTransactionsAsync(
            string currency,
            Type transactionType);
        Task<IEnumerable<IBlockchainTransaction>> GetUnconfirmedTransactionsAsync(
            string currency,
            Type transactionType);
        Task<bool> RemoveTransactionByIdAsync(
            string id);

        #endregion Transactions

        #region Outputs

        Task<bool> UpsertOutputsAsync(
            IEnumerable<ITxOutput> outputs,
            string currency,
            string address);
        Task<IEnumerable<ITxOutput>> GetAvailableOutputsAsync(
            string currency,
            Type outputType,
            Type transactionType);
        Task<IEnumerable<ITxOutput>> GetAvailableOutputsAsync(
            string currency,
            string address,
            Type outputType,
            Type transactionType);
        Task<IEnumerable<ITxOutput>> GetOutputsAsync(
            string currency,
            Type outputType);
        Task<IEnumerable<ITxOutput>> GetOutputsAsync(
            string currency,
            string address,
            Type outputType);
        Task<ITxOutput> GetOutputAsync(
            string currency,
            string txId,
            uint index,
            Type outputType);

        #endregion Outputs

        #region Swaps

        Task<bool> AddSwapAsync(Swap swap);
        Task<bool> UpdateSwapAsync(Swap swap);
        Task<Swap> GetSwapByIdAsync(long id);
        Task<IEnumerable<Swap>> GetSwapsAsync();

        #endregion Swaps
    }
}