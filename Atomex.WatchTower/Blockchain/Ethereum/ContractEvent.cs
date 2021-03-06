using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Atomex.WatchTower.Blockchain.Ethereum
{
    internal class Response<T>
    {
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }
        [JsonProperty(PropertyName = "result")]
        public T Result { get; set; }
    }

    public class ContractEvent
    {
        [JsonProperty(PropertyName = "address")]
        public string Address { get; set; }

        [JsonProperty(PropertyName = "topics")]
        public List<string> Topics { get; set; }

        [JsonProperty(PropertyName = "data")]
        public string HexData { get; set; }

        [JsonProperty(PropertyName = "blockNumber")]
        public string HexBlockNumber { get; set; }

        [JsonProperty(PropertyName = "timeStamp")]
        public string HexTimeStamp { get; set; }

        [JsonProperty(PropertyName = "gasPrice")]
        public string HexGasPrice { get; set; }

        [JsonProperty(PropertyName = "gasUsed")]
        public string HexGasUsed { get; set; }

        [JsonProperty(PropertyName = "logIndex")]
        public string HexLogIndex { get; set; }

        [JsonProperty(PropertyName = "transactionHash")]
        public string HexTransactionHash { get; set; }

        [JsonProperty(PropertyName = "transactionIndex")]
        public string HexTransactionIndex { get; set; }

        public string EventSignatureHash()
        {
            if (Topics != null && Topics.Count > 0)
                return Topics[0];

            throw new Exception("Contract event does not contain event signature hash");
        }
    }
}