﻿using Amazon.S3;
 using Miki.Bot.Models.Repositories;

 namespace Miki
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Miki.Accounts;
    using Miki.API;
    using Miki.Bot.Models;
    using Miki.Bot.Models.Models.User;
    using Miki.BunnyCDN;
    using Miki.Cache;
    using Miki.Cache.StackExchange;
    using Miki.Configuration;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Discord.Gateway;
    using Miki.Discord.Rest;
    using Miki.Framework;
    using Miki.Framework.Commands;
    using Miki.Framework.Commands.Filters;
    using Miki.Framework.Commands.Filters.Filters;
    using Miki.Framework.Commands.Localization;
    using Miki.Framework.Events;
    using Miki.Framework.Events.Triggers;
    using Miki.Localization;
    using Miki.Localization.Exceptions;
    using Miki.Logging;
    using Miki.Models.Objects.Backgrounds;
    using Miki.Serialization.Protobuf;
    using Miki.Services.Achievements;
    using Miki.UrbanDictionary;
    using Retsu.Consumer;
    using SharpRaven;
    using SharpRaven.Data;
    using StackExchange.Redis;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Resources;
    using System.Threading.Tasks;

    public class Program
	{
		private static async Task Main(string[] args)
        {
			// Migrate the database if the program was started with the argument '--migrate' or '-m'.
			if(args.Any(x => x.ToLowerInvariant() == "--migrate" 
                             || x.ToLowerInvariant() == "-m"))
			{
                try
                {
                    using(var context = new MikiDbContextFactory()
                        .CreateDbContext())
                    {
                        await context.Database.MigrateAsync()
                            .ConfigureAwait(false);
                    }
                }
                catch(Exception ex)
                {
                    Log.Error("Failed to migrate the database: " + ex.Message);
                    Log.Debug(ex.ToString());
                    Console.ReadKey();
                    return;
                }
            }

			if (args.Any(x => x.ToLowerInvariant() == "--newconfig" || x.ToLowerInvariant() == "-nc"))
            {
                try
                {
                    var conf = await Config.InsertNewConfigAsync(Environment.GetEnvironmentVariable(Constants.ENV_ConStr));
                    Console.WriteLine("New Config inserted into database with Id: " + conf.Id);
                    Console.ReadKey();
                    return;
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to generate new config: " + ex.Message);
                    Log.Debug(ex.ToString());
                    Console.ReadKey();
                    return;
                }
            }

            CreateLogger();
            
            Log.Message("Loading services");

			// Start the bot.
			var appBuilder = new MikiAppBuilder();
			await LoadServicesAsync(appBuilder)
                .ConfigureAwait(false);
			MikiApp app = appBuilder.Build();

            if (string.IsNullOrWhiteSpace(app.GetService<Config>().Token))
            {
                Log.Error("Token empty, update configuration in database");
                Console.ReadKey();
                return;
            }

			Log.Message("Building command tree");

			var commandBuilder = new CommandTreeBuilder(app);

			var configManager = app.GetService<ConfigurationManager>();
			if(configManager != null)
			{
				commandBuilder.OnContainerLoaded += (c, sc) =>
				{
					var config = sc.GetService<ConfigurationManager>();
					config.RegisterType(c.Instance.GetType(), c.Instance);
				};
			}

			var cmd = commandBuilder.Create(Assembly.GetEntryAssembly());

			Log.Message("Building command pipeline");

			var commands = BuildPipeline(app, cmd);
			await LoadFiltersAsync(app, commands);

			if(configManager != null)
			{
				await LoadConfigAsync(configManager);
			}

			Log.Message("Connecting to Providers");

			LoadDiscord(app, commands);

			Log.Message("Loading Locales");

			LoadLocales(commands);

            for (int i = 0; i < int.Parse(Environment.GetEnvironmentVariable(Constants.ENV_MsgWkr)); i++)
            {
                MessageBucket.AddWorker();
            }

			await app.GetService<IGateway>()
				.StartAsync()
                .ConfigureAwait(false);

			Log.Message("Ready to receive requests!");
			await Task.Delay(-1)
                .ConfigureAwait(false);
		}

        private static void CreateLogger()
        {
            var theme = new LogTheme();
            theme.SetColor(
                LogLevel.Information,
                new LogColor
                {
                    Foreground = ConsoleColor.Cyan,
                    Background = 0
                });
            theme.SetColor(
                LogLevel.Error,
                new LogColor
                {
                    Foreground = ConsoleColor.Red,
                    Background = 0
                });
            theme.SetColor(
                LogLevel.Warning,
                new LogColor
                {
                    Foreground = ConsoleColor.Yellow,
                    Background = 0
                });

            new LogBuilder()
                .AddLogEvent((msg, lvl) =>
                {
                    if (lvl >= (LogLevel)Enum.Parse(typeof(LogLevel), 
                            Environment.GetEnvironmentVariable(Constants.ENV_LogLvl)))
                    {
                        Console.WriteLine(msg);
                    }
                })
                .SetLogHeader((msg) => $"[{msg}]: ")
                .SetTheme(theme)
                .Apply();
        }

		private static CommandPipeline BuildPipeline(MikiApp app, CommandTree cmdTree)
			=> new CommandPipelineBuilder(app)
				.UseStage(new CorePipelineStage())
				.UseFilters(
					new BotFilter(),
					new UserFilter()
				)
				.UsePrefixes(
					new PrefixTrigger(">", true, true),
					new PrefixTrigger("miki.", false),
					new MentionTrigger()
				)
				.UseLocalization()
				.UseArgumentPack()
				.UseCommandHandler(cmdTree)
				.UsePermissions()
                .UseScopes()
				.Build();

		private static void LoadLocales(CommandPipeline app)
		{

			var locale = app.PipelineStages
				.OfType<LocalizationPipelineStage>()
				.FirstOrDefault();
            if(locale == null)
            {
                Log.Warning("No localization loaded, and therefore no locales need to be loaded.");
                return;
            }

            const string nameSpace = "Miki.Languages";
            var typeList = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass
                            && t.Namespace == nameSpace);

            foreach(var t in typeList)
			{
				try
				{
					string languageName = t.Name.ToLowerInvariant();

					ResourceManager resources = new ResourceManager(
						$"Miki.Languages.{languageName}",
						t.Assembly);

					IResourceManager resourceManager = new ResxResourceManager(
						resources);

					locale.LoadLanguage(
						languageName,
						resourceManager,
						resourceManager.GetString("current_language_name"));
				}
				catch(Exception ex)
				{
					Log.Error($"Language {t.Name} did not load correctly");
					Log.Debug(ex.ToString());
				}
			}

			locale.SetDefaultLanguage("eng");
		}

		public static async Task LoadServicesAsync(MikiAppBuilder app)
		{
			var config = await Config.GetOrInsertAsync(
                Environment.GetEnvironmentVariable(Constants.ENV_ConStr) ?? null, 
                Environment.GetEnvironmentVariable(Constants.ENV_ConfId) ?? null);

            if (config == null)
            {
                return;
            }

            app.Services.AddSingleton(config);

            if (string.IsNullOrWhiteSpace(config.Token))
            {
                return;
            }

            var cache = new StackExchangeCacheClient(
                new ProtobufSerializer(),
                await ConnectionMultiplexer.ConnectAsync(config.RedisConnectionString)
            );

			// Setup Redis
			{
				app.AddSingletonService<ICacheClient>(cache);
				app.AddSingletonService<IExtendedCacheClient>(cache);
			}

			// Setup Entity Framework
            {
                app.Services.AddDbContext<MikiDbContext>(x
                    => x.UseNpgsql(
                        Environment.GetEnvironmentVariable(Constants.ENV_ConStr), 
                        b => b.MigrationsAssembly("Miki.Bot.Models")));
                app.Services.AddDbContext<DbContext, MikiDbContext>(x
                    => x.UseNpgsql(
                        Environment.GetEnvironmentVariable(Constants.ENV_ConStr), 
                        b => b.MigrationsAssembly("Miki.Bot.Models")));
            }

            app.Services.AddTransient<AchievementRepository>();

            // Setup Miki API
            {
                if (!string.IsNullOrWhiteSpace(config.MikiApiBaseUrl) 
                    && !string.IsNullOrWhiteSpace(config.MikiApiKey))
                {
                    app.AddSingletonService(new MikiApiClient(config.MikiApiKey));
                }
                else
                {
                    Log.Warning("No Miki API parameters were supplied, ignoring Miki API.");
                }
            }

            // Setup Amazon CDN Client
            {
                if(!string.IsNullOrWhiteSpace(config.CdnAccessKey) 
                   && !string.IsNullOrWhiteSpace(config.CdnSecretKey) 
                   && !string.IsNullOrWhiteSpace(config.CdnRegionEndpoint))
                {
                    app.AddSingletonService(new AmazonS3Client(
                        config.CdnAccessKey, 
                        config.CdnSecretKey, 
                        new AmazonS3Config()
                    {
                        ServiceURL = config.CdnRegionEndpoint
                    }));
                }
            }

			// Setup Discord
			{
				app.AddSingletonService<IApiClient>(
                    s => new DiscordApiClient(config.Token, s.GetService<ICacheClient>()));

				IGateway gateway = null;
                if (bool.Parse(Environment.GetEnvironmentVariable(Constants.ENV_SelfHost).ToLowerInvariant()))
				{
                    gateway = new GatewayCluster(new GatewayProperties
                    {
                        ShardCount = 1,
                        ShardId = 0,
                        Token = config.Token,
                        Compressed = true,
                        AllowNonDispatchEvents = true
                    });
				}
				else
				{
                    gateway = new RetsuConsumer(new ConsumerConfiguration
                    {
                        ConnectionString = new Uri(config.RabbitUrl.ToString()),
                        QueueName = "gateway",
                        ExchangeName = "consumer",
                        ConsumerAutoAck = false,
                        PrefetchCount = 25,
                    });
				}
				app.AddSingletonService(gateway);

				app.AddSingletonService<IDiscordClient, DiscordClient>();
			}

			// Setup web services
			{
				app.AddSingletonService(new UrbanDictionaryAPI());
				app.AddSingletonService(new BunnyCDNClient(config.BunnyCdnKey));
			}

			// Setup miscellanious services
			{
				app.AddSingletonService(new ConfigurationManager());
				app.AddSingletonService(new BackgroundStore());

                if (!string.IsNullOrWhiteSpace(config.SharpRavenKey))
                {
                    app.AddSingletonService(new RavenClient(config.SharpRavenKey));
                }
                else
                {
                    Log.Warning("Sentry.io key not provided, ignoring distributed error logging...");
                }

                app.AddSingletonService<AccountService>();
                app.AddSingletonService<AchievementService>();
            }
		}

		public static async Task LoadConfigAsync(ConfigurationManager m)
        {
            //string configFile = Environment.CurrentDirectory;

			//if(File.Exists(configFile))
			//{
			//	await m.ImportAsync(
			//		new JsonSerializationProvider(),
			//		configFile
			//	);
			//}

			//await m.ExportAsync(
			//	new JsonSerializationProvider(),
			//	configFile
			//);
		}

		public static void LoadDiscord(MikiApp app, CommandPipeline pipeline)
		{
            if(app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            var discord = app.GetService<IDiscordClient>();

			if(pipeline != null)
			{
				discord.MessageCreate += pipeline.CheckAsync;
				pipeline.CommandError += OnErrorAsync;
			}
			discord.GuildJoin += Client_JoinedGuild;
			discord.UserUpdate += Client_UserUpdated;
        }

		public static async Task LoadFiltersAsync(MikiApp app, CommandPipeline pipeline)
		{
			var filters = pipeline.PipelineStages
				.OfType<FilterPipelineStage>()
				.FirstOrDefault();
			if(filters == null)
			{
				Log.Warning("Filters not set up in command pipeline.");
				return;
			}

			var userFilter = filters
				.GetFilterOfType<UserFilter>();
			if(userFilter == null)
			{
				Log.Warning("User filter not set up in command pipeline.");
				return;
			}

            using(var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetService<MikiDbContext>();

                List<IsBanned> bannedUsers = await context.IsBanned
                    .Where(x => x.ExpirationDate > DateTime.UtcNow)
                    .ToListAsync();

                foreach(var u in bannedUsers)
                {
                    userFilter.Users.Add(u.UserId);
                }
            }
        }

        private static async Task Client_UserUpdated(IDiscordUser oldUser, IDiscordUser newUser)
        {
            using(var scope = MikiApp.Instance.Services.CreateScope())
            {
                if(oldUser.AvatarId != newUser.AvatarId)
                {
                    await Utils.SyncAvatarAsync(newUser,
                            scope.ServiceProvider.GetService<IExtendedCacheClient>(),
                            scope.ServiceProvider.GetService<MikiDbContext>())
                        .ConfigureAwait(false);
                }
            }
        }

        private static async Task Client_JoinedGuild(IDiscordGuild arg)
        {
            using(var scope = MikiApp.Instance.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetService<DbContext>();

                IDiscordChannel defaultChannel = await arg.GetDefaultChannelAsync()
                    .ConfigureAwait(false);
                if(defaultChannel != null)
                {
                    var locale = scope.ServiceProvider.GetService<LocalizationPipelineStage>();
                    IResourceManager i = await locale.GetLocaleAsync(
                            scope.ServiceProvider,
                            (long)defaultChannel.Id)
                        .ConfigureAwait(false);
                    (defaultChannel as IDiscordTextChannel).QueueMessage(i.GetString("miki_join_message"));
                }

                List<string> allArgs = new List<string>();
                List<object> allParams = new List<object>();
                List<object> allExpParams = new List<object>();

                try
                {
                    var members = await arg.GetMembersAsync();
                    for(int i = 0; i < members.Count(); i++)
                    {
                        allArgs.Add($"(@p{i * 2}, @p{i * 2 + 1})");

                        allParams.Add(members.ElementAt(i).Id.ToDbLong());
                        allParams.Add(members.ElementAt(i).Username);

                        allExpParams.Add(arg.Id.ToDbLong());
                        allExpParams.Add(members.ElementAt(i).Id.ToDbLong());
                    }

                    await context.Database.ExecuteSqlCommandAsync(
                        $"INSERT INTO dbo.\"Users\" (\"Id\", \"Name\") VALUES {string.Join(",", allArgs)} ON CONFLICT DO NOTHING",
                        allParams);

                    await context.Database.ExecuteSqlCommandAsync(
                        $"INSERT INTO dbo.\"LocalExperience\" (\"ServerId\", \"UserId\") VALUES {string.Join(",", allArgs)} ON CONFLICT DO NOTHING",
                        allExpParams);

                    await context.SaveChangesAsync();
                }
                catch(Exception e)
                {
                    Log.Error(e.ToString());
                }
            }
        }

        private static async Task OnErrorAsync(Exception exception, IContext context)
		{
			if(exception is LocalizedException botEx)
			{
				await Utils.ErrorEmbedResource(context, botEx.LocaleResource)
					.ToEmbed()
                    .QueueAsync(context.GetChannel());
			}
			else
			{
				Log.Error(exception);
				var sentry = context.GetService<RavenClient>();
				if(sentry != null)
				{
					await sentry.CaptureAsync(new SentryEvent(exception));
				}
			}
		}
	}
}