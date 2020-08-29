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

    [ConnectionAspect]
    public string RetrieveLastKnownName(string nDiscordID)
    { 
      string cmdText = "SELECT TOP(1) * FROM [Last_Known_Name] WHERE [DiscordID] = @discordID";

      using(SqlCommand cmd = new SqlCommand(cmdText, m_Conn))
      { 
        cmd.Parameters.AddWithValue("@discordID", nDiscordID);
        using(SqlDataReader reader = cmd.ExecuteReader())
        { 
          while(reader.Read())
          { 
            return $"{reader.GetString(reader.GetOrdinal("DiscordName"))}#{reader.GetInt32(reader.GetOrdinal("DiscordIdentifier")).ToString()}";
          }
        }
      }
      return "";
    }

    [ConnectionAspect]
    public bool InsertIntoDatabase(string nDiscordID)
    { 
      string cmdTxt = "INSERT INTO [Last_Known_Name] ([DiscordID], [DiscordName], [DiscordIdentifier], [DateLeft]) VALUES (@discID, @discName, @discIdentifier, NULL)";
      using(SqlCommand cmd = new SqlCommand(cmdTxt, m_Conn))
      { 
        cmd.Parameters.AddWithValue("@discID", nDiscordID);
        cmd.Parameters.AddWithValue("@discName", nDiscordID);
        cmd.Parameters.AddWithValue("@discIdentifier", 1234);

        return cmd.ExecuteNonQuery() > 0;
      }
    }

    [ConnectionAspect]
    public void UpdateDatabaseUser(string nDiscordID)
    { 
      string cmdTxt = "UPDATE [Last_Known_Name] SET [DiscordName] = @discordName, [DiscordIdentifier] = @discIdentifier WHERE [DiscordID] = @discID";

      using(SqlCommand cmd = new SqlCommand(cmdTxt, m_Conn))
      { 
        cmd.Parameters.AddWithValue("@discordName", "test");
        cmd.Parameters.AddWithValue("@discIdentifier", 123);
        cmd.Parameters.AddWithValue("@discID", nDiscordID);

        if(cmd.ExecuteNonQuery() == 0)
          InsertIntoDatabase(nDiscordID);
      }
    }

    [ConnectionAspect]
    public string GetConfigOptions(string nConfigName)
    { 
      string cmdTxt = "SELECT TOP(1) [ConfigValue] FROM [dbo].[Configuration] WHERE [ConfigName] = @confName";
      using(SqlCommand cmd = new SqlCommand(cmdTxt, m_Conn))
      { 
        cmd.Parameters.AddWithValue("@confName", nConfigName);
        using(SqlDataReader reader = cmd.ExecuteReader())
        { 
          while(reader.Read())
          { 
            string test = reader.GetString(0);
            return reader.GetString(0);
          }
        }
      }
      return null;
    }
  }
}