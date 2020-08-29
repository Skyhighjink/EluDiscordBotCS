using System;
using EluDiscordBotCS.SQL;

namespace EluDiscordBotCS
{
  class Program
  {
    static void Main(string[] args)
    {
      ELUSQL sql = new ELUSQL();

      Console.WriteLine(sql.GetPunishmentHistory("287637295962783745"));
    }
  }
}
