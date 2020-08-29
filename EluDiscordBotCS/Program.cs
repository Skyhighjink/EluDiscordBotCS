using System;
using EluDiscordBotCS.SQL;
using static EluDiscordBotCS.Enums.PunishmentEnum;

namespace EluDiscordBotCS
{
  class Program
  {
    static void Main(string[] args)
    {
      ELUSQLInterface sql = new ELUSQLInterface();

      Console.WriteLine(sql.GetPunishmentHistory("287637295962783745"));

      sql.RegisterPunishment("test2", "test3", (pAction)Enum.Parse(typeof(pAction), "KICK"), "Test1");
    }
  }
}
