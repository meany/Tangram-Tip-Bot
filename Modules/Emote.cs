using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace dm.TanTipBot
{
    public class Emote
    {
        private readonly ICommandContext context;

        public Emote(ICommandContext context)
        {
            this.context = context;
        }

        public async Task<IEmote> Get(string Name)
        {
            var guilds = await context.Client.GetGuildsAsync().ConfigureAwait(false);
            var emote = guilds.FirstOrDefault().Emotes.FirstOrDefault(e => e.Name == Name);
            return emote;
        }
    }
}
