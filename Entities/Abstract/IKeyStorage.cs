using Atomex.Common;
using Atomex.Currencies.Abstract;

namespace Atomex.Entities.Abstract
{
    public interface IKeyStorage
    {
        SecureBytes GetPrivateKey(ICurrency currency, KeyIndex keyIndex);
        SecureBytes GetPublicKey(ICurrency currency, KeyIndex keyIndex);
    }
}