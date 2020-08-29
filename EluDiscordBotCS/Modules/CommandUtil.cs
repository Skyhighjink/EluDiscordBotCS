﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using EluDiscordBotCS.SQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Text;
using EluDiscordBotCS.EluObjects;

namespace EluDiscordBotCS.Modules
{
  public static class CommandUtil
  {
    public enum PermissionType
    { 
      ADMINSITRATOR,
      DEVELOPER,
      STAFF,
      USER,
      UNVERIFIED
    }

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

    public static bool ELUGetPermission(this GuildPermissions nPermission, PermissionType nType)
    { 
      switch(nType)
      { 
        case PermissionType.ADMINSITRATOR:
          if(nPermission.Administrator)
            return true;
          return false;
        case PermissionType.STAFF:
          if(nPermission.Administrator || nPermission.BanMembers || nPermission.KickMembers || nPermission.MuteMembers || nPermission.DeafenMembers)
            return true;
          return false;
        default:
          return false;
      }
    }

    public static Embed EluBuildHistory(this List<PunishmentObject> nObj, bool isSpecificMember)
    { 
      var embedMsg = new EmbedBuilder();
      embedMsg.Color = Color.Blue;
      
      embedMsg.Title = "History";
      embedMsg.Description = isSpecificMember ? $"Last 5 punishments for the user: {nObj[0].PunishedDiscordID}" : $"Last 5 punishments on the server";
      
      for(int x = 0; x < nObj.Count; x++)
      { 
        embedMsg.AddField(name:$"Punishment {x + 1}", value: nObj[0].BuildHistoryCell(), inline: false);
      }

      return embedMsg.Build();
    }

    public static string BuildHistoryCell(this PunishmentObject nObj)
    {
      ELUSQLInterface sql = new ELUSQLInterface();
      string toReturn = $"User: {sql.RetrieveLastKnownName(nObj.PunishedDiscordID)}\nPunished By: {sql.RetrieveLastKnownName(nObj.PunisherDiscordID)}\n";
      toReturn += $"Action: {nObj.Action}\nReason: {nObj.Reason}\nDate Punished: {nObj.PunishmentDate.Value.ToString("dd-MM-yyyy hh:mm:ss")}";

      return toReturn;
    }
  }
}
