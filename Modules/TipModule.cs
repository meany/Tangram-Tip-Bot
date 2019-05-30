using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dm.TanTipBot.Common;
using dm.TanTipBot.Data;
using dm.TanTipBot.Models;
using Microsoft.Extensions.Options;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TanDotNet;

namespace dm.TanTipBot.Modules
{
    public class TipModule : ModuleBase
    {
        private readonly Config config;
        private readonly AppDbContext db;
        private readonly ITangramClient client;
        private static Logger log = LogManager.GetCurrentClassLogger();

        public TipModule(IOptions<Config> config, AppDbContext db, ITangramClient client)
        {
            this.config = config.Value;
            this.db = db;
            this.client = client;
        }

        [Command("tan")]
        [Summary("Sends a tip to users.")]
        [Remarks("Tip specified amount to one or many users via mention. The minimum tip is 1.\n" +
            "The recipients of you tip will be notified via DM.\n" +
            "Tips will be deducted from your available balance immediately.\n" +
            "Example: **.tan 2 @user1 @user2** would send `2` to `user1` and `2` to `user2`")]
        [Alias("t", "tanny", "tip")]
        public async Task Tip(int amount, params IUser[] users)
        {
            try
            {
                var emotes = new Emotes(Context);
                var wait = await emotes.Get(config.EmoteWait).ConfigureAwait(false);
                var msg = Context.Message;
                var cur = Currency.Tangram;
                await msg.AddReactionAsync(wait).ConfigureAwait(false);

                var cleanUsers = users.Distinct()
                    .Where(x => !x.IsBot && x != msg.Author);
                int totalUsers = cleanUsers.Count();
                if (totalUsers == 0)
                {
                    await msg.RemoveReactionAsync(wait, Context.Client.CurrentUser).ConfigureAwait(false);
                    await msg.AddReactionAsync(new Emoji(config.EmoteBad)).ConfigureAwait(false);
                    return;
                }

                var userFrom = await UserHelper.GetOrCreateUser(db, client, Context.User.Id, Context.User.ToString()).ConfigureAwait(false);
                if (userFrom.Wallet.Balance < (amount * totalUsers))
                {
                    await msg.RemoveReactionAsync(wait, Context.Client.CurrentUser).ConfigureAwait(false);
                    await msg.AddReactionAsync(new Emoji(config.EmoteBad)).ConfigureAwait(false);
                    return;
                }

                foreach (var clean in cleanUsers)
                {
                    var userTo = await UserHelper.GetOrCreateUser(db, client, clean.Id, clean.ToString()).ConfigureAwait(false);
                    await WalletHelper.TipUser(db, client, userFrom.Wallet, userFrom.DiscordName, userTo.Wallet, amount).ConfigureAwait(false);
                    await Discord.SendDMAsync(clean, message: $"You have been tipped **{amount} {cur.ToString(true)}** by {msg.Author.Username}").ConfigureAwait(false);
                }

                await msg.RemoveReactionAsync(wait, Context.Client.CurrentUser);
                await msg.AddReactionAsync(new Emoji(config.EmoteGood)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error(ex);
                throw ex;
            }
        }
    }
}