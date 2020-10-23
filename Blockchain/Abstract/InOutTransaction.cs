using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Atomex.Blockchain.Abstract;
using Atomex.Entities.Abstract;
using Atomex.Services.Abstract;

namespace Atomex.Guard.Blockchain.Abstract
{
    public interface IInOutTransaction : IBlockchainTransaction
    {
        TxPoint[] Inputs { get; }
        ITxOutput[] Outputs { get; }
        long? Fees { get; set; }
        long Amount { get; set; }

        Task<bool> SignAsync(
            IAddressResolver addressResolver,
            IKeyStorage keyStorage,
            IEnumerable<ITxOutput> spentOutputs,
            CancellationToken cancellationToken = default);
    }
}