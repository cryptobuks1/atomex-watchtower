using System;
using System.Numerics;
using System.Linq;
using Newtonsoft.Json.Linq;

using Atomex.Common;
using Atomex.Cryptography;
using Atomex.WatchTower.Blockchain.Abstract;
using Atomex.WatchTower.Entities;

namespace Atomex.WatchTower.Blockchain.Tezos
{
    public class TezosTransaction : BlockchainTransaction
    {
        public BigInteger Amount { get; set; }
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
            return (decimal)Amount;
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
                "withdraw" => GetSecret(Params?["value"]?["args"]?[0]),
                "redeem" => GetSecret(Params?["value"]),
                _ => null
            };
        }

        public bool IsSwapInit(
            string secretHash,
            string participantAddress,
            ulong refundTimeStamp)
        {
            try
            {
                if (Params == null)
                    return false;

                var entrypoint = Params?["entrypoint"]?.ToString();

                return entrypoint switch
                {
                    "default" => IsSwapInit(Params?["value"]?["args"]?[0]?["args"]?[0], secretHash, participantAddress, refundTimeStamp),
                    "fund" => IsSwapInit(Params?["value"]?["args"]?[0], secretHash, participantAddress, refundTimeStamp),
                    "initiate" => IsSwapInit(Params?["value"], secretHash, participantAddress, refundTimeStamp),
                    _ => false
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
            string participantAddress,
            ulong refundTimeStamp)
        {
            return initParams?["args"]?[1]?["args"]?[0]?["args"]?[0]?["bytes"]?.Value<string>() == secretHash &&
                   initParams?["args"]?[1]?["args"]?[0]?["args"]?[1]?["int"]?.Value<ulong>() >= refundTimeStamp &&
                   initParams?["args"]?[0]?["string"]?.Value<string>() == participantAddress;
        }

        public bool IsSwapAdd(string secretHash)
        {
            try
            {
                if (Params == null)
                    return false;

                var entrypoint = Params?["entrypoint"]?.ToString();

                return entrypoint switch
                {
                    "default" => IsSwapAdd(Params?["value"]?["args"]?[0]?["args"]?[0], secretHash) &&
                                 Params?["value"]?["prim"]?.Value<string>() == "Left",
                    "fund" => IsSwapAdd(Params?["value"]?["args"]?[0], secretHash),
                    "add" => IsSwapAdd(Params?["value"], secretHash),
                    _ => false
                };
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsSwapAdd(JToken addParams, string secretHash)
        {
            return addParams?["bytes"]?.Value<string>() == secretHash;
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
                    "default" => GetSecret(Params?["value"]?["args"]?[0]?["args"]?[0]),
                    "withdraw" => GetSecret(Params?["value"]?["args"]?[0]),
                    "redeem" => GetSecret(Params?["value"]),
                    _ => null
                };

                if (paramSecretHex == null)
                    return false;

                var secretHashBytes = Hex.FromString(secretHash);
                var paramSecretBytes = Hex.FromString(paramSecretHex);
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

                if (entrypoint == "default" && Params?["value"]?["prim"]?.Value<string>() != "Right")
                    return false;

                var paramSecretHash = entrypoint switch
                {
                    "default" => GetSecretHash(Params?["value"]?["args"]?[0]?["args"]?[0]),
                    "withdraw" => GetSecretHash(Params?["value"]?["args"]?[0]),
                    "refund" => GetSecretHash(Params?["value"]),
                    _ => null
                };

                if (paramSecretHash == null)
                    return false;

                var secretHashBytes = Hex.FromString(secretHash);
                var paramSecretHashBytes = Hex.FromString(paramSecretHash);

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