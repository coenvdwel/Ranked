using Nancy.Security;
using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Security.Cryptography;
using System.Text;

namespace Ranked.Utility
{
  public static class Security
  {
    private static MemoryCache _sessions = new MemoryCache("Ranked_Sessions");
    private static MemoryCache _attempts = new MemoryCache("Ranked_Attempts");

    public class User : IUserIdentity
    {
      public string SessionId { get; }
      public string UserName { get; }
      public IEnumerable<string> Claims { get; }

      public User(string sessionId)
      {
        SessionId = sessionId;
        UserName = _sessions.Get(sessionId) as string;
      }
    }

    public static Models.Session Session(string user)
    {
      var id = Guid.NewGuid().ToString().Replace("-", "");

      var session = new Models.Session
      {
        Id = id,
        User = user,
        Expires = DateTimeOffset.UtcNow.AddMonths(3)
      };

      _sessions.Add(session.Id, session.User, session.Expires);
      _attempts.Remove(session.User);
      return session;
    }

    public static bool Logout(string sessionId)
    {
      return _sessions.Remove(sessionId) != null;
    }

    public static bool Limited(string user)
    {
      int attempt;
      _attempts.Set(user, attempt = ((_attempts.Get(user) as int?) ?? 0) + 1, DateTimeOffset.UtcNow.AddMinutes(1));
      return (attempt > 3);
    }

    public static string Hash(string s, int i = 777)
    {
      if (i <= 0) return s;

      var hash = new StringBuilder();
      foreach (var b in new SHA256Managed().ComputeHash(Encoding.UTF8.GetBytes(s), 0, Encoding.UTF8.GetByteCount(s))) hash.Append(b.ToString("x2"));
      return Hash(hash.ToString(), --i);
    }
  }
}