using System.Collections.Generic;
using NBitcoin;
using NBitcoin.Protocol;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.BlockStore;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.AzureIndexer;
using Stratis.Bitcoin.Utilities;
using System.Linq;

namespace Stratis.Bitcoin.Indexer.Console
{
    public class Program
    {
        public static void Main(string[] args)
        {
            List<string> agsList = new List<string>();

            agsList.Add("-debug=1");
            agsList.Add("-azureacc=ivantest9");
            agsList.Add("-azemu=0");
            agsList.Add("-azurekey=BnnVR/ruVbkxCvh75CStshRU5mWfKlGPhEjHcxtmWCYI7Plzb2CYkPcis9B7Ntw6W82X/y9aY03V1WzjO18+hQ==");
            args = agsList.ToArray();

            Network network = args.Contains("-testnet") ? Network.StratisTest : Network.StratisMain;

            if (NodeSettings.PrintHelp(args, network))
            {
                AzureIndexerSettings.PrintHelp(network);
                return;
            }

            NodeSettings nodeSettings = NodeSettings.FromArguments(args, "stratis", network, ProtocolVersion.ALT_PROTOCOL_VERSION);

            // NOTES: running BTC and STRAT side by side is not possible yet as the flags for serialization are static

            var node = new FullNodeBuilder()
                .UseNodeSettings(nodeSettings)
                .UseStratisConsensus()
                .UseBlockStore()
                .UseAzureIndexer()
                .Build();

            node.Run();
        }
    }
}
