using dm.TanTipBot.Common;

namespace dm.TanTipBot.Models
{
    public class Wallet
    {
        public int WalletId { get; set; }
        public Currency WalletType { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int Balance { get; set; }
        public string Identifier { get; set; }
        public string Password { get; set; }
        public string Address { get; set; }
        public string PublicKey { get; set; }
        public string SecretKey { get; set; }
    }
}
