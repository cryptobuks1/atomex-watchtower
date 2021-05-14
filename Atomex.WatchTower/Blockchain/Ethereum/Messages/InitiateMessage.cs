using System.Numerics;

using Nethereum.Hex.HexTypes;

namespace Atomex.WatchTower.Blockchain.Ethereum.Messages
{
    public class InitiateMessage
    {
        private const int PrefixLength = 2;
        private const int MethodLength = 8;
        private const int ParamOffset = PrefixLength + MethodLength;
        private const int ParamLength = 64;
        private const int InputLength = ParamOffset + 6 * ParamLength;

        public string SecretHash { get; set; }
        public string Participant { get; set; }
        public string Watcher { get; set; }
        public long RefundTimeStamp { get; set; }
        public bool WatcherForRedeem { get; set; }
        public BigInteger Payoff { get; set; }

        public static bool TryParse(string input, out InitiateMessage initiate)
        {
            initiate = null;

            if (input.Length != InputLength)
                return false;

            try
            {
                initiate = new InitiateMessage
                {
                    SecretHash       = input.Substring(ParamOffset, ParamLength),
                    Participant      = "0x" + input.Substring(ParamOffset + ParamLength, ParamLength),
                    Watcher          = "0x" + input.Substring(ParamOffset + 2 * ParamLength, ParamLength),
                    RefundTimeStamp  = (long)new HexBigInteger(input.Substring(ParamOffset + 3 * ParamLength, ParamLength)).Value,
                    WatcherForRedeem = new HexBigInteger(input.Substring(ParamOffset + 4 * ParamLength, ParamLength)).Value != BigInteger.Zero,
                    Payoff           = new HexBigInteger(input.Substring(ParamOffset + 5 * ParamLength, ParamLength)).Value
                };

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}