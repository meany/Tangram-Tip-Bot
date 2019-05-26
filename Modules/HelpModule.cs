using Discord;
using Discord.Commands;
using dm.TanTipBot.Common;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;

namespace dm.TanTipBot.Modules
{
    public class HelpModule : ModuleBase
    {
        private readonly Config config;
        private readonly CommandService commands;

        public HelpModule(IOptions<Config> config, CommandService commands)
        {
            this.config = config.Value;
            this.commands = commands;
        }

        [Command("help")]
        [Summary("List of commands and information for the bot")]
        [Remarks("Simply using 'help' will show all commands and command aliases.\n" +
            "Aliases are available to make sending commands faster.\n" +
            "To review command usage and remarks, use the 'help' command along with the full 'name' of the command (not an alias).\n")]
        public async Task Help(string command = "")
        {
            var output = new EmbedBuilder()
                .WithColor(Color.TANGRAM_BLUE);
            if (command == string.Empty)
            {
                foreach (var cmd in commands.Commands)
                {
                    AddHelp(cmd, ref output);
                    output.WithAuthor(author =>
                    {
                        author.WithName($"TanTipBot v{Utils.GetVersion()}");
                    }).WithFooter(footer =>
                    {
                        footer.WithText($"Use '{config.BotPrefix}help <command>' to get help with a specifc command")
                            .WithIconUrl(Asset.INFO);
                    });
                }
            }
            else
            {
                var cmd = commands.Commands.FirstOrDefault(m => m.Name.ToLower() == command.ToLower());
                if (cmd == null)
                {
                    return;
                }

                output.AddField($"Command: **{cmd.Aliases.FirstOrDefault()}**",
                    $"{GetParams(cmd)}\n" +
                    $"**Summary**: {cmd.Summary}\n" +
                    $"**Remarks**: {cmd.Remarks}" +
                    $"{GetAliases(cmd)}");
            }

            await Discord.ReplyDMAsync(Context, output).ConfigureAwait(false);
        }

        private void AddHelp(CommandInfo cmd, ref EmbedBuilder output)
        {
            output.AddField($"— {cmd.Aliases.FirstOrDefault()} {GetParams(cmd, false)}",
                $"{cmd.Summary}" +
                $"{GetAliases(cmd)}");
        }

        private string GetAliases(CommandInfo cmd)
        {
            string s = string.Empty;
            var aliases = cmd.Aliases.Where(x => x != cmd.Name);
            if (aliases.Any())
            {
                string aliasJoin = string.Join("|", aliases.Select(x => $"`{x}`"));
                s = $"\n**Aliases:** {aliasJoin}";
            }
            return s;
        }

        private string GetParams(CommandInfo cmd, bool withIntro = true)
        {
            string s = string.Empty;
            if (cmd.Parameters.Any())
            {
                s += (withIntro) ? "\n**Parameters**: " : string.Empty;
                foreach (var param in cmd.Parameters)
                {
                    if (param.IsOptional)
                    {
                        s += $"[{param.Name} = '{param.DefaultValue}'] ";
                    }
                    else if (param.IsMultiple)
                    {
                        s += $"*{param.Name}... ";
                    }
                    else if (param.IsRemainder)
                    {
                        s += $"...{param.Name} ";
                    }
                    else
                    {
                        s += $"<{param.Name}> ";
                    }
                }
            }
            return s;
        }
    }
}
