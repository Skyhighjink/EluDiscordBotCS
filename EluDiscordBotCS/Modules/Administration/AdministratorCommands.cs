using Discord;
using Discord.Commands;
using Discord.WebSocket;
using EluDiscordBotCS.EluObjects;
using EluDiscordBotCS.Enums;
using EluDiscordBotCS.SQL;
using PostSharp.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EluDiscordBotCS.Modules.Administration
{
  public class AdministratorCommands : ModuleBase<SocketCommandContext>
  {
    private ELUSQLInterface sql = new ELUSQLInterface();

    [Command("history")]
    public async Task GetHistory(SocketGuildUser nUser)
    { 
      SocketGuildUser sender = Context.User as SocketGuildUser;
      if(!sender.GuildPermissions.ELUGetPermission(CommandUtil.PermissionType.STAFF)) { return; }

      List<PunishmentObject> histObj = new List<PunishmentObject>();
      histObj = sql.GetPunishmentHistory(nUser == null ? null : nUser.Id.ToString());

      await Context.Channel.SendMessageAsync("", false, histObj.EluBuildHistory(nUser == null));
    }

    [Command("history")]
    public async Task GetHistory([Remainder] string args = null)
    {
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
      catch(Exception)
      { 
        await ReplyAsync("Please enter a valid time");
        return;
      }

      string reason = string.IsNullOrEmpty(string.Join(' ', args.Split(' ').Skip(1))) ? "Probably something bad" : string.Join(' ', args.Split(' ').Skip(1));

      Console.WriteLine(time + " | " + reason);

      //Convert hours to miliseconds
      long muteTime = timeNum;

      ELUSQLInterface sql = new ELUSQLInterface();

      await EluMuteObject.AddMuted(nUser, muteTime);
      sql.RegisterPunishment(nUser.Id.ToString(), Context.User.Id.ToString(), PunishmentEnum.pAction.MUTE, reason);
      Console.WriteLine("Finished");
    }

    [Command("purge")]
    public async Task PurgeChat(int args)
    {
      GuildPermissions perms = Context.Guild.GetUser(Context.User.Id).GuildPermissions;

      if(!perms.Administrator && !perms.BanMembers && !perms.ManageChannels && !perms.ManageMessages && !perms.ManageRoles && Context.Guild.OwnerId != Context.User.Id)
        return;

      await ReplyAsync("Purge in Progress, please wait.");

      var channel = Context.Guild.GetChannel(Context.Channel.Id) as ITextChannel;

      int channelSlowMode = channel.SlowModeInterval;

      await channel.ModifyAsync(x => {
        x.SlowModeInterval = 500;
      });

      var messages = await Context.Channel.GetMessagesAsync(Context.Message, Direction.Before, args).FlattenAsync();

      var filtedMsg = messages.Where(x => (DateTimeOffset.Now - x.Timestamp).TotalDays <= 14);

      if(filtedMsg.Count() < 1)
      { 
        await ReplyAsync("No messages past 14 days can be deleted. Therefore no messages will be deleted.");
        return;
      }

      await channel.DeleteMessagesAsync(filtedMsg);
      await ReplyAsync("Messages deleted");

      await channel.ModifyAsync(x => {
        x.SlowModeInterval = channelSlowMode;
      });
    }

    [Command("rules")]
    public async Task ApplyRules([Remainder] string args)
    { 
      GuildPermissions perms = Context.Guild.GetUser(Context.User.Id).GuildPermissions;

      if(!perms.Administrator)
        return;

      await Context.Channel.DeleteMessageAsync(Context.Message);

      Embed embedMsg = CommandUtil.BuildRules(args);

      var message = await Context.Channel.SendMessageAsync(embed: embedMsg);

      Emoji myEmoji = new Emoji("👍");

      await message.AddReactionAsync(myEmoji);

      sql.CurrentServerRule(message, Context.Guild.GetUser(Context.User.Id));
    }
  }
}
