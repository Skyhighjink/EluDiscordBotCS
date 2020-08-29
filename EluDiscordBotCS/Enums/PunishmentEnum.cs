using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
      pAction toReturn = pAction.NA;
      try 
      { 
        toReturn = (pAction)Enum.Parse(typeof(pAction), nAction, true); 
      }
      catch(Exception){ }

      return toReturn;
    }
  }
}
