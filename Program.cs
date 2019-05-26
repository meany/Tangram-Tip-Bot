using Discord;
using Discord.Commands;
using Discord.WebSocket;
using dm.TanTipBot.Common;
using dm.TanTipBot.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NLog;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using TanDotNet;

namespace dm.TanTipBot
{
    public class Program
    {
        private CommandService commands;
        private DiscordSocketClient discordClient;
        private IServiceProvider services;
        private IConfigurationRoot configuration;
        private Config config;
        private AppDbContext db;
        private ITangramClient tangramClient;
        private static Logger log = LogManager.GetCurrentClassLogger();

        public static void Main(string[] args)
            => new Program().MainAsync(args).GetAwaiter().GetResult();

        public async Task MainAsync(string[] args)
        {
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("Config.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("Config.Local.json", optional: true, reloadOnChange: true);

                configuration = builder.Build();

                discordClient = new DiscordSocketClient(new DiscordSocketConfig
                {
                    MessageCacheSize = 100
                });
                discordClient.Log += Log;

                string nodeUrl = configuration.GetSection("Node").GetValue<string>("Url");
                services = new ServiceCollection()
                    .Configure<Config>(configuration)
                    .AddDatabase<AppDbContext>(configuration.GetConnectionString("Database"))
                    .AddSingleton<ITangramClient>(new TangramClient(nodeUrl))
                    .BuildServiceProvider();
                config = services.GetService<IOptions<Config>>().Value;
                db = services.GetService<AppDbContext>();
                tangramClient = services.GetService<ITangramClient>();

                if (args.Length > 0)
                {
                    await RunArgs(args).ConfigureAwait(false);
                }
                else
                {
                    await RunBot().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private async Task RunArgs(string[] args)
        {
            try
            {
                await Start().ConfigureAwait(false);
                var handle = new Args(discordClient, tangramClient, db);
                switch (args[0])
                {
                    case "deposit":
                        if (int.TryParse(args[1], out int depositId))
                        {
                            await handle.Deposit(depositId).ConfigureAwait(false);
                        }
                        else
                        {
                            log.Warn($"Could not parse DepositId '{args[1]}'");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private async Task RunBot()
        {
            try
            {
                commands = new CommandService();

                await Install().ConfigureAwait(false);
                await Start().ConfigureAwait(false);
                await discordClient.SetGameAsync($"Tandie Tips | {config.BotPrefix}help").ConfigureAwait(false);

                await Task.Delay(-1).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private Task Log(LogMessage msg)
        {
            log.Info(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task Install()
        {
            try
            {
                var events = new Events(commands, discordClient, services, config, db);
                discordClient.Connected += events.HandleConnected;
                discordClient.MessageReceived += events.HandleCommand;
                //client.ReactionAdded += events.HandleReaction;
                await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services).ConfigureAwait(false);

                await tangramClient.WalletVaultUnseal(config.Node.VaultShard);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private async Task Start()
        {
            try
            {
                await discordClient.LoginAsync(TokenType.Bot, config.BotToken).ConfigureAwait(false);
                await discordClient.StartAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
    }
}
