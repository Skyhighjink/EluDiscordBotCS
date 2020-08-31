using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
      }
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
