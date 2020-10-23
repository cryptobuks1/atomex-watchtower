using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Atomex.Common;
using Atomex.Cryptography;

namespace Atomex.Guard.Common
{
    public class HttpCache
    {
        private string _pathToCache;

        public HttpCache(string pathToCache)
        {
            _pathToCache = pathToCache;
        }

        public Task AddAsync(
            string url,
            string content,
            CancellationToken cancellationToken = default)
        {
            var fileName = Hex.ToHexString(Sha256.Compute(Encoding.UTF8.GetBytes(url))) + ".cache";

            var pathToFile = Path.Combine(_pathToCache, fileName);

            return File.WriteAllTextAsync(pathToFile, content, cancellationToken);
        }

        public async Task<string> GetAsync(
            string url,
            CancellationToken cancellationToken = default)
        {
            var fileName = Hex.ToHexString(Sha256.Compute(Encoding.UTF8.GetBytes(url))) + ".cache";

            var pathToFile = Path.Combine(_pathToCache, fileName);

            return File.Exists(pathToFile)
                ? await File.ReadAllTextAsync(pathToFile, cancellationToken)
                : null;
        }
    }
}