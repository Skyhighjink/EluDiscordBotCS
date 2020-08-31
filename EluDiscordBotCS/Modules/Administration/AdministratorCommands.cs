using Discord;
using Discord.Commands;
using Discord.WebSocket;
using EluDiscordBotCS.EluObjects;
using EluDiscordBotCS.Enums;
using EluDiscordBotCS.SQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EluDiscordBotCS.Modules.Administration
{
  public class AdministratorCommands : ModuleBase<SocketCommandContext>
  {
    [Command("history")]
    public async Task GetHistory(SocketGuildUser nUser)
    { 
      ELUSQLInterface sql = new ELUSQLInterface();
      SocketGuildUser sender = Context.User as SocketGuildUser;
      if(!sender.GuildPermissions.ELUGetPermission(CommandUtil.PermissionType.STAFF)) { return; }

      List<PunishmentObject> histObj = new List<PunishmentObject>();
      histObj = sql.GetPunishmentHistory(nUser == null ? null : nUser.Id.ToString());

      await Context.Channel.SendMessageAsync("", false, histObj.EluBuildHistory(nUser == null));
    }

    [Command("history")]
    public async Task GetHistory([Remainder] string args = null)
    {
      ELUSQLInterface sql = new ELUSQLInterface();
      SocketGuildUser sender = Context.User as SocketGuildUser;
      if (!sender.GuildPermissions.ELUGetPermission(CommandUtil.PermissionType.STAFF)) { return; }

      if(args == null || args.Length < 1)
      { 
        await ReplyAsync("Please enter a valid Discord ID or Tag that person using '@'");
      }

      List<PunishmentObject> histObj = new List<PunishmentObject>();
      histObj = sql.GetPunishmentHistory(args == null || args.Length < 1 ? null : args.Split(' ')[0]);

      await Context.Channel.SendMessageAsync("", false, histObj.EluBuildHistory(true));
    }

    [Command("ban")]
    [Alias("kick")]
    public async Task Punishments(SocketGuildUser nUser, [Remainder] string args = null)
    { 
      GuildPermissions perms = Context.Guild.GetUser(Context.User.Id).GuildPermissions;

      string currCmd = Context.GetCommand();

      if (currCmd.ToLower() == "kick")
        if(!perms.KickMembers && (Context.Guild.Owner.Id != Context.User.Id))
          return;

      else if(currCmd.ToLower() == "ban")
        if(!perms.BanMembers && (Context.Guild.Owner.Id != Context.User.Id))
          return;

      SocketRole senderRole = Context.Guild.GetUser(Context.User.Id).Roles.OrderByDescending(x => x.Position).First();
      SocketRole targetRole = nUser.Roles.OrderByDescending(x => x.Position).First();
      
      if(senderRole.Position < targetRole.Position)
        await ReplyAsync($"You cannot '{currCmd}' someone who is higher rank than you!");

      List<string> sortedContext = CommandUtil.GetKickBanReason(args);

      string msg = $"You have been {currCmd.ToLower()} from {Context.Guild.Name} by <@{Context.User.Id}> ({Context.Guild.GetUser(Context.User.Id).Roles.OrderByDescending(x => x.Position).First()})";
      
      if(Context.GetCommand().ToLower() == "ban")
        await nUser.BanAsync(reason: sortedContext[1]);
      else
        await nUser.KickAsync(sortedContext[1]);

      if(!(sortedContext[0] == "silent"))
        await ReplyAsync($"User: <@{nUser.Id}> has been {currCmd.ToLower()} for: '{sortedContext[1]}'");
      else
        await ReplyAsync($"User: <@{nUser.Id}> has been {currCmd.ToLower()} for probably something bad!");

      ELUSQLInterface sql = new ELUSQLInterface();
      sql.RegisterPunishment(nUser.Id.ToString(), Context.User.Id.ToString(), PunishmentEnum.GetAction(currCmd), sortedContext[1]);
    }


    [Command("mute")]
    public async Task Mute(SocketGuildUser nUser, [Remainder] string args = null)
    { 
      if(!EluMuteObject.IsRunning()) 
      { 
        Thread thread = new Thread(new ThreadStart(async () => await EluMuteObject.ControlMuted(Context)));
        thread.IsBackground = true;
        thread.Start();
      }

      GuildPermissions perms = Context.Guild.GetUser(Context.User.Id).GuildPermissions;

      if(!perms.MuteMembers)
        return;

      if(string.IsNullOrEmpty(args))
        await ReplyAsync("Please enter a valid time");
      
      string time = args.Split(' ')[0];
      int timeNum = 0;
      try
      { 
        timeNum = int.Parse(time);

        //if(timeNum > 168)
        //{ 
        //  await ReplyAsync("You cannot mute for more than a week!");
        // return;
        //}
      }
      catch(Exception ex)
      { 
        await ReplyAsync("Please enter a valid time");
        return;
      }

      string reason = string.IsNullOrEmpty(string.Join(' ', args.Split(' ').Skip(1))) ? "Probably something bad" : string.Join(' ', args.Split(' ').Skip(1));

      Console.WriteLine(time + " | " + reason);

      //Convert hours to miliseconds
      long muteTime = timeNum;

      ELUSQLInterface sql = new ELUSQLInterface();

      await EluMuteObject.LogMuted(nUser, muteTime);
      Console.WriteLine("Finished");
      //await EluMuteObject.AddMuteObject(Context, nUser, muteTime);
    }
  }
}
