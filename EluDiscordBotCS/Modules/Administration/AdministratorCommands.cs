using Discord.Commands;
using Discord.WebSocket;
using EluDiscordBotCS.EluObjects;
using EluDiscordBotCS.SQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
      histObj = nUser == null ? sql.GetPunishmentHistory(null) : sql.GetPunishmentHistory(nUser.Id.ToString());
      
      await Context.Channel.SendMessageAsync("", false, histObj.EluBuildHistory(nUser == null));
    }

    [Command("history")]
    public async Task GetHistory()
    { 
      await GetHistory(null);
    }
  }
}
