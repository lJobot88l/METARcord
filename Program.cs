using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Newtonsoft.Json;
using Classes;

namespace METARcord
{
    class Program
    {
		public static CommandsNextExtension commands;
		public static DiscordClient discord;
		public static DiscordActivity g1 = new DiscordActivity("");
		public static ulong LastHb = 0; // Last heartbeat message
		public static SetupInfo cInf; // The setup info
        
		public static CommandsNextConfiguration cNcfg; // The commanddsnext config
		public static DiscordConfiguration dCfg; // The discord config
		static void Main(string[] args)
        {
            try
            {
                if (File.Exists("config/mconfig.json"))
                {
                    cInf = Newtonsoft.Json.JsonConvert.DeserializeObject<SetupInfo>(File.ReadAllText("config/mconfig.json"));
					cInf.Version = new SetupInfo().Version;
                }
                else
                {
                    Console.WriteLine("Missing setup info");
                    Environment.Exit(0);
                }
                cNcfg = new CommandsNextConfiguration
                {
                    StringPrefixes = cInf.Prefixes,
                    CaseSensitive = false,
                    EnableDefaultHelp = true
                };
                dCfg = new DiscordConfiguration
                {
                    Token = cInf.Token,
                    TokenType = TokenType.Bot
                };
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                Environment.Exit(0);
            }
            MainAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            try
            {
                discord = new DiscordClient(dCfg);
                commands = discord.UseCommandsNext(cNcfg);

                commands.RegisterCommands<Commands>();

                commands.CommandErrored += CmdErrorHandler;


                await discord.ConnectAsync();
                await SendHeartbeatAsync().ConfigureAwait(false);
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fuck (see Error.log)");
                File.WriteAllText("Error.log", ex.ToString());
                Environment.Exit(0);
            }
        }
        static async  Task CmdErrorHandler(CommandsNextExtension _m, CommandErrorEventArgs e)
        {
            try
			{
				var failedChecks = ((DSharpPlus.CommandsNext.Exceptions.ChecksFailedException)e.Exception).FailedChecks;
                // Code {...}
			}
			catch (Exception ex)
			{
				Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(ex, Formatting.Indented));
				Console.WriteLine(e.Exception.ToString());
			}
        }
        
		public static async Task SendHeartbeatAsync()
		{
			while (true)
			{
				try
				{
					await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
					DiscordEmbedBuilder embed = new DiscordEmbedBuilder { Description = $"Heartbeat received!\n{discord.Ping.ToString()}ms" };
					int ping = discord.Ping;
					embed.WithFooter($"Today at [{System.DateTime.UtcNow.ToShortTimeString()}]");
					if (ping < 200)
					{
						embed.Color = DiscordColor.Green;
					}
					else if (ping < 500)
					{
						embed.Color = DiscordColor.Orange;
					}
					else
					{
						embed.Color = DiscordColor.Red;
					}
					DiscordMessage msghb = null;
					msghb = await discord.SendMessageAsync(await discord.GetChannelAsync(cInf.ErrorHbChannel), embed);


					await discord.UpdateStatusAsync(g1);
					Console.WriteLine($"{System.DateTime.UtcNow.ToShortTimeString()} Ping: {discord.Ping}ms ");
					if (LastHb != 0)
					{
						try
						{
							DiscordChannel hbch = await discord.GetChannelAsync(cInf.ErrorHbChannel);
							DiscordMessage hbmsg = await hbch.GetMessageAsync(LastHb);
							await hbmsg.DeleteAsync();
						}
						catch { }
					}
					LastHb = msghb.Id;
				}
				catch (Exception ex)
				{
					await discord.SendMessageAsync(await discord.GetChannelAsync(cInf.ErrorHbChannel), $"Failed to heartbeat\n\n{ex.ToString()}");
				}
				await Task.Delay(TimeSpan.FromMinutes(10));
			}
		}
    }
	
}

namespace CAttributes
{

	[AttributeUsage( AttributeTargets.All, AllowMultiple = false, Inherited = false)]
	public sealed class RequireUserPermissions2Attribute : CheckBaseAttribute
    {
        /// <summary>
        /// Gets the permissions required by this attribute.
        /// </summary>
        public Permissions Permissions { get; }

        /// <summary>
        /// Gets this check's behaviour in DMs. True means the check will always pass in DMs, whereas false means that it will always fail.
        /// </summary>
        public bool IgnoreDms { get; } = true;

        /// <summary>
        /// Defines that usage of this command is restricted to members with specified permissions.
        /// </summary>
        /// <param name="permissions">Permissions required to execute this command.</param>
        /// <param name="ignoreDms">Sets this check's behaviour in DMs. True means the check will always pass in DMs, whereas false means that it will always fail.</param>
        public RequireUserPermissions2Attribute(Permissions permissions, bool ignoreDms = true)
        {
            this.Permissions = permissions;
            this.IgnoreDms = ignoreDms;
        }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
			if(ctx.Command.Name == "help")
			{
				return Task.FromResult(true);
			}
            if (ctx.Guild == null)
                return Task.FromResult(this.IgnoreDms);

            var usr = ctx.Member;
            if (usr == null)
                return Task.FromResult(false);

            if (usr.Id == ctx.Guild.OwnerId)
                return Task.FromResult(true);

            var pusr = ctx.Channel.PermissionsFor(usr);

            if ((pusr & Permissions.Administrator) != 0)
                return Task.FromResult(true);

            return (pusr & this.Permissions) == this.Permissions ? Task.FromResult(true) : Task.FromResult(false);
        }
    }

	[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public sealed class RequireBotPermissions2Attribute : CheckBaseAttribute
    {
        /// <summary>
        /// Gets the permissions required by this attribute.
        /// </summary>
        public Permissions Permissions { get; }

        /// <summary>
        /// Gets this check's behaviour in DMs. True means the check will always pass in DMs, whereas false means that it will always fail.
        /// </summary>
        public bool IgnoreDms { get; } = true;

        /// <summary>
        /// Defines that usage of this command is only possible when the bot is granted a specific permission.
        /// </summary>
        /// <param name="permissions">Permissions required to execute this command.</param>
        /// <param name="ignoreDms">Sets this check's behaviour in DMs. True means the check will always pass in DMs, whereas false means that it will always fail.</param>
        public RequireBotPermissions2Attribute(Permissions permissions, bool ignoreDms = true)
        {
            this.Permissions = permissions;
            this.IgnoreDms = ignoreDms;
        }

        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
			if(ctx.Command.Name == "help")
			{
				return true;
			}
            if (ctx.Guild == null)
                return this.IgnoreDms;

            var bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id).ConfigureAwait(false);
            if (bot == null)
                return false;

            if (bot.Id == ctx.Guild.OwnerId)
                return true;

            var pbot = ctx.Channel.PermissionsFor(bot);

            if ((pbot & Permissions.Administrator) != 0)
                return true;

            return (pbot & this.Permissions) == this.Permissions;
        }
    }
}

namespace Classes
{
	public class SetupInfo
	{
        // Main Info
		public string Token { get; set; }
		public ulong ErrorHbChannel { get; set; }
		public List<string> Prefixes { get; set; }
        public string Invite = "";
		public string Version = "1.0.0";
	}

	[Flags]
	public enum CommandClasses
	{
		[System.ComponentModel.Description("exampleclass")]
		exampleclass = 1
	}

	public static class EnumExtensions
	{

		// This extension method is broken out so you can use a similar pattern with 
		// other MetaData elements in the future. This is your base method for each.
		public static T GetAttribute<T>(this Enum value) where T : Attribute
		{
			var type = value.GetType();
			var memberInfo = type.GetMember(value.ToString());
			var attributes = memberInfo[0].GetCustomAttributes(typeof(T), false);
			return attributes.Length > 0
			  ? (T)attributes[0]
			  : null;
		}

		// This method creates a specific call to the above method, requesting the
		// Description MetaData attribute.
		public static string ToName(this Enum value)
		{
			var attribute = value.GetAttribute<System.ComponentModel.DescriptionAttribute>();
			return attribute == null ? value.ToString() : attribute.Description;
		}

	}

	[System.Runtime.Serialization.DataContract]
    public class FullXml
    {
        [System.Runtime.Serialization.DataMember(Name = "?xml")]
        public Header xml;
        [System.Runtime.Serialization.DataMember(Name = "response")]
        public METARResponse response;
    }

	[System.Runtime.Serialization.DataContract]
    public class Header
    {
        [System.Runtime.Serialization.DataMember(Name = "@version")]
        public string version;
        [System.Runtime.Serialization.DataMember(Name = "@encoding")]

        public string encoding;
    }

	[System.Runtime.Serialization.DataContract]
    public class METARResponse
    {
        [System.Runtime.Serialization.DataMember(Name = "@xmlns:xsd")]
        public string xmlns_xsd;
        [System.Runtime.Serialization.DataMember(Name = "@xmlns:xsi")]
        public string xmlns_xsi;
        [System.Runtime.Serialization.DataMember(Name = "@version")]
        public string version;
        [System.Runtime.Serialization.DataMember(Name = "@xsi:noNamespaceSchemaLocation")]
        public string xsi_noNamespaceSchemaLocation;
        [System.Runtime.Serialization.DataMember(Name = "request_index")]
        public string request_index;
        [System.Runtime.Serialization.DataMember(Name = "data_source")]
        public DataSource data_source;
        [System.Runtime.Serialization.DataMember(Name = "request")]
        public Request request;
        [System.Runtime.Serialization.DataMember(Name = "errors")]
        public string errors;
        [System.Runtime.Serialization.DataMember(Name = "warnings")]
        public string warnings;
        [System.Runtime.Serialization.DataMember(Name = "time_taken_ms")]
        public string time_taken_ms;
        [System.Runtime.Serialization.DataMember(Name = "data")]
        public Data data;
    	[System.Runtime.Serialization.DataContract]
        public class DataSource
        {
            [System.Runtime.Serialization.DataMember(Name = "@name")]
            public string name;
        }
    	[System.Runtime.Serialization.DataContract]
        public class Request
        {
            [System.Runtime.Serialization.DataMember(Name = "@type")]
            public string type;
        }
    	[System.Runtime.Serialization.DataContract]
        public class Data
        {
            [System.Runtime.Serialization.DataMember(Name = "@num_results")]
            public string num_results;
            [System.Runtime.Serialization.DataMember(Name = "METAR")]
            public Metar METAR;
        }
        public class Metar
        {
            [System.Runtime.Serialization.DataMember(Name = "raw_text")]
            public string raw_text;
        }
    }
}