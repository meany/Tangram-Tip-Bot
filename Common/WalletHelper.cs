using dm.TanTipBot.Data;
using dm.TanTipBot.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TanDotNet;

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

        public static async Task<bool> TipUser(AppDbContext db, ITangramClient client, Wallet from, string user, Wallet to, int amount)
        {
            try
            {
                var message = await NodeHelper.Send(client, from, amount, to.Address, $"Tip from {user}", true).ConfigureAwait(false);
                var toNewBal = await NodeHelper.Receive(client, to, message).ConfigureAwait(false);

                var fromItem = await db.Wallets
                    .Where(x => x.WalletId == from.WalletId)
                    .SingleAsync()
                    .ConfigureAwait(false);
                var toItem = await db.Wallets
                    .Where(x => x.WalletId == to.WalletId)
                    .SingleAsync()
                    .ConfigureAwait(false);

                fromItem.Balance = fromItem.Balance - amount;
                db.Update(fromItem);
                toItem.Balance = toNewBal;
                db.Update(toItem);

                await db.SaveChangesAsync().ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
