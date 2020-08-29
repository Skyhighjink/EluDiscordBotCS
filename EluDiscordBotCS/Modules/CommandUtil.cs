using Discord.Commands;
using EluDiscordBotCS.SQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Text;

namespace EluDiscordBotCS.Modules
{
  public static class CommandUtil
  {
    public static string GetCommand(this SocketCommandContext nContext, ELUSQLInterface sql)
    { 
      if(sql == null)
        sql = new ELUSQLInterface();
      string disPre = sql.GetConfigOptions("DiscordPrefix");
      string command = nContext.Message.ToString().Split(' ').Where(x => x.Contains(disPre)).First().Replace(disPre, "");
      return @command;
    }

    public static string GetCommand(this SocketCommandContext nContext)
    { 
      return nContext.GetCommand(null);
    }
  }
}
