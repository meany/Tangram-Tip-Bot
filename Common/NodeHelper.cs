using dm.TanTipBot.Models;
using System.Threading.Tasks;
using TanDotNet;
using TanDotNet.Models;

namespace dm.TanTipBot.Common
{
    public static class NodeHelper
    {
        public static async Task<WalletAccount> CreateWallet(ITangramClient client)
        {
            var item = await client.WalletCreate().ConfigureAwait(false);

            return item;
        }

        public static async Task<int> GetBalance(ITangramClient client, Wallet wallet)
        {
            var item = await client.WalletBalance(new WalletAccount
            {
                Identifier = wallet.Identifier,
                Password = wallet.Password
            }).ConfigureAwait(false);

            return item.Balance;
        }

        public static async Task<int> Receive(ITangramClient client, Wallet wallet, RedemptionMessage message = null)
        {
            var item = await client.WalletReceive(new WalletAccount
            {
                Identifier = wallet.Identifier,
                Password = wallet.Password,
                Address = wallet.Address
            }, message).ConfigureAwait(false);

            return item.Balance;
        }

        public static async Task<int> Send(ITangramClient client, Wallet wallet, int amount, string destination, string memo = null)
        {
            var item = await client.WalletSend(new WalletAccount
            {
                Identifier = wallet.Identifier,
                Password = wallet.Password
            }, amount, destination, false, memo).ConfigureAwait(false);

            return item.Balance;
        }

        public static async Task<RedemptionMessage> Send(ITangramClient client, Wallet wallet, int amount, string destination, string memo = null, bool createRedemptionKey = true)
        {
            var item = await client.WalletSend(new WalletAccount
            {
                Identifier = wallet.Identifier,
                Password = wallet.Password
            }, amount, destination, createRedemptionKey, memo).ConfigureAwait(false);

            return item.Message;
        }
    }
}

