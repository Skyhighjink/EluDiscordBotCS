using Discord.WebSocket;
using EluDiscordBotCS.EluObjects;
using EluDiscordBotCS.Enums;
using EluDiscordBotCS.SQL.Util;
using PostSharp.Aspects;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Text;
using static EluDiscordBotCS.Enums.PunishmentEnum;

namespace EluDiscordBotCS.SQL
{
  public class ELUSQLInterface
  {
    protected internal SqlConnection m_Conn = new SqlConnection();

    public ELUSQLInterface()
    { 
      this.m_Conn = new SqlConnection(ConfigurationManager.AppSettings["DBConn"]);
    }

    [ConnectionAspect]
    public string GetPunishmentHistory(string nDiscordID)
    { 
      List<PunishmentObject> punishmentHistory = new List<PunishmentObject>();

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

      return ELUSqlUtil.FormatString(punishmentHistory);
    }

    [ConnectionAspect]
    public void RegisterPunishment(string nDiscordID, string nPunisherDiscordID, pAction nAction, string nReason)
    { 
      string sqlCmd = "INSERT INTO [Punishment] ([PunishedDiscordID], [PunisherDiscordID], [Action], [Reason], [Duration], [PunishmentDate]) VALUES (@discID, @punisherDiscID, @action, @reason, 0, GETDATE())";

      using (SqlCommand cmd = new SqlCommand(sqlCmd, m_Conn))
      {
        cmd.Parameters.AddWithValue("@discID", nDiscordID);
        cmd.Parameters.AddWithValue("@punisherDiscID", nPunisherDiscordID);
        cmd.Parameters.AddWithValue("@action", Enum.GetName(typeof(pAction), nAction));
        cmd.Parameters.AddWithValue("@reason", nReason);

        cmd.ExecuteNonQuery();
      }
    }

    
    public string RetrieveLastKnownName(string nDiscordID)
    { 
      return "";
    }
  }
}
