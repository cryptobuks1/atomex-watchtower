using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace Atomex.Cryptography
{
    public class Ed25519
    {
        public static byte[] Sign(byte[] data, byte[] privateKey)
        {
            var key = new Ed25519PrivateKeyParameters(privateKey, 0);

            var signer = new Ed25519Signer();
            signer.Init(true, key);
            signer.BlockUpdate(data, 0, data.Length);
            return signer.GenerateSignature();
        }

        public static bool Verify(byte[] data, byte[] sign, byte[] publicKey)
        {
            var key = new Ed25519PublicKeyParameters(publicKey, 0);

            var signer = new Ed25519Signer();
            signer.Init(false, key);
            signer.BlockUpdate(data, 0, data.Length);
            return signer.VerifySignature(sign);
        }
    }
}