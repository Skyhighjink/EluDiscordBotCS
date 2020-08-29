using EluDiscordBotCS.EluObjects;
using PostSharp.Aspects;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using static EluDiscordBotCS.Enums.PunishmentEnum;

namespace EluDiscordBotCS.SQL.Util
{
  public class ELUSqlUtil
  {
    protected internal static string FormatString(List<PunishmentObject> nObjects)
    {
      string toReturn = "";
      foreach (PunishmentObject obj in nObjects)
        toReturn += $"User: {obj.PunishedDiscordID}\nPunished By: {obj.PunisherDiscordID}\nAction {Enum.GetName(typeof(pAction), obj.Action)}\nDate: {obj.PunishmentDate.ToString()}\n\n";

      return toReturn;
    }
  }
}
