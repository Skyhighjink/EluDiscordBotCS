using Discord.Rest;
using Discord.WebSocket;
using EluDiscordBotCS.EluObjects;
using EluDiscordBotCS.Enums;
using EluDiscordBotCS.SQL.Util;
using PostSharp.Aspects;
using PostSharp.Extensibility;
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
    public List<PunishmentObject> GetPunishmentHistory(string nDiscordID)
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

      return punishmentHistory;
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
            string discName = reader.GetString(reader.GetOrdinal("DiscordName"));
            string discIdentifier = reader.GetInt32(reader.GetOrdinal("DiscordIdentifier")).ToString();
            if(discIdentifier.Length < 4) discIdentifier = discIdentifier.PadLeft(4, '0');

            return $"{discName}#{discIdentifier}";
          }
        }
      }
      return "";
    }

    [ConnectionAspect]
    public bool InsertIntoDatabase(SocketGuildUser nDiscord)
    { 
      string cmdTxt = "INSERT INTO [Last_Known_Name] ([DiscordID], [DiscordName], [DiscordIdentifier], [DateLeft]) VALUES (@discID, @discName, @discIdentifier, NULL)";
      using(SqlCommand cmd = new SqlCommand(cmdTxt, m_Conn))
      { 
        cmd.Parameters.AddWithValue("@discID", nDiscord.Id.ToString());
        cmd.Parameters.AddWithValue("@discName", nDiscord.Username);
        cmd.Parameters.AddWithValue("@discIdentifier", nDiscord.Discriminator);

        return cmd.ExecuteNonQuery() > 0;
      }
    }

    [ConnectionAspect]
    public void UpdateDatabaseUser(SocketGuildUser nDiscordID)
    { 
      string cmdTxt = "UPDATE [Last_Known_Name] SET [DiscordName] = @discordName, [DiscordIdentifier] = @discIdentifier WHERE [DiscordID] = @discID";

      using(SqlCommand cmd = new SqlCommand(cmdTxt, m_Conn))
      { 
        cmd.Parameters.AddWithValue("@discordName", nDiscordID.Username);
        cmd.Parameters.AddWithValue("@discIdentifier", nDiscordID.Discriminator);
        cmd.Parameters.AddWithValue("@discID", nDiscordID.Id);

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

    [ConnectionAspect]
    public void AddMutedUser(SocketUser user, long time, DateTime timeMuted)
    { 
      string cmdTxt = "INSERT INTO [dbo].[MutedLog] ([MutedDiscordID], [DateMuted], [UnmuteTime], [IsUnmuted]) VALUES (@mutedID, @dateMuted, @unmute, 0)";
      DateTime unmuteDateTime = timeMuted.AddMilliseconds(time);

      using(SqlCommand cmd = new SqlCommand(cmdTxt, m_Conn))
      { 
        cmd.Parameters.AddWithValue("@mutedID", user.Id.ToString());
        cmd.Parameters.AddWithValue("@dateMuted", timeMuted);
        cmd.Parameters.AddWithValue("@unmute", unmuteDateTime);

        cmd.ExecuteNonQuery();
      }
    }

    [ConnectionAspect]
    public void LogUnmuted(SocketUser user)
    { 
      string cmdTxt = "UPDATE [MutedLog] SET [IsUnmuted] = 1 WHERE [MutedDiscordId] = @discID";
      using(SqlCommand cmd = new SqlCommand(cmdTxt, m_Conn))
      { 
        cmd.Parameters.AddWithValue("@discID", user.Id.ToString());

        cmd.ExecuteNonQuery();
      }
    }

    [ConnectionAspect]
    public Dictionary<string, DateTime> GetUnmuted()
    { 
      string cmdTxt = "SELECT * FROM [MutedLog] WHERE [IsUnmuted] = 0";

      Dictionary<string, DateTime> toReturn = new Dictionary<string, DateTime>();

      using(SqlCommand cmd = new SqlCommand(cmdTxt, m_Conn))
      { 
        using(SqlDataReader reader = cmd.ExecuteReader())
        { 
          while(reader.Read())
          { 
            toReturn.Add(reader.GetString(reader.GetOrdinal("MutedDiscordID")), reader.GetDateTime(reader.GetOrdinal("UnmuteTime")));
          }
        }
        return toReturn;
      }
    }

    [ConnectionAspect]
    public void CurrentServerRule(RestUserMessage msg, SocketGuildUser user)
    { 
      string cmdTxt = "DELETE FROM [CurrRules]";

      SqlTransaction transaction = m_Conn.BeginTransaction();

      using(SqlCommand cmd = new SqlCommand(cmdTxt, m_Conn, transaction))
      {
        cmd.ExecuteNonQuery();
      }

      cmdTxt = "INSERT INTO [CurrRules] ([CurrentRuleMsgID], [Setter], [SetDate]) VALUES (@currRuleID, @setter, GETDATE())";

      using(SqlCommand cmd = new SqlCommand(cmdTxt, m_Conn, transaction))
      { 
        cmd.Parameters.AddWithValue("@currRuleID", msg.Id.ToString());
        cmd.Parameters.AddWithValue("@setter", user.Id.ToString());

        Console.WriteLine(cmd.ExecuteNonQuery());
      }
      
      transaction.Commit();
    }

    [ConnectionAspect]
    public string GetCurrentServerRuleMsg()
    { 
      string cmdTxt = "SELECT TOP(1) FROM [CurrRules] ORDER BY [SetDate] DESC";

      using(SqlCommand cmd = new SqlCommand(cmdTxt, m_Conn))
      { 
        using(SqlDataReader reader = cmd.ExecuteReader())
        { 
          while(reader.Read())
          { 
            return reader.GetString(reader.GetOrdinal("CurrentRuleMsgID"));
          }
        }
      }
      return null;
    }
  }
}