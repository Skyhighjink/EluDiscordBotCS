using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using EluDiscordBotCS.EluObjects;
using EluDiscordBotCS.SQL;
using Microsoft.Extensions.DependencyInjection;

namespace EluDiscordBotCS
{
  class Program
  {
    protected internal ELUSQLInterface sql;
    private DiscordSocketClient _client;
    private CommandService _commands;
    private IServiceProvider _services;

    static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

    public async Task MainAsync()
    { 
      sql = new ELUSQLInterface();

      _client = new DiscordSocketClient();
      _client.Log += Log;
      _client.UserJoined += RegisterUserJoin;
      _client.ReactionAdded += ReactionAddedEvent;

      _commands = new CommandService();
      _services = new ServiceCollection()
                      .AddSingleton(_client)
                      .AddSingleton(_commands)
                      .BuildServiceProvider();

      string token = sql.GetConfigOptions("DiscordToken");

      await CommandAsync();

      await _client.LoginAsync(TokenType.Bot, token);
      await _client.StartAsync();

      await StartUp();

      await Task.Delay(-1);
    }

    public async Task ReactionAddedEvent(Cacheable<IUserMessage, UInt64> message, ISocketMessageChannel channel, SocketReaction reaction)
    { 
      Console.WriteLine($"{message.Id} : {message.Value} : {reaction.UserId}");

      if(reaction.UserId == _client.CurrentUser.Id)
        return;

      if(message.Id == ulong.Parse(sql.GetCurrentServerRuleMsg()))
      { 
        if(reaction.Emote == new Emoji("👍"))
        { 
          SocketGuild guild = _client.GetGuild(742727537188405328);
          foreach(SocketRole role in guild.GetUser(reaction.UserId).Roles)
          { 
            if(role.Id == 735933111065378827)
              return;
          }

          await guild.GetUser(reaction.UserId).AddRoleAsync(guild.GetRole(735933111065378827));
        }
      }
    }

    public async Task CommandAsync()
    {
      _client.MessageReceived += HandleCommandAsync;
      await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }

    public async Task StartUp()
    { 
      while(_client.Guilds.Count < 1) { }

      if (!EluMuteObject.IsRunning())
      {
        while(!_client.GetGuild(742727537188405328).IsSynced) { }
        SocketGuild guild = _client.GetGuild(742727537188405328);

        Dictionary<string, DateTime> toMute = sql.GetUnmuted();

        Dictionary<SocketUser, DateTime> toRemute = new Dictionary<SocketUser, DateTime>();

        foreach(string userID in toMute.Keys.ToList())
        { 
          toRemute.Add(_client.GetUser(ulong.Parse(userID)), toMute[userID]);
        }

        await EluMuteObject.ControlMuted(guild, toRemute);

        List<SocketGuildUser> users = guild.Users.ToList();

        SocketTextChannel channel = null;

        foreach (SocketTextChannel chan in guild.TextChannels)
        {
          var messages = chan.GetMessagesAsync(50);

          ulong msgId = ulong.Parse(sql.GetCurrentServerRuleMsg());

          await foreach (var message in messages)
          {
            foreach (var msg in message)
            {
              if (msg.Id == msgId)
              {
                channel = chan;
                break;
              }
            }
          }
        }

        if(channel == null)
          return;

        foreach (SocketGuildUser user in users)
        { 
          if(user.IsBot || user.Roles.Where(x => x.Id == 735933111065378827).Count() > 0) 
            continue;

          IMessage msg = channel.GetMessageAsync(ulong.Parse(sql.GetCurrentServerRuleMsg())).Result;

          var lst = msg.GetReactionUsersAsync(new Emoji("👍"), 5000000);

          await foreach(var value in lst)
          { 
            foreach(var currUser in value)
            { 
              if(currUser.Id == user.Id)
              { 
                await guild.GetUser(user.Id).AddRoleAsync(guild.Roles.Where(x => x.Name.ToLower() == "travellers").First());
              }
            }
          }
        }
      }
    }

    public async Task RegisterUserJoin(SocketGuildUser nUser)
    { 
      sql.UpdateDatabaseUser(nUser);

      SocketGuild guild = nUser.Guild;
      SocketGuildChannel welcomeChannel = guild.Channels.Where(x => x.Name.ToLower().Contains("welcome")).First();

      await guild.GetTextChannel(guild.Channels.Where(x => x.Name.ToLower().Contains("welcome")).First().Id).SendMessageAsync($"Welcome <@{nUser.Id} to {guild.Name}!");
    }

    private async Task HandleCommandAsync(SocketMessage msg)
    { 
      SocketUserMessage message = msg as SocketUserMessage;
      SocketCommandContext context = new SocketCommandContext(_client, message);

      if (message.Author.IsBot) return;

      int argPos = 0;
      if (message.HasStringPrefix(sql.GetConfigOptions("DiscordPrefix"), ref argPos))
      {
        var result = await _commands.ExecuteAsync(context, argPos, _services);
        if (!result.IsSuccess)
          Console.WriteLine(result.ErrorReason);
      }
    }

    private Task Log(LogMessage msg)
    {
      Console.WriteLine(msg.ToString());
      return Task.CompletedTask;
    }
  }
}
