using Discord.Commands;
using EluDiscordBotCS.SQL;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EluDiscordBotCS.Modules.General
{
  public class GeneralInfoCommand : ModuleBase<SocketCommandContext>
  {
    [Command("website")]
    [Alias("forum", "store", "apply", "vote", "donate", "ip", "server")]
    public async Task GetInfo()
    {
      ELUSQLInterface sql = new ELUSQLInterface();
      await ReplyAsync(sql.GetConfigOptions($"{Context.GetCommand(sql)}URL"));
    }
  }
}