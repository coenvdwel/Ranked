using Dapper;
using Nancy.Security;
using Ranked.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Security.Cryptography;
using System.Text;

namespace Ranked.Utility
{
  public static class Security
  {
    private static MemoryCache _attempts = new MemoryCache("Ranked_Attempts");

    public class User : IUserIdentity
    {
      public Session Session { get; }
      public string UserName { get; }
      public IEnumerable<string> Claims { get; }

      public User(string sessionId)
      {
        using (var conn = Database.Connect())
        {
          Session = conn.Query<Session>("SELECT s.Id, s.UserId, s.Expires FROM [Session] s WHERE GETDATE() < s.Expires", new { Id = sessionId }).FirstOrDefault();
          UserName = Session?.UserId;
        }
      }
    }

    public static Session Session(string user)
    {
      using (var conn = Database.Connect())
      {
        var id = Guid.NewGuid().ToString().Replace("-", "");

        var session = new Session
        {
          Id = id,
          UserId = user,
          Expires = DateTime.UtcNow.AddMonths(3)
        };

        _attempts.Remove(session.UserId);

        conn.Execute("INSERT INTO [Session] (Id, UserId, Expires) VALUES (@Id, @UserId, @Expires)", new { session.Id, session.UserId, session.Expires });
        return session;
      }
    }

    public static bool Logout(string sessionId)
    {
      using (var conn = Database.Connect())
      {
        return conn.Execute("DELETE [Session] WHERE Id = @Id", new { Id = sessionId }) > 0;
      }
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