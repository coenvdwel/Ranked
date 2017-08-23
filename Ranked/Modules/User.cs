using Dapper;
using Nancy;
using Nancy.Security;
using Ranked.Utility;
using System.Data;
using System.Linq;

namespace Ranked.Modules
{
  public class User : NancyModule
  {
    public static void Score(IDbConnection conn, string winner, string loser)
    {
      const int k = 40;

      var winnerRating = conn.Query<int>("SELECT Rating FROM [User] WHERE Id = @Winner", new {Winner = winner}).First();
      var loserRating = conn.Query<int>("SELECT Rating FROM [User] WHERE Id = @Loser", new {Loser = loser}).First();
      var loserIsProvisional = conn.Query<int>("SELECT COUNT(*) FROM Match WHERE (Winner = @Loser OR Loser = @Loser) AND IsWinnerConfirmed = 1 AND IsLoserConfirmed = 1", new {Loser = loser}).First() < 10;

      var odds = (10 ^ (winnerRating/400))/((double) (10 ^ (winnerRating/400)) + (10 ^ (loserRating/400)));

      var newWinnerRating = winnerRating + (int) (k*(1 - odds)*(loserIsProvisional ? 0.5 : 1));
      var newLoserRating = loserRating - (int) (k*(1 - odds));

      Slack.SendMessage($"{winner} (+{newWinnerRating - winnerRating}) [{newWinnerRating}] just won from {loser} (-{loserRating - newLoserRating}) [{newLoserRating}]!", "#ranked", ":ranked:", "Ranked");

      conn.Execute(
        @"UPDATE [User] SET Rating = @WinnerRating WHERE Id = @Winner; UPDATE [User] SET Rating = @LoserRating WHERE Id = @Loser",
        new {Winner = winner, WinnerRating = newWinnerRating, Loser = loser, LoserRating = newLoserRating});
    }

    public User() : base("/users")
    {
      Post["/"] = _ =>
      {
        using (var conn = Database.Connect())
        {
          var user = this.TryBind<Models.User>();
          if (user == null || string.IsNullOrWhiteSpace(user.Password)) return HttpStatusCode.UnprocessableEntity;

          user.Id = user.Id.ToLower();
          user.Password = Security.Hash(user.Password);

          if (!user.Id.EndsWith("@diract-it.nl")) return HttpStatusCode.Forbidden;

          if (!conn.TryExecute("INSERT INTO [User] (Id, Password, Rating) VALUES (@Id, @Password, 1200)", new { user.Id, user.Password })) return HttpStatusCode.UnprocessableEntity;
          return user;
        }
      };

      Get["/"] = _ =>
      {
        this.RequiresAuthentication();

        using (var conn = Database.Connect())
        {
          return conn.Query(@"
            
            SELECT q.*
            FROM 
            (
              SELECT
                u.Id,
                u.Rating,
                CASE WHEN u.Id = @Id THEN 1 ELSE 0 END as Me,
                CASE WHEN sm.Winner = @Id THEN 1 ELSE 0 END as ConfirmWin,
                CASE WHEN sm.Loser = @Id THEN 1 ELSE 0 END as ConfirmLoss,
                CASE WHEN om.Winner = @Id THEN 1 ELSE 0 END as PendingWin,
                CASE WHEN om.Loser = @Id THEN 1 ELSE 0 END as PendingLoss,
                (SELECT COUNT(*) FROM Match m WHERE m.Winner = u.Id AND m.IsWinnerConfirmed = 1 AND m.IsLoserConfirmed = 1) as Wins,
                (SELECT COUNT(*) FROM Match m WHERE m.Loser = u.Id AND m.IsWinnerConfirmed = 1 AND m.IsLoserConfirmed = 1) as Losses,
                CASE WHEN (SELECT COUNT(*) FROM Match m WHERE (m.Winner = u.Id OR m.Loser = u.Id) AND m.IsWinnerConfirmed = 1 AND m.IsLoserConfirmed = 1) < 10 THEN 1 ELSE 0 END as Provisional
              FROM [User] u
              LEFT OUTER JOIN Match sm ON (sm.Winner = @Id AND sm.Loser = u.Id AND sm.IsWinnerConfirmed = 0) OR (sm.Winner = u.Id AND sm.Loser = @Id AND sm.IsLoserConfirmed = 0)
              LEFT OUTER JOIN Match om ON (om.Winner = @Id AND om.Loser = u.Id AND om.IsLoserConfirmed = 0) OR (om.Winner = u.Id AND om.Loser = @Id AND om.IsWinnerConfirmed = 0)
            ) q
            ORDER BY q.Provisional DESC, q.Rating DESC
            
          ", new { Id = Context.CurrentUser.UserName }).ToList();
        }
      };

      Put["/"] = _ =>
      {
        this.RequiresAuthentication();

        var newUser = this.TryBind<Models.User>();
        if (newUser == null || string.IsNullOrWhiteSpace(newUser.Password)) return HttpStatusCode.UnprocessableEntity;

        newUser.Id = Context.CurrentUser.UserName;
        newUser.Password = Security.Hash(newUser.Password);

        using (var conn = Database.Connect())
        {
          if (!conn.TryExecute("UPDATE [User] SET Password = @Password WHERE Id = @Id", new { newUser.Id, newUser.Password })) return HttpStatusCode.UnprocessableEntity;
          return newUser;
        }
      };
    }
  }
}