using Discord.Commands;
using Discord.WebSocket;
using EluDiscordBotCS.SQL;
using PostSharp.Aspects.Advices;
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
    internal protected static ELUSQLInterface sql = new ELUSQLInterface();
    private static SocketRole Muted = null;
    private static bool isRunning = false;
    internal protected static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

    public static bool IsRunning() { return isRunning; }

    /* Start up / Main Mute thread*/
    public static async Task ControlMuted(SocketGuild context, Dictionary<SocketUser, DateTime> nStartupList)
    {
      Dictionary<SocketUser, long> currMuted = new Dictionary<SocketUser, long>();
      Muted = context.Roles.Where(x => x.Name.ToLower() == "mute" || x.Name.ToLower() == "muted").OrderByDescending(y => y.Position).First();

      await GetMuted(nStartupList);

      Thread dictSortThread = new Thread(new ThreadStart(() =>
      {
        while (true)
        {
          Thread.Sleep(500);
          lock (currMuted)
          {
            lock (CurrentMuted)
            {
              currMuted = (from entry in CurrentMuted orderby entry.Value ascending select entry).ToDictionary(x => x.Key, x => x.Value);
            }
          }
        }
      }));
      dictSortThread.IsBackground = true;

      Thread removeMute = new Thread(new ThreadStart(async () =>
      {
        while (true)
        {
          int time = 1000;
          if (CurrentMuted.Keys.Count() > 0)
            time = CurrentMuted.First().Value < time ? (int)CurrentMuted.First().Value : 1000;

          if(time < 0)
            time = 1;

          Thread.Sleep(time);

          await RemoveTime(time);
        }
      }));

      removeMute.IsBackground = true;

      dictSortThread.Start();
      removeMute.Start();
    }

    public static async Task AddMuted(SocketUser nUser, long nDuration)
    {
      await Task.Run(async () =>
      {
        lock (CurrentMuted)
        {
          EluMuteObject.CurrentMuted.Add(nUser, nDuration);
        }
        await nUser.MutualGuilds.Where(x => x.Id == 742727537188405328).First().GetUser(nUser.Id).AddRoleAsync(Muted); // Removes Muted Role
        await LogMuted(nUser, nDuration);
      });
    }

    private static async Task LogMuted(SocketUser nUser, long nDuration)
    {
      await Task.Run(() =>
      {
        sql.AddMutedUser(nUser, nDuration, DateTime.Now);
      });
    }

    private static async Task LogUnmute(SocketUser nUser)
    {
      await Task.Run(() =>
      {
        sql.LogUnmuted(nUser);
      });
    }

    private static async Task RemoveTime(int time)
    {
      if (CurrentMuted.Keys.Count() > 0)
      {
        Dictionary<SocketUser, long> currTime = new Dictionary<SocketUser, long>();

        lock (CurrentMuted)
        {
          foreach (SocketUser user in CurrentMuted.Keys)
          {
            currTime.Add(user, CurrentMuted[user]);
          }
        }

        lock(currTime)
        { 
          lock(CurrentMuted)
          { 
            foreach (SocketUser user in currTime.Keys.ToList())
            {
              currTime[user] -= time;
              if (currTime[user] < 50)
              {
                CurrentMuted.Remove(user);
              }
            }
          }
        }

        await semaphoreSlim.WaitAsync();
        try
        { 
          foreach(SocketUser user in currTime.Keys.Where(x => currTime[x] < 50))
          { 
            await user.MutualGuilds.Where(x => x.Id == 742727537188405328).First().GetUser(user.Id).RemoveRoleAsync(Muted); // Removes Muted Role
            await LogUnmute(user);
          }
        }
        finally
        { 
          semaphoreSlim.Release();
        }


        lock (CurrentMuted)
        {
          foreach (SocketUser user in currTime.Keys)
          {
            if (CurrentMuted.ContainsKey(user))
            {
              CurrentMuted[user] = currTime[user];
            }
          }
        }
      }
    }

    private static async Task GetMuted(Dictionary<SocketUser, DateTime> nList)
    { 
      foreach(SocketUser user in nList.Keys)
      { 
        TimeSpan span = nList[user] - DateTime.Now;
        await Task.Run(() => { 
          CurrentMuted.Add(user, long.Parse(Math.Ceiling(span.TotalMilliseconds).ToString()));
        });
      }
    }
  }
}
