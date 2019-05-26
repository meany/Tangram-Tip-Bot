using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dm.TanTipBot.Common;
using dm.TanTipBot.Data;

namespace dm.TanTipBot
{
    public class Events
    {
        private readonly CommandService commands;
        private readonly DiscordSocketClient client;
        private readonly IServiceProvider services;
        private readonly Config config;
        private readonly AppDbContext db;

        public Events(CommandService commands, DiscordSocketClient client, IServiceProvider services, Config config, AppDbContext db)
        {
            this.commands = commands;
            this.client = client;
            this.services = services;
            this.config = config;
            this.db = db;
        }

        public async Task HandleCommand(SocketMessage messageParam)
        {
            if (!(messageParam is SocketUserMessage message))
            {
                return;
            }

            int argPos = 0;
            var context = new CommandContext(client, message);

            if (message.HasStringPrefix(config.BotPrefix, ref argPos))
            {
                var result = await commands.ExecuteAsync(context, argPos, services).ConfigureAwait(false);
                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                    await context.Channel.SendMessageAsync(result.ErrorReason).ConfigureAwait(false);
                return;
            }
        }

        public async Task HandleConnected()
        {
            foreach (var g in client.Guilds)
            {
                await client.CurrentUser.ModifyAsync(x =>
                {
                    x.Username = config.BotName;
                }).ConfigureAwait(false);
            }

            //var chan = (ITextChannel)client.GetChannel(config.ChannelIds[0]);
            //if (chan != null)
            //{
            //    await chan.SendMessageAsync($"Started at {DateTime.Now.ToDate()}").ConfigureAwait(false);
            //}
        }

        //public async Task HandleReaction(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction)
        //{
        //    return;
        //}
    }
}
