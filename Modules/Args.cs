using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using dm.TanTipBot.Common;
using dm.TanTipBot.Data;
using Microsoft.EntityFrameworkCore;
using NLog;
using TanDotNet;

namespace dm.TanTipBot
{
    public class Args
    {
        private readonly DiscordSocketClient discordClient;
        private readonly ITangramClient tangramClient;
        private readonly AppDbContext db;
        private static Logger log = LogManager.GetCurrentClassLogger();

        public Args(DiscordSocketClient discordClient, ITangramClient tangramClient, AppDbContext db)
        {
            this.discordClient = discordClient;
            this.tangramClient = tangramClient;
            this.db = db;
        }

        public async Task Deposit(int depositId)
        {
            try
            {
                var deposit = await db.Deposits
                    .AsNoTracking()
                    .Where(x => x.DepositId == depositId)
                    .Include(x => x.Wallet)
                    .Include(x => x.Wallet.User)
                    .SingleOrDefaultAsync()
                    .ConfigureAwait(false);

                var account = deposit.Wallet;
                var user = discordClient.GetUser(account.User.DiscordId);
                string icon = account.WalletType.GetIcon();
                string desc = $"A deposit of **{deposit.Amount.Format()} {account.WalletType.ToString(true)}** was received.\n";

                var bal = await NodeHelper.GetBalance(tangramClient, account).ConfigureAwait(false);

                var output = new EmbedBuilder()
                    .WithColor(Color.SUCCESS)
                    .WithAuthor(author =>
                    {
                        author.WithName($"{account.WalletType.ToString(true)} Deposit")
                            .WithIconUrl(icon);
                    })
                    .WithDescription(desc)
                    .AddField("Total Available Funds",
                        $"```ml\n" +
                        $"{bal.Format()} {account.WalletType.ToString(true)}```")
                    .AddField("Memo",
                        $"{deposit.Memo}")
                    .AddField("Hash",
                        $"{deposit.Hash}")
                    .WithFooter(footer =>
                    {
                        footer.WithText($"If you have any issues with funds, please DM an admin")
                            .WithIconUrl(Asset.SUCCESS);
                    });

                await Discord.SendDMAsync(user, output).ConfigureAwait(false);
            } catch (Exception ex)
            {
                log.Error(ex);
            }
        }
    }
}