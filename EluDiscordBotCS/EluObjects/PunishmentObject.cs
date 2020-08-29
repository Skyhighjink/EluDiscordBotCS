using System;
using System.Collections.Generic;
using System.Text;

using static EluDiscordBotCS.Enums.PunishmentEnum;

namespace EluDiscordBotCS.EluObjects
{
  public class PunishmentObject
  {
    public string PunishedDiscordID { get; set; }
    public string PunisherDiscordID { get; set; }
    public pAction Action { get; set; }
    public string Reason { get; set; }
    public DateTime? PunishmentDate { get; set; }

    public PunishmentObject(string nPunishedDiscordID, string nPunisherDiscordID, pAction nAction, string nReason, DateTime? nPunishmentDate)
    { 
      this.PunishedDiscordID = nPunishedDiscordID;
      this.PunisherDiscordID = nPunisherDiscordID;
      this.Action = nAction;
      this.Reason = nReason;
      this.PunishmentDate = nPunishmentDate;
    }
  }
}
