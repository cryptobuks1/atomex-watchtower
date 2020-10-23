using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin;

using Atomex.Common;

namespace Atomex.WatchTower.Blockchain.Bitcoin
{
    public class BitcoinScript
    {
        private static Script GenerateHtlcP2PkhSwapPayment(
            string aliceRefundAddress,
            string bobAddress,
            long lockTimeStamp,
            byte[] secretHash,
            int secretSize,
            Network expectedNetwork = null)
        {
            // OP_IF
            //    <lockTimeStamp> OP_CHECKLOCKTIMEVERIFY OP_DROP OP_DUP OP_HASH160 <aliceRefundAddress> OP_EQUALVERIFY CHECKSIG
            // OP_ELSE
            //    OP_SIZE <secretSize> OP_EQUALVERIFY OP_HASH256 <secretHash> OP_EQUALVERIFY OP_DUP OP_HASH160 <bobAddress> OP_EQUALVERIFY OP_CHECKSIG
            // OP_ENDIF

            if (aliceRefundAddress == null)
                throw new ArgumentNullException(nameof(aliceRefundAddress));

            if (bobAddress == null)
                throw new ArgumentNullException(nameof(bobAddress));

            if (secretHash == null)
                throw new ArgumentNullException(nameof(secretHash));

            if (secretSize <= 0)
                throw new ArgumentException("Invalid Secret Size", nameof(secretSize));

            var aliceRefundAddressHash = new BitcoinPubKeyAddress(aliceRefundAddress, expectedNetwork).Hash;
            var bobAddressHash = new BitcoinPubKeyAddress(bobAddress, expectedNetwork).Hash;

            return new Script(new List<Op>
            {
                // if refund
                OpcodeType.OP_IF,
                Op.GetPushOp(lockTimeStamp),
                OpcodeType.OP_CHECKLOCKTIMEVERIFY,
                OpcodeType.OP_DROP,
                OpcodeType.OP_DUP,
                OpcodeType.OP_HASH160,
                Op.GetPushOp(aliceRefundAddressHash.ToBytes()),
                OpcodeType.OP_EQUALVERIFY,
                OpcodeType.OP_CHECKSIG,
                // else redeem
                OpcodeType.OP_ELSE,
                OpcodeType.OP_SIZE,
                Op.GetPushOp(secretSize),
                OpcodeType.OP_EQUALVERIFY,
                OpcodeType.OP_HASH256,
                Op.GetPushOp(secretHash),
                OpcodeType.OP_EQUALVERIFY,
                OpcodeType.OP_DUP,
                OpcodeType.OP_HASH160,
                Op.GetPushOp(bobAddressHash.ToBytes()),
                OpcodeType.OP_EQUALVERIFY,
                OpcodeType.OP_CHECKSIG,
                OpcodeType.OP_ENDIF
            });
        }

        public static long ExtractLockTimeFromHtlcP2PkhSwapPayment(string script) =>
            Script.FromHex(script)
                .ToOps()
                .ToList()[1]
                .GetLong() ?? 0;

        public static byte[] ExtractSecretHashFromP2PkhSwapPayment(string script) =>
            Script.FromHex(script)
                .ToOps()
                .ToList()[8]
                .PushData;

        public static byte[] ExtractSecretHashFromHtlcP2PkhSwapPayment(string script) =>
            new Script(script)
                .ToOps()
                .ToList()[14]
                .PushData;

        public static byte[] ExtractTargetPkhFromP2PkhSwapPayment(string script) =>
            Script.FromHex(script)
                .ToOps()
                .ToList()[12]
                .PushData;

        public static byte[] ExtractTargetPkhFromHtlcP2PkhSwapPayment(string script) =>
            Script.FromHex(script)
                .ToOps()
                .ToList()[18]
                .PushData;

        public static IEnumerable<byte[]> ExtractAllPushData(string script) =>
            Script.FromHex(script)
                .ToOps()
                .Where(op => op.PushData != null)
                .Select(op => op.PushData);

        public static bool IsP2PkhSwapPayment(string script)
        {
            var ops = Script.FromHex(script)
                .ToOps()
                .ToList();

            if (ops.Count != 16)
                return false;

            return ops[0].Code == OpcodeType.OP_IF &&
                   ops[1].Code == OpcodeType.OP_2 &&
                   ops[4].Code == OpcodeType.OP_2 &&
                   ops[5].Code == OpcodeType.OP_CHECKMULTISIG &&
                   ops[6].Code == OpcodeType.OP_ELSE &&
                   IsSwapHash(ops[7].Code) && //ops[7].Code == OpcodeType.OP_SHA256
                   ops[9].Code == OpcodeType.OP_EQUALVERIFY &&
                   ops[10].Code == OpcodeType.OP_DUP &&
                   ops[11].Code == OpcodeType.OP_HASH160 &&
                   ops[13].Code == OpcodeType.OP_EQUALVERIFY &&
                   ops[14].Code == OpcodeType.OP_CHECKSIG &&
                   ops[15].Code == OpcodeType.OP_ENDIF;
        }

