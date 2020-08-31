using Discord.Commands;
using Discord.WebSocket;
using EluDiscordBotCS.SQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EluDiscordBotCS.EluObjects
{
  public class EluMuteObject
  {
    internal protected static Dictionary<SocketUser, long> CurrentMuted = new Dictionary<SocketUser, long>();
    private static SocketRole Muted = null;
    private static bool isRunning = false;

    public static bool IsRunning() { return isRunning; } 

    public static async Task ControlMuted(SocketCommandContext context)
    { 
      Dictionary<SocketUser, long> currMuted = new Dictionary<SocketUser, long>();
      Muted = context.Guild.Roles.Where(x => x.Name.ToLower() == "mute" || x.Name.ToLower() == "muted").OrderByDescending(y => y.Position).First();
      while (true)
      { 
        isRunning = true;
        Thread thread = new Thread(new ThreadStart(() => { 
          Thread.Sleep(500);
          lock(currMuted)
          {
            currMuted = (from entry in CurrentMuted orderby entry.Value ascending select entry).ToDictionary(x => x.Key, x => x.Value);
          }
        }));
        thread.IsBackground = true;
        thread.Start();

        while(true)
        { 
          int currAmount = 1000; 
          if (CurrentMuted.Count() > 0)
            currAmount = CurrentMuted.First().Value < 1000 ? (int)CurrentMuted.First().Value : 1000;
          Thread.Sleep(currAmount);
          await RemoveValues(currAmount);
        }
      }
    }

    public async static Task AddMuteObject(SocketCommandContext context, SocketUser nUser, long time)
    { 
      await Task.Run(async () => {
        await context.Guild.GetUser(nUser.Id).AddRoleAsync(Muted);
        lock(CurrentMuted)
        {
          CurrentMuted.Add(nUser, time);
        }
        await LogMuted(nUser, time);
      });
    }

    private async static Task RemoveValues(int amount)
    { 
      await Task.Run(() =>{
        lock (CurrentMuted)
        {
          foreach (SocketUser user in CurrentMuted.Keys)
          {
            CurrentMuted[user] -= amount;
          }
        }
      });
    }

    private async static Task RemoveMuted(SocketCommandContext context)
    {
      List<SocketUser> usersToRemove = new List<SocketUser>();
      await Task.Run(() => {
        lock (CurrentMuted)
        {
          foreach (SocketUser user in CurrentMuted.Keys)
          {
            if (CurrentMuted[user] >= 0)
            {
              usersToRemove.Add(user);
              context.Guild.GetUser(user.Id).RemoveRoleAsync(Muted);
              CurrentMuted.Remove(user);
            }
          }
        }
        await LogUnmite()
      });
    }

    public async static Task LogMuted(SocketUser user, long time)
    { 
      DateTime now = DateTime.Now;
      ELUSQLInterface sql = new ELUSQLInterface();
      await Task.Run(() => { 
        sql.AddMutedUser(user, time, now);
      });
    }

    public async static Task LogUnmute(List<SocketUser> users)
    { 
      
    }
  }
}
