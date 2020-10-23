namespace Atomex.Guard.Blockchain.Abstract
{
    public class TxPoint
    {
        public uint Index { get; }
        public string Hash { get; }

        public TxPoint(uint index, string hash)
        {
            Index = index;
            Hash = hash;
        }
    }
}