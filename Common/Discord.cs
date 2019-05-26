using Discord;
using Discord.Commands;
using System;
using System.IO;
using System.Threading.Tasks;

namespace dm.TanTipBot
{
    public static class Discord
    {
        public static async Task<IUserMessage> ReplyDMAsync(ICommandContext ctx, EmbedBuilder embed = null, Stream file = null, string fileName = null, string message = "", bool deleteUserMessage = true)
        {
            var msg = await SendDMAsync(ctx.User, embed, file, fileName, message);

            if (deleteUserMessage && ctx.Guild != null)
            {
                await ctx.Message.DeleteAsync().ConfigureAwait(false);
            }

            return msg;
        }

        public static async Task<IUserMessage> SendAsync(ICommandContext ctx, ulong channelId, EmbedBuilder embed = null, Stream file = null, string fileName = null, string message = "")
        {
            var channel = (ITextChannel)await ctx.Client.GetChannelAsync(channelId).ConfigureAwait(false);

            if (file != null && fileName != null && embed != null)
            {
                return await channel.SendFileAsync(file, fileName, embed: embed.Build()).ConfigureAwait(false);
            }
            else if (embed != null)
            {
                return await channel.SendMessageAsync(message, embed: embed.Build()).ConfigureAwait(false);
            }
            else
            {
                return await channel.SendMessageAsync(message).ConfigureAwait(false);
            }
        }

        public static async Task<IUserMessage> SendDMAsync(IUser user, EmbedBuilder embed = null, Stream file = null, string fileName = null, string message = "")
        {
            var channel = await user.GetOrCreateDMChannelAsync().ConfigureAwait(false);

            if (file != null && fileName != null && embed != null)
            {
                return await channel.SendFileAsync(file, fileName, embed: embed.Build()).ConfigureAwait(false);
            }
            else if (embed != null)
            {
                return await channel.SendMessageAsync(message, embed: embed.Build()).ConfigureAwait(false);
            }
            else
            {
                return await channel.SendMessageAsync(message).ConfigureAwait(false);
            }
        }
    }
}
