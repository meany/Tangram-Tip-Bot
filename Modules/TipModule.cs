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
        [Summary("Sends a Tangram tip to the mentioned users.")]
        [Remarks("Tip specified amount to the mentioned user(s) (minimum tip is 1).\n" +
            "The recipients will be notified of your tip via DM.\n" +
            "Successful tips will be deducted from your available balance immediately.\n" +
            "Example: **.tan 2 @user1 @user2** would send `2` to `user1` and `2` to `user2`")]
        [Alias("t", "tanny", "tip")]
        public async Task Tip(int amount, params IUser[] users)
        {
            try
            {
                var cleanUsers = users.Distinct()
                    .Where(x => !x.IsBot && x != Context.Message.Author);

                // TODO: valid tip?

                var user = await UserHelper.GetOrCreateUser(db, client, Context.User.Id, Context.User.ToString()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error(ex);
                throw ex;
            }
        }
    }
}