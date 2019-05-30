using Discord;
using Discord.Commands;
using dm.TanTipBot.Common;
using dm.TanTipBot.Data;
using dm.TanTipBot.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NLog;
using System;
using System.IO;
using System.Text;
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

        [Command("balance")]
        [Summary("Review the balance for the supported currency.")]
        [Remarks("If you have no deposit address, the first time you run this command it will be generated.\n" +
            "This command is similar to the `funds` command, but skips retrieving messages from the network.")]
        [Alias("b", "bal", "register")]
        public async Task Balance()
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

            await ShowFundsInfo(icon, addr, cur, user, false).ConfigureAwait(false);
        }

        [Command("funds")]
        [Summary("Receives funds from the network.\n" +
            "This command also provides your deposit address and available funds for the supported currency.")]
        [Remarks("Deposits can take a few seconds to become available on the network.\n" +
            "You can skip the deposit process by running the `balance` command.\n" +
            "**Sending funds from other exchanges is not supported!** Do so at your own risk.")]
        [Alias("f", "deposit", "receive")]
        public async Task Deposit()
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

                var emotes = new Emotes(Context);
                var wait = await emotes.Get(config.EmoteWait);
                output.WithColor(color)
                .WithDescription($"{wait} Please wait while we check for new transactions...")
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

                    string title = $"{cur.ToString(false)} Deposit";
                    color = Color.TANGRAM_PINK;
                    output = new EmbedBuilder();
                    output.WithColor(color)
                    .WithAuthor(author =>
                    {
                        author.WithName(title)
                            .WithIconUrl(icon);
                    })
                    .WithDescription($"We found **{add.Format()} {cur.ToString(true)}** in the message pool and received the funds.")
                    .AddField("Balance",
                        $"```ml\n" +
                        $"Available: {wallet.Balance.Format()} {cur.ToString(true)}```")
                    .WithFooter(footer =>
                    {
                        footer.WithText("If you have any issues, please DM an admin.");
                    });

                    await Discord.SendDMAsync(Context.User, output).ConfigureAwait(false);
                }
                else
                {
                    await tempMsg.DeleteAsync().ConfigureAwait(false);
                    await ShowFundsInfo(icon, addr, cur, user).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
                throw ex;
            }
        }

        private async Task ShowFundsInfo(string icon, string addr, Currency cur, User user, bool showDepositInfo = true)
        {
            try
            {
                uint dark = Color.BLACK;
                uint light = Color.WHITE;

                var title = $"{cur.ToString(false)} Funds";
                var output = new EmbedBuilder();
                output.WithColor(Color.TANGRAM_BLUE)
                .WithAuthor(author =>
                {
                    author.WithName(title)
                        .WithIconUrl(icon);
                })
                .WithDescription($"```ml\n" +
                    $"Available: {user.Wallet.Balance.Format()} {cur.ToString(true)}```")
                .WithFooter(footer =>
                {
                    footer.WithText("If you have any issues, please DM an admin.");
                });

                if (showDepositInfo)
                {
                    string fileName = $"qr-{addr}.png";

                    output.WithImageUrl($"attachment://{fileName}")
                    .AddField("Important Information",
                    "Deposits should take only a few seconds to become available, but can sometimes take up to one minute. " +
                    "Be sure to double check the **address** before sending any funds! " +
                    "**Sending funds from other exchanges is not supported!** Do so at your own risk.")
                    .AddField("Deposit Address", $"```{addr}```");

                    Stream stream = new MemoryStream();
                    var darkColor = System.Drawing.Color.FromArgb((int)dark);
                    var lightColor = System.Drawing.Color.FromArgb((int)light);
                    QR.Generate(addr, darkColor, lightColor, ref stream);
                    stream.Seek(0, SeekOrigin.Begin);

                    await Discord.ReplyDMAsync(Context, output, stream, fileName).ConfigureAwait(false);

                    stream.Dispose();
                }
                else
                {
                    await Discord.ReplyDMAsync(Context, output).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
                throw ex;
            }
        }

        [Command("withdraw")]
        [Summary("Withdraw some or all of your funds.")]
        [Remarks("You must specify an amount and destination address.\n" +
            "Withdraws broadcast to the network should take only a few seconds to execute, but can sometimes take up to one minute.\n" +
            "If you specify `true` for `createRedemptionKey`, the file will be sent via DM\n" +
            "**Sending funds to other exchanges is not supported!** Do so at your own risk.")]
        [Alias("w", "send")]
        public async Task Withdraw(int amount, string destination, bool createRedemptionKey = false, string memo = null)
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
                    await Discord.ReplyDMAsync(Context, output).ConfigureAwait(false);
                    return;
                }

                //if (!await NodeHelper.IsValidAddress(node, address).ConfigureAwait(false))
                //{
                //    WithdrawError($"Address **{address}** invalid. Double check your destination address and currency selections.", ref output);
                //    await Discord.ReplyDMAsync(Context, output).ConfigureAwait(false)
                //    return;
                //}

                var user = await UserHelper.GetOrCreateUser(db, client, Context.User.Id, Context.User.ToString()).ConfigureAwait(false);
                int cooldown = await UserHelper.WithdrawCooldown(db, config, user.Wallet).ConfigureAwait(false);

                if (cooldown > 0)
                {
                    WithdrawError($"Cannot withdraw yet, please wait another **{cooldown}** seconds (withdraws must 'cooldown').", ref output);
                    await Discord.ReplyDMAsync(Context, output).ConfigureAwait(false);
                    return;

                }

                if (user.Wallet.Balance < amount)
                {
                    WithdrawError($"Cannot withdraw **{amount.Format()} {cur.ToString(true)}**, amount too high. Double check your available balance.", ref output);
                    await Discord.ReplyDMAsync(Context, output).ConfigureAwait(false);
                    return;
                }

                int newBal = user.Wallet.Balance - amount;

                output.WithColor(Color.SUCCESS)
                .WithDescription($"A withdraw amount of **{amount.Format()} {cur.ToString(true)}** was processed.")
                .AddField($"Balance",
                    $"```ml\n" +
                    $"Available: {newBal} {cur.ToString(true)}```")
                .WithFooter(footer =>
                {
                    footer.WithText($"If you have any issues with funds, please DM an admin")
                        .WithIconUrl(Asset.SUCCESS);
                });

                if (createRedemptionKey)
                {
                    var withdraw = await NodeHelper.Send(client, user.Wallet, amount, destination, memo, true).ConfigureAwait(false);

                    await WalletHelper.UpdateWalletBalance(db, user.Wallet, newBal).ConfigureAwait(false);

                    string fileName = $"redem{DateTime.Now.GetHashCode()}.rdkey";
                    output.WithImageUrl($"attachment://{fileName}");
                    string content =
                        "--------------Begin Redemption Key--------------" +
                        Environment.NewLine +
                        JsonConvert.SerializeObject(withdraw) +
                        Environment.NewLine +
                        "--------------End Redemption Key----------------";

                    Stream stream = new MemoryStream(Encoding.ASCII.GetBytes(content));
                    stream.Seek(0, SeekOrigin.Begin);

                    await Discord.SendDMAsync(Context.User, output).ConfigureAwait(false);
                    var channel = await Context.User.GetOrCreateDMChannelAsync().ConfigureAwait(false);
                    await channel.SendFileAsync(stream, fileName).ConfigureAwait(false);

                    stream.Dispose();
                }
                else
                {
                    var withdraw = await NodeHelper.Send(client, user.Wallet, amount, destination, memo).ConfigureAwait(false);
                    await WalletHelper.UpdateWalletBalance(db, user.Wallet, newBal).ConfigureAwait(false);
                    await Discord.ReplyDMAsync(Context, output).ConfigureAwait(false);
                }
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