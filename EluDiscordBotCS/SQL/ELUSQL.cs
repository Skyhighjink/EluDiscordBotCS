using Discord.WebSocket;
using EluDiscordBotCS.EluObjects;
using EluDiscordBotCS.Enums;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Text;
using static EluDiscordBotCS.Enums.PunishmentEnum;

namespace EluDiscordBotCS.SQL
{
  public class ELUSQL
  {
    private SqlConnection m_Conn;

    public ELUSQL()
    { 
      this.m_Conn = new SqlConnection(ConfigurationManager.AppSettings["DBConn"]);
    }

    public string GetPunishmentHistory(string nDiscordID)
    { 
      List<PunishmentObject> punishmentHistory = new List<PunishmentObject>();

      OpenConnection();
      string historyCmd = "SELECT TOP(5) * FROM [dbo].[Punishment] ";
      if (!string.IsNullOrEmpty(nDiscordID)) { historyCmd += "WHERE [PunishedDiscordID] = @discordID "; }
      historyCmd += "ORDER BY [PunishmentDate] DESC";

      using(SqlCommand cmd = new SqlCommand(historyCmd, m_Conn))
      { 
        if(!string.IsNullOrEmpty(nDiscordID)) { cmd.Parameters.AddWithValue("@discordID", nDiscordID); }

        using(SqlDataReader reader = cmd.ExecuteReader())
        { 
          while(reader.Read())
          { 
            string discID = reader.GetString(reader.GetOrdinal("PunishedDiscordID"));
            string punisherDiscID = reader.GetString(reader.GetOrdinal("PunisherDiscordID"));
            string action = reader.GetString(reader.GetOrdinal("Action"));
            string reason = reader.GetString(reader.GetOrdinal("Reason"));
            DateTime punishedDate = reader.GetDateTime(reader.GetOrdinal("PunishmentDate"));

            punishmentHistory.Add(new PunishmentObject(discID, punisherDiscID, PunishmentEnum.GetAction(action), reason, punishedDate));
          }
        }
      }
      CloseConnection();

      return FormatString(punishmentHistory);
    }

    private string FormatString(List<PunishmentObject> nObjects)
    { 
      string toReturn = "";
      foreach(PunishmentObject obj in nObjects)
        toReturn += $"User: {obj.PunishedDiscordID}\nPunished By: {obj.PunisherDiscordID}\nAction {Enum.GetName(typeof(pAction), obj.Action)}\nDate: {obj.PunishmentDate.ToString()}\n\n";

      return toReturn;
    }

    private void OpenConnection() { m_Conn.Open(); }

    private void CloseConnection() { m_Conn.Close(); }
  }
}
