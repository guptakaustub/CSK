using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CSK
{
    internal class Program
    {
        private static Dictionary<string, bool> transactionVisited;
        private static readonly int TOTAL_COUNT = 2875;
        private static HttpClient httpClient;

        static Program()
        {
            httpClient = new HttpClient();
        }
        private static async Task<List<Transaction>> GetTransactions(int count)
        {
            string url = $"https://blockstream.info/api/block/000000000000000000076c036ff5119e5a5a74df77abf64203473364509f7732/txs/{count}";

            var httpResponse = await httpClient.GetAsync(url);
            var response = await httpResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Transaction>>(response);
        }
        public static async Task<List<Transaction>> GetTransactionInBlock()
        {
            // TODO - 2875
            List<Transaction> transactionList = new List<Transaction>();
            int count = 0;
            while (count < TOTAL_COUNT)
            {
                var transactions = await GetTransactions(count);
                transactionList.AddRange(transactions);
                count += 25;
            }

            return transactionList;
        }

        private static int CalculateHeight(string transactionid, Dictionary<string, Transaction> transactions)
        {
            int count = 1;
            transactionVisited[transactionid] = true;

            List<Input> inputs = transactions[transactionid].input;
            foreach (var txId in inputs.Select(x=>x.txnId))
            {
                if (transactions.ContainsKey(txId))
                {
                    count += CalculateHeight(txId, transactions);
                }
            }
            return count;
        }

        static async Task Main(string[] args)
        {
            var transactionList = await GetTransactionInBlock();
            transactionVisited = new Dictionary<string, bool>();
            Dictionary<string, Transaction> transactions = new Dictionary<string, Transaction>();
            
            foreach (var item in transactionList)
            {
                transactions.Add(item.txnId, item);
                transactionVisited[item.txnId] = false;
            }

            List<Output> ancestorCounts = new List<Output>();
            foreach (var transactionId in transactionList.Select(x=>x.txnId))
            {
                if (!transactionVisited[transactionId])
                {
                    ancestorCounts.Add(new Output()
                    {
                        transactionId = transactionId,
                        ancestorCount = CalculateHeight(transactionId, transactions)
                    });
                }
            }

            var outputArr = ancestorCounts.OrderByDescending(x => x.ancestorCount).Take(10);

            foreach (var output in outputArr)
            {
                Console.WriteLine($"txn id : {output.transactionId} with count {output.ancestorCount-1}");
            }
        }

        public class Output
        {
            public string transactionId { get; set; }
            public int ancestorCount { get; set; }
        }

        public class Transaction
        {
            [JsonProperty("txid")]
            public string txnId { get; set; }
            [JsonProperty("vin")]
            public List<Input> input { get; set; }
        }

        public class Input
        {
            [JsonProperty("txid")]
            public string txnId { get; set; }
        }
    }
}
