using Dapper;
using Nancy;
using Nancy.Security;
using Ranked.Utility;
using System.Linq;

namespace Ranked.Modules
{
  public class User : NancyModule
  {
    public User() : base("/users")
    {
      Post["/"] = _ =>
      {
        using (var conn = Database.Connect())
        {
          var user = this.TryBind<Models.User>();
          if (user == null || string.IsNullOrWhiteSpace(user.Password)) return HttpStatusCode.UnprocessableEntity;

          if (!user.Id.ToLower().EndsWith("@diract-it.nl")) return HttpStatusCode.Forbidden;

          user.Password = Security.Hash(user.Password);

          if (!conn.TryExecute("INSERT INTO [User] ([Id], [Password]) VALUES (@Id, @Password)", new { user.Id, user.Password })) return HttpStatusCode.UnprocessableEntity;
          return user;
        }
      };

      Get["/"] = _ =>
      {
        this.RequiresAuthentication();

        using (var conn = Database.Connect())
        {
          return conn.Query(@"
            
            DECLARE @Stats TABLE (
              Id nvarchar(255),
              Wins int,
              Losses int,
              Worth decimal(18,6),
              Score int
            )

            INSERT INTO @Stats (Id, Wins, Losses)
            SELECT
              u.Id,
              (SELECT COUNT(*) FROM Match m WHERE m.Winner = u.Id AND IsWinnerConfirmed = 1 AND IsLoserConfirmed = 1) as Wins,
              (SELECT COUNT(*) FROM Match m WHERE m.Loser = u.Id AND IsWinnerConfirmed = 1 AND IsLoserConfirmed = 1) as Losses
            FROM [User] u

            UPDATE @Stats SET Worth = (Wins/(Losses + 1)) + 1

            UPDATE s
            SET s.Score = ISNULL((SELECT SUM(l.Worth) FROM Match m JOIN @Stats l ON m.Loser = l.Id WHERE m.Winner = s.Id AND m.IsWinnerConfirmed = 1 AND m.IsLoserConfirmed = 1), 0)
            FROM @Stats s

            SELECT
              u.Id,
              CASE WHEN u.Id = @Id THEN 1 ELSE 0 END as Me,
              CASE WHEN sm.Winner = @Id THEN 1 ELSE 0 END as ConfirmWin,
              CASE WHEN sm.Loser = @Id THEN 1 ELSE 0 END as ConfirmLoss,
              CASE WHEN om.Winner = @Id THEN 1 ELSE 0 END as PendingWin,
              CASE WHEN om.Loser = @Id THEN 1 ELSE 0 END as PendingLoss,
              s.Wins,
              s.Losses,
              s.Worth,
              s.Score
            FROM [User] u
            JOIN @Stats s ON s.Id = u.Id
            LEFT OUTER JOIN Match sm ON (sm.Winner = @Id AND sm.Loser = u.Id AND sm.IsWinnerConfirmed = 0) OR (sm.Winner = u.Id AND sm.Loser = @Id AND sm.IsLoserConfirmed = 0)
            LEFT OUTER JOIN Match om ON (om.Winner = @Id AND om.Loser = u.Id AND om.IsLoserConfirmed = 0) OR (om.Winner = u.Id AND om.Loser = @Id AND om.IsWinnerConfirmed = 0)
            ORDER BY s.Score DESC

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
          if (!conn.TryExecute("UPDATE [User] SET [Password] = @Password WHERE [Id] = @Id", new { newUser.Id, newUser.Password })) return HttpStatusCode.UnprocessableEntity;
          return newUser;
        }
      };
    }
  }
}