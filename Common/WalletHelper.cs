using dm.TanTipBot.Data;
using dm.TanTipBot.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace dm.TanTipBot.Common
{
    public static class WalletHelper
    {
        public static async Task<Wallet> UpdateWalletBalance(AppDbContext db, Wallet wallet, int balance)
        {
            var item = await db.Wallets
                .Where(x => x.WalletId == wallet.WalletId)
                .SingleAsync()
                .ConfigureAwait(false);

            item.Balance = balance;
            db.Update(item);

            await db.SaveChangesAsync().ConfigureAwait(false);

            return item;
        }
    }
}
