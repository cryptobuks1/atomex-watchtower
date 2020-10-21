using System;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json.Linq;

using Atomex.Entities;
using Atomex.Guard.Blockchain.Abstract;
using Atomex.Common;
using Atomex.Cryptography;

namespace Atomex.Guard.Blockchain.Tezos.Fa2
{
    public class Fa2Transaction : BlockchainTransaction
    {
        public string To { get; set; }
        public JObject Params { get; set; }

        public override bool IsConfirmed => Status == TransactionStatus.Confirmed;

        public override decimal GetAmount(
            string secretHash = null,
            string participantAddress = null,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32)
        {
            if (Params == null)
                return 0m;

            var entrypoint = Params?["entrypoint"]?.ToString();

            return entrypoint switch
            {
                "default"  => GetAmount(Params?["value"]?["args"]?[0]?["args"]?[0]),
                "initiate" => GetAmount(Params?["value"]),
                _          => 0m
            };
        }

        public override string GetSecret(
            string secretHash = null,
            int secretSize = 32)
        {
            if (Params == null)
                return null;

            var entrypoint = Params?["entrypoint"]?.ToString();

            return entrypoint switch
            {
                "default" => GetSecret(Params?["value"]?["args"]?[0]?["args"]?[0]),
                "redeem"  => GetSecret(Params?["value"]),
                _ => null
            };
        }

        private static decimal GetAmount(JToken initParams)
        {
            return initParams?["args"]?[1]?["args"]?[1]?["int"]?.ToObject<decimal>() ?? 0m;
        }

        public bool IsSwapInit(
            string secretHash,
            string tokenContractAddress,
            string participantAddress,
            ulong refundTimeStamp,
            long tokenId)
        {
            try
            {
                if (Params == null)
                    return false;

                var entrypoint = Params?["entrypoint"]?.ToString();

                return entrypoint switch
                {
                    "default"  => IsSwapInit(Params?["value"]?["args"]?[0]?["args"]?[0], secretHash, tokenContractAddress, participantAddress, refundTimeStamp, tokenId),
                    "initiate" => IsSwapInit(Params?["value"], secretHash, tokenContractAddress, participantAddress, refundTimeStamp, tokenId),
                    _          => false
                };
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsSwapInit(
            JToken initParams,
            string secretHash,
            string tokenContractAddress,
            string participantAddress,
            ulong refundTimeStamp,
            long tokenId)
        {
            return initParams?["args"]?[0]?["args"]?[0]?["args"]?[0]?["bytes"]?.Value<string>() == secretHash &&
                   initParams?["args"]?[0]?["args"]?[0]?["args"]?[1]?["string"]?.Value<string>() == participantAddress &&
                   initParams?["args"]?[0]?["args"]?[1]?["args"]?[0]?["string"]?.Value<ulong>() == refundTimeStamp &&
                   initParams?["args"]?[0]?["args"]?[1]?["args"]?[1]?["string"]?.Value<string>() == tokenContractAddress &&
                   initParams?["args"]?[1]?["args"]?[0]?["int"]?.Value<int>() == tokenId;
        }

        public bool IsSwapRedeem(string secretHash)
        {
            try
            {
                if (Params == null)
                    return false;

                var entrypoint = Params?["entrypoint"]?.ToString();

                var paramSecretHex = entrypoint switch
                {
                    "default"  => GetSecret(Params?["value"]?["args"]?[0]?["args"]?[0]),
                    "redeem"   => GetSecret(Params?["value"]),
                    _          => null
                };

                if (paramSecretHex == null)
                    return false;

                var secretHashBytes = secretHash.FromHexToByteArray();
                var paramSecretBytes = paramSecretHex.FromHexToByteArray();
                var paramSecretHashBytes = Sha256.Compute(paramSecretBytes, iterations: 2);

                return paramSecretHashBytes.SequenceEqual(secretHashBytes);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string GetSecret(JToken redeemParams)
        {
            return redeemParams?["bytes"]?.Value<string>();
        }

        public bool IsSwapRefund(string secretHash)
        {
            try
            {
                if (Params == null)
                    return false;

                var entrypoint = Params?["entrypoint"]?.ToString();

                var paramSecretHash = entrypoint switch
                {
                    "default"  => GetSecretHash(Params?["value"]?["args"]?[0]),
                    "refund"   => GetSecretHash(Params?["value"]),
                    _          => null
                };

                if (paramSecretHash == null)
                    return false;

                var secretHashBytes = secretHash.FromHexToByteArray();
                var paramSecretHashBytes = paramSecretHash.FromHexToByteArray();

                return paramSecretHashBytes.SequenceEqual(secretHashBytes);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string GetSecretHash(JToken refundParams)
        {
            return refundParams?["bytes"]?.Value<string>();
        }
    }
}