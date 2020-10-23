using Atomex.Common;
using Atomex.Currencies.Abstract;
using Atomex.Entities;

namespace Atomex.Services.Abstract
{
    public interface IKeyStorage
    {
        SecureBytes GetPrivateKey(ICurrency currency, KeyIndex keyIndex);
        SecureBytes GetPublicKey(ICurrency currency, KeyIndex keyIndex);
    }
}