using dm.TanTipBot.Data;
using dm.TanTipBot.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TanDotNet;
using TanDotNet.Models;

namespace dm.TanTipBot.Common
{
    public static class UserHelper
    {
        public static async Task<User> GetOrCreateUser(AppDbContext db, ITangramClient client, ulong discordId, string discordName)
        {
            var user = await GetUser(db, discordId).ConfigureAwait(false);
            if (user == null)
            {
                await CreateUser(db, client, discordId, discordName).ConfigureAwait(false);
                user = await GetUser(db, discordId).ConfigureAwait(false);
            }

            return user;
        }

        private static async Task<User> CreateDbUser(AppDbContext db, WalletAccount account, ulong discordId, string discordName)
        {
            var user = new User
            {
                DiscordId = discordId,
                DiscordName = discordName,
            };

            db.Users.Add(user);

            var wallet = new Wallet
            {
                Address = account.Address,
                Identifier = account.Identifier,
                Password = account.Password,
                PublicKey = account.PublicKey,
                //SecretKey = account.SecretKey,
                UserId = user.UserId,
                WalletType = Currency.Tangram,
            };

            db.Wallets.Add(wallet);

            await db.SaveChangesAsync().ConfigureAwait(false);

            return user;
        }

        private static async Task CreateUser(AppDbContext db, ITangramClient client, ulong discordId, string discordName)
        {
            var wallet = await NodeHelper.CreateWallet(client).ConfigureAwait(false);
            await CreateDbUser(db, wallet, discordId, discordName).ConfigureAwait(false);
        }

        private static async Task<Withdraw> GetLastWithdraw(AppDbContext db, Wallet wallet)
        {
            return await db.Withdraws
                .AsNoTracking()
                .Where(x => x.WalletId == wallet.WalletId)
                .OrderByDescending(x => x.Date)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }

        private static async Task<User> GetUser(AppDbContext db, ulong discordId)
        {
            return await db.Users
                .AsNoTracking()
                .Where(x => x.DiscordId == discordId)
                .Include(x => x.Wallets)
                .SingleOrDefaultAsync()
                .ConfigureAwait(false);
        }

        public static async Task<int> WithdrawCooldown(AppDbContext db, Config config, Wallet account)
        {
            var withdraw = await GetLastWithdraw(db, account).ConfigureAwait(false);
            if (withdraw != null)
            {
                return config.WithdrawCooldown - (int)Math.Round(DateTime.UtcNow.Subtract(withdraw.Date).TotalSeconds);
            }

            return 0;
        }
    }
}
