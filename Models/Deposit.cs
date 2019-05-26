using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace dm.TanTipBot.Models
{
    public class Deposit
    {
        public int DepositId { get; set; }
        public int WalletId { get; set; }
        public Wallet Wallet { get; set; }
        public int Amount { get; set; }
        public string Hash { get; set; }
        public int Version { get; set; }
        public string Memo { get; set; }
        public DateTime Date { get; set; }
    }
}
