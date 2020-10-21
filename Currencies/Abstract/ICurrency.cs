namespace Atomex.Currencies.Abstract
{
    public interface ICurrency
    {
        string Name { get; }
        decimal DigitsMultiplier { get; }

        bool IsAddressFromKey(string address, byte[] publicKey);
        bool IsValidAddress(string address);
        bool Verify(byte[] data, byte[] sign, byte[] publicKey);
    }
}