        public static bool IsHtlcP2PkhSwapPayment(string script)
        {
            var ops = Script.FromHex(script)
                .ToOps()
                .ToList();

            if (ops.Count != 22)
                return false;

            return ops[0].Code == OpcodeType.OP_IF &&
                   ops[2].Code == OpcodeType.OP_CHECKLOCKTIMEVERIFY &&
                   ops[3].Code == OpcodeType.OP_DROP &&
                   ops[4].Code == OpcodeType.OP_DUP &&
                   ops[5].Code == OpcodeType.OP_HASH160 &&
                   ops[7].Code == OpcodeType.OP_EQUALVERIFY &&
                   ops[8].Code == OpcodeType.OP_CHECKSIG &&
                   ops[9].Code == OpcodeType.OP_ELSE &&
                   ops[10].Code == OpcodeType.OP_SIZE &&
                   ops[12].Code == OpcodeType.OP_EQUALVERIFY &&
                   IsSwapHash(ops[13].Code) &&
                   ops[15].Code == OpcodeType.OP_EQUALVERIFY &&
                   ops[16].Code == OpcodeType.OP_DUP &&
                   ops[17].Code == OpcodeType.OP_HASH160 &&
                   ops[19].Code == OpcodeType.OP_EQUALVERIFY &&
                   ops[20].Code == OpcodeType.OP_CHECKSIG &&
                   ops[21].Code == OpcodeType.OP_ENDIF;
        }

        public static bool IsSwapPayment(
            string script,
            string secretHash,
            string address,
            string refundAddress,
            long lockTimeStamp,
            int secretSize,
            Network network)
        {
            var secretHashBytes = Hex.FromString(secretHash);

            if (IsP2PkhSwapPayment(script) &&
                ExtractSecretHashFromP2PkhSwapPayment(script).SequenceEqual(secretHashBytes))
                return true;

            try
            {
                var paymentScript = GenerateHtlcP2PkhSwapPayment(
                    refundAddress,
                    address,
                    lockTimeStamp,
                    Hex.FromString(secretHash),
                    secretSize,
                    network);

                var paymentScriptHash = paymentScript
                    .Hash
                    .ScriptPubKey
                    .ToHex();

                if (paymentScriptHash == script)
                    return true;
            }
            catch
            {
            }

            try
            {
                // compatibility with old swaps, witch provide payment scripts
                var paymentScriptHash = new Script(Convert.FromBase64String(refundAddress))
                    .Hash
                    .ScriptPubKey
                    .ToHex();

                if (paymentScriptHash == script)
                    return true;
            }
            catch
            {
            }

            return false;
        }

        public static bool IsSwapHash(OpcodeType opcodeType) =>
            opcodeType == OpcodeType.OP_HASH160 ||
            opcodeType == OpcodeType.OP_HASH256 ||
            opcodeType == OpcodeType.OP_SHA256;

        public static bool IsP2PkhSwapRedeem(string script)
        {
            var ops = Script.FromHex(script)
                .ToOps()
                .ToList();

            if (ops.Count < 4)
                return false;

            return ops[^1].Code == OpcodeType.OP_FALSE;
        }

        public static bool IsP2PkhScriptSwapRedeem(string script)
        {
            var ops = Script.FromHex(script)
                .ToOps()
                .ToList();

            if (ops.Count < 5)
                return false;

            return ops[^2].Code == OpcodeType.OP_FALSE;
        }

        public static bool IsSwapRedeem(string script) =>
            IsP2PkhSwapRedeem(script) || IsP2PkhScriptSwapRedeem(script);

        public static bool IsP2PkhSwapRefund(string script)
        {
            var ops = Script.FromHex(script)
                .ToOps()
                .ToList();

            if (ops.Count < 3)
                return false;

            return ops[^1].Code != OpcodeType.OP_FALSE;
        }

        public static bool IsP2PkhScriptSwapRefund(string script)
        {
            var ops = Script.FromHex(script)
                .ToOps()
                .ToList();

            if (ops.Count < 4)
                return false;

            return ops[^2].Code != OpcodeType.OP_FALSE;
        }

        public static bool IsSwapRefund(string script) =>
            IsP2PkhSwapRefund(script) || IsP2PkhScriptSwapRefund(script);
    }
}