using System.Collections.Generic;
using System.Linq;

namespace dm.TanTipBot.Models
{
    public class User
    {
        public int UserId { get; set; }
        public ulong DiscordId { get; set; }
        public string DiscordName { get; set; }
        public List<Wallet> Wallets { get; set; }

        public Wallet Wallet {
            get {
                return Wallets.FirstOrDefault();
            }
        }
    }
}
