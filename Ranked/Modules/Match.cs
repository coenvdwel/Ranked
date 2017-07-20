using Dapper;
using Nancy;
using Nancy.Security;
using Ranked.Utility;
using System;
using System.Linq;

namespace Ranked.Modules
{
  public class Match : NancyModule
  {
    public Match() : base("/match")
    {
      this.RequiresAuthentication();

      Post["/"] = _ =>
      {
        using (var conn = Database.Connect())
        {
          var match = this.TryBind<Models.Match>();
          if (match == null) return HttpStatusCode.UnprocessableEntity;

          if (string.IsNullOrEmpty(match.Winner)) match.Winner = Context.CurrentUser.UserName; else match.Loser = Context.CurrentUser.UserName;
          if (match.Winner == match.Loser) return HttpStatusCode.BadRequest;

          match.IsWinnerConfirmed = match.Winner == Context.CurrentUser.UserName;
          match.IsLoserConfirmed = match.Loser == Context.CurrentUser.UserName;
          match.Date = DateTime.Now;

          if (!conn.TryExecute(@"
            
            DECLARE @Id AS int
            SELECT @Id = Id FROM Match WHERE (IsWinnerConfirmed = 0 OR IsLoserConfirmed = 0) AND ((Winner = @Winner AND Loser = @Loser) OR (Winner = @Loser AND Loser = @Winner))
            IF @Id IS NULL INSERT INTO Match (Winner, Loser, Date, IsWinnerConfirmed, IsLoserConfirmed) VALUES (@Winner, @Loser, @Date, @IsWinnerConfirmed, @IsLoserConfirmed)
            ELSE BEGIN

	            DECLARE @CurrentWinner AS nvarchar(255)
	            SELECT @CurrentWinner = Winner FROM Match WHERE Id = @Id
	            IF @Winner = @CurrentWinner UPDATE Match SET IsWinnerConfirmed = (CASE WHEN IsWinnerConfirmed = 1 OR @IsWinnerConfirmed = 1 THEN 1 ELSE 0 END), IsLoserConfirmed = (CASE WHEN IsLoserConfirmed = 1 OR @IsLoserConfirmed = 1 THEN 1 ELSE 0 END) WHERE Id = @Id
	            ELSE UPDATE Match SET Winner = @Winner, Loser = @Loser, IsWinnerConfirmed = @IsWinnerConfirmed, IsLoserConfirmed = @IsLoserConfirmed WHERE Id = @Id

            END
            
          ", new { match.Winner, match.Loser, match.IsWinnerConfirmed, match.IsLoserConfirmed, match.Date })) return HttpStatusCode.UnprocessableEntity;

          return match;
        }
      };

      Get["/"] = _ =>
      {
        this.RequiresAuthentication();

        using (var conn = Database.Connect())
        {
          return conn.Query(@"
            
            SELECT
              Winner,
              Loser,
              Date
            FROM Match
            WHERE IsWinnerConfirmed = 1 AND IsLoserConfirmed = 1
            ORDER BY Id DESC

          ").ToList();
        }
      };
    }
  }
}