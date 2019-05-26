using Discord;
using Discord.Commands;
using dm.TanTipBot.Common;
using dm.TanTipBot.Data;
using dm.TanTipBot.Models;
using Microsoft.Extensions.Options;
using NLog;
using System;
using System.IO;
using System.Threading.Tasks;
using TanDotNet;

namespace dm.TanTipBot.Modules
{
    public class WalletModule : ModuleBase
    {
        private readonly Config config;
        private readonly AppDbContext db;
        private readonly ITangramClient client;
        private static Logger log = LogManager.GetCurrentClassLogger();

        public WalletModule(IOptions<Config> config, AppDbContext db, ITangramClient client)
        {
            this.config = config.Value;
            this.db = db;
            this.client = client;
        }

        [Command("funds")]
        [Summary("You can use this command to review the available funds for the supported currency. " +
            "Also, this command provides your deposit address, and receives funds from the message pool.")]
        [Remarks("If you have no deposit address, the first time you run this command it will be generated.\n" +
            "Deposits can take up to one minute to become available from the message pool.\n" +
            "You can skip the deposit process by adding `true` after the command.\n" +
            "**Sending funds from other exchanges is not supported!** Do so at your own risk.")]
        [Alias("f", "bal", "balance", "deposit", "receive", "register")]
        public async Task Deposit(bool skipDepositCheck = false)
        {
            try
            {
                var user = await UserHelper.GetOrCreateUser(db, client, Context.User.Id, Context.User.ToString()).ConfigureAwait(false);
                var output = new EmbedBuilder();
                var cur = Currency.Tangram;
                string icon = string.Empty;
                uint color = 0x0;
                string addr = string.Empty;
                switch (cur)
                {
                    case Currency.Tangram:
                        addr = user.Wallet.Address;
                        icon = Asset.TANGRAM;
                        color = Color.TANGRAM_BLUE;
                        break;
                }

                if (!skipDepositCheck)
                {
                    output.WithColor(color)
                    .WithDescription($"<a:{config.EmoteWait}> Please wait while we check for new transactions...")
                    .WithFooter(footer =>
                    {
                        footer.WithText("If you have any issues, please DM an admin.");
                    });

                    var tempMsg = await Discord.ReplyDMAsync(Context, output).ConfigureAwait(false);

                    var receive = await NodeHelper.Receive(client, user.Wallet);
                    if (receive > user.Wallet.Balance)
                    {
                        await tempMsg.DeleteAsync().ConfigureAwait(false);

                        int add = receive - user.Wallet.Balance;
                        var wallet = await WalletHelper.UpdateWalletBalance(db, user.Wallet, receive).ConfigureAwait(false);

                        string title = "Funds Received";
                        color = Color.TANGRAM_PINK;
                        output = new EmbedBuilder();
                        output.WithColor(color)
                        .WithAuthor(author =>
                        {
                            author.WithName(title)
                                .WithIconUrl(icon);
                        })
                        .AddField("Deposit Received",
                            $"We found **{add.Format()} {cur.ToString(true)}** in the message pool and received the funds.\n" +
                            $"```ml\n" +
                            $"Available: {wallet.Balance.Format()} {cur.ToString(true)}```")
                        .WithFooter(footer =>
                        {
                            footer.WithText("If you have any issues, please DM an admin.");
                        });

                        await Discord.ReplyDMAsync(Context, output).ConfigureAwait(false);
                    }
                    else
                    {
                        await tempMsg.DeleteAsync().ConfigureAwait(false);
                        await ShowFundsInfo(icon, addr, cur, user).ConfigureAwait(false);
                    }
                }
                else
                {
                    await ShowFundsInfo(icon, addr, cur, user).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
                throw ex;
            }
        }

        private async Task ShowFundsInfo(string icon, string addr, Currency cur, User user)
        {
            try
            {
                string fileName = $"qr-{addr}.png";
                uint dark = Color.BLACK;
                uint light = Color.WHITE;

                var title = "Funds Information";
                var output = new EmbedBuilder();
                output.WithColor(Color.TANGRAM_BLUE)
                .WithAuthor(author =>
                {
                    author.WithName(title)
                        .WithIconUrl(icon);
                })
                .WithImageUrl($"attachment://{fileName}")
                .AddField($"Balance",
                    $"```ml\n" +
                    $"Available: {user.Wallet.Balance.Format()} {cur.ToString(true)}```")
                .AddField("Important Information",
                    "Deposits should take only a few seconds to become available, but can sometimes take up to one minute. " +
                    "Be sure to double check the **address** before sending any funds! " +
                    "**Sending funds from other exchanges is not supported!** Do so at your own risk.")
                .AddField("Deposit Address", $"```{addr}```")
                .WithFooter(footer =>
                {
                    footer.WithText("If you have any issues, please DM an admin.");
                });

                Stream stream = new MemoryStream();
                var darkColor = System.Drawing.Color.FromArgb((int)dark);
                var lightColor = System.Drawing.Color.FromArgb((int)light);
                QR.Generate(addr, darkColor, lightColor, ref stream);
                stream.Seek(0, SeekOrigin.Begin);
                await Discord.ReplyDMAsync(Context, output, stream, fileName).ConfigureAwait(false);
                stream.Dispose();
            }
            catch (Exception ex)
            {
                log.Error(ex);
                throw ex;
            }
        }

        [Command("withdraw")]
        [Summary("Withdraw some or all of your funds")]
        [Remarks("You must specify an amount and destination address.\n" +
            "Withdraws should take only a few seconds to execute, but can sometimes take up to one minute.\n" +
            "**Sending funds to other exchanges is not supported!** Do so at your own risk.")]
        [Alias("w", "send")]
        public async Task Withdraw(int amount, string destination, string memo = null)
        {
            try
            {
                var output = new EmbedBuilder();
                string icon = string.Empty;
                var cur = Currency.Tangram;
                switch (cur)
                {
                    case Currency.Tangram:
                        icon = Asset.TANGRAM;
                        break;
                }

                output.WithAuthor(author =>
                {
                    author.WithName($"Withdraw")
                        .WithIconUrl(icon);
                });

                if (amount <= 0)
                {
                    WithdrawError("Cannot withdraw zero or less amount.", ref output);
                }
                //else if (!await NodeHelper.IsValidAddress(node, address).ConfigureAwait(false))
                //{
                //    WithdrawError($"Address **{address}** invalid. Double check your destination address and currency selections.", ref output);
                //}
                else
                {
                    var user = await UserHelper.GetOrCreateUser(db, client, Context.User.Id, Context.User.ToString()).ConfigureAwait(false);
                    //switch (cur)
                    //{
                    //    case Currency.Tangram:
                    //        wallet = user.Wallet;
                    //        break;
                    //}

                    int cooldown = await UserHelper.WithdrawCooldown(db, config, user.Wallet).ConfigureAwait(false);
                    if (cooldown > 0)
                    {
                        WithdrawError($"Cannot withdraw yet, please wait another **{cooldown}** seconds (withdraws must 'cooldown').", ref output);
                    }
                    else if (user.Wallet.Balance < amount)
                    {
                        WithdrawError($"Cannot withdraw **{amount.Format()} {cur.ToString(true)}**, amount too high. Double check your available balance.", ref output);
                    }
                    else
                    {
                        var withdraw = await NodeHelper.Send(client, user.Wallet, amount, destination, memo).ConfigureAwait(false);

                        await WalletHelper.UpdateWalletBalance(db, user.Wallet, withdraw).ConfigureAwait(false);

                        output.WithColor(Color.SUCCESS)
                        .WithDescription($"A withdraw amount of **{amount.Format()} {cur.ToString(true)}** was processed.")
                        .AddField($"Balance",
                            $"```ml\n" +
                            $"Available: {withdraw.Format()} {cur.ToString(true)}```")
                        .WithFooter(footer =>
                        {
                            footer.WithText($"If you have any issues with funds, please DM an admin")
                                .WithIconUrl(Asset.SUCCESS);
                        });
                    }
                }

                await Discord.ReplyDMAsync(Context, output).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error(ex);
                throw ex;
            }
        }

        private void WithdrawError(string error, ref EmbedBuilder output)
        {
            output.WithColor(Color.ERROR)
            .WithDescription(error)
            .WithFooter(footer =>
            {
                footer.WithText($"If you have any issues with funds, please DM an admin")
                    .WithIconUrl(Asset.ERROR);
            });
        }
    }
}