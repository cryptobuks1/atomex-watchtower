using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Newtonsoft.Json.Linq;

using Atomex.Guard.Blockchain.Abstract;
using Atomex.Guard.Blockchain.Ethereum.Dto;
using Atomex.Guard.Blockchain.Ethereum.Erc20.Dto;
using ERC20 = Atomex.Currencies.Erc20;

namespace Atomex.Guard.Blockchain.Ethereum.Erc20
{
    public class Erc20EtherScanApi : EtherScanApi
    {
        public Erc20EtherScanApi(ERC20 currency, EtherScanSettings settings)
            : base(currency, settings)
        {
        }

        public override async Task<BlockchainTransaction> GetTransactionAsync(
            string txId,
            CancellationToken cancellationToken = default)
        {
            return await GetTransactionAsync(
                txId: txId,
                transactionCreator: tx =>
                {
                    return new Erc20Transaction
                    {
                        Currency    = _currency.Name,
                        TxId        = txId,
                        BlockHeight = (long)new HexBigInteger(tx["blockNumber"].Value<string>()).Value,
                        Input       = tx["input"]?.Value<string>(),
                    };
                },
                cancellationToken: cancellationToken);
        }

        public override async Task<BlockchainTransaction> FindLockAsync(
            string secretHash,
            string contractAddress = null,
            string address = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            CancellationToken cancellationToken = default)
        {
            var contractSettings = _settings.Contracts
                .FirstOrDefault(s => s.Address.Equals(contractAddress, StringComparison.OrdinalIgnoreCase));

            if (contractSettings == null)
                throw new Exception($"Unknown contract address {contractAddress}");

            var events = await GetContractEventsAsync(
                address: contractAddress,
                fromBlock: contractSettings.StartBlock,
                toBlock: ulong.MaxValue,
                topic0: EventSignatureExtractor.GetSignatureHash<Erc20InitiatedEventDto>(),
                topic1: "0x" + secretHash,
                topic2: "0x000000000000000000000000" + _settings.TokenContract.Substring(2),
                topic3: "0x000000000000000000000000" + address.Substring(2),
                cancellationToken: cancellationToken);

            if (events == null || !events.Any())
                return null;

            if (!(await GetTransactionAsync(events.First().HexTransactionHash, cancellationToken) is Erc20Transaction lockTx))
                return null;

            Erc20InitiateMessage.TryParse(lockTx.Input, out var initiateMessage);

            if (initiateMessage.RefundTimeStamp < (long)(timeStamp + lockTime))
                return null;

            return lockTx;
        }

        public override async Task<IEnumerable<BlockchainTransaction>> FindAdditionalLocksAsync(
            string secretHash,
            string contractAddress = null,
            bool useCacheOnly = false,
            CancellationToken cancellationToken = default)
        {
            var contractSettings = _settings.Contracts
                .FirstOrDefault(s => s.Address.Equals(contractAddress, StringComparison.OrdinalIgnoreCase));

            if (contractSettings == null)
                throw new Exception($"Unknown contract address {contractAddress}");

            var events = await GetContractEventsAsync(
                address: contractAddress,
                fromBlock: contractSettings.StartBlock,
                toBlock: ulong.MaxValue,
                topic0: EventSignatureExtractor.GetSignatureHash<Erc20AddedEventDto>(),
                topic1: "0x" + secretHash,
                cancellationToken: cancellationToken);

            if (events == null || !events.Any())
                return Enumerable.Empty<BlockchainTransaction>();

            return await Task.WhenAll(events.Select(e => GetTransactionAsync(e.HexTransactionHash, cancellationToken)));
        }

        public override async Task<BlockchainTransaction> FindRedeemAsync(
            string secretHash,
            string contractAddress = null,
            string lockTxId = null,
            string address = null,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32,
            CancellationToken cancellationToken = default)
        {
            var contractSettings = _settings.Contracts
                .FirstOrDefault(s => s.Address.Equals(contractAddress, StringComparison.OrdinalIgnoreCase));

            if (contractSettings == null)
                throw new Exception($"Unknown contract address {contractAddress}");

            var events = await GetContractEventsAsync(
                address: contractAddress,
                fromBlock: contractSettings.StartBlock,
                toBlock: ulong.MaxValue,
                topic0: EventSignatureExtractor.GetSignatureHash<Erc20RedeemedEventDto>(),
                topic1: "0x" + secretHash,
                cancellationToken: cancellationToken);

            if (events == null || !events.Any())
                return null;

            return await GetTransactionAsync(events.First().HexTransactionHash, cancellationToken);
        }

        public override async Task<BlockchainTransaction> FindRefundAsync(
            string secretHash,
            string contractAddress = null,
            string lockTxId = null,
            string address = null,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32,
            CancellationToken cancellationToken = default)
        {
            var contractSettings = _settings.Contracts
                .FirstOrDefault(s => s.Address.Equals(contractAddress, StringComparison.OrdinalIgnoreCase));

            if (contractSettings == null)
                throw new Exception($"Unknown contract address {contractAddress}");

            var events = await GetContractEventsAsync(
                address: contractAddress,
                fromBlock: contractSettings.StartBlock,
                toBlock: ulong.MaxValue,
                topic0: EventSignatureExtractor.GetSignatureHash<Erc20RefundedEventDTO>(),
                topic1: "0x" + secretHash,
                cancellationToken: cancellationToken);

            if (events == null || !events.Any())
                return null;

            return await GetTransactionAsync(events.First().HexTransactionHash, cancellationToken);
        }
    }
}