using System;
using System.Collections.Generic;
using System.Text;

namespace EluDiscordBotCS.Enums
{
  public class PunishmentEnum
  {
    public enum pAction
    {
      KICK,
      MUTE,
      BAN,
      NA
    }

    public static pAction GetAction(string nAction)
    { 
      switch(nAction.ToLower())
      {
        case "kick":
          return pAction.KICK;
        case "ban":
          return pAction.BAN;
        case "mute":
          return pAction.MUTE;
        default:
          return pAction.NA;
      }
    }
  }
}
