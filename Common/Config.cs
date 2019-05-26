using System.Collections.Generic;

namespace dm.TanTipBot.Common
{
    public class Config
    {
        public string BotName { get; set; }
        public string BotPrefix { get; set; }
        public string BotToken { get; set; }
        public List<ulong> ChannelIds { get; set; }
        public string EmoteGood { get; set; }
        public string EmoteBad { get; set; }
        public string EmoteWait { get; set; }
        public int RequestCooldown { get; set; }
        public Node Node { get; set; }
        public int WithdrawCooldown { get; set; }
    }

    public class Node
    {
        public string Url { get; set; }
        public string VaultShard { get; set; }
    }
}
