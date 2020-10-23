using Atomex.Currencies.Abstract;

namespace Atomex.Guard.Blockchain.Abstract
{
    public interface ITxOutput
    {
        uint Index { get; }
        long Value { get; }
        bool IsValid { get; }
        string TxId { get; }
        bool IsSpent { get; }
        TxPoint SpentTxPoint { get; set; }
        string DestinationAddress(ICurrency currency);
    }
}