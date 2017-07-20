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

          // setup object
          match.Winner = string.IsNullOrEmpty(match.Winner) ? Context.CurrentUser.UserName : match.Winner;
          match.Loser = string.IsNullOrEmpty(match.Loser) ? Context.CurrentUser.UserName : match.Loser;
          match.IsWinnerConfirmed = match.Winner == Context.CurrentUser.UserName;
          match.IsLoserConfirmed = match.Loser == Context.CurrentUser.UserName;
          match.Date = DateTime.Now;

          // sanity-check the code monkeys
          if (match.Winner == match.Loser) return HttpStatusCode.BadRequest;

          // check if there is an existing proposal for this match
          var existingMatch = conn.Query<Models.Match>(@"
          
            SELECT Id, Winner, Loser, Date, IsWinnerConfirmed, IsLoserConfirmed
            FROM Match WHERE (IsWinnerConfirmed = 0 OR IsLoserConfirmed = 0) AND ((Winner = @Winner AND Loser = @Loser) OR (Winner = @Loser AND Loser = @Winner))
          
          ", new { match.Winner, match.Loser }).FirstOrDefault();

          // first proposal of this match, submit for review
          if (existingMatch == null)
          {
            return conn.Query<Models.Match>(
              "INSERT INTO Match (Winner, Loser, Date, IsWinnerConfirmed, IsLoserConfirmed) OUTPUT inserted.* VALUES (@Winner, @Loser, @Date, @IsWinnerConfirmed, @IsLoserConfirmed)",
              new { match.Winner, match.Loser, match.Date, match.IsWinnerConfirmed, match.IsLoserConfirmed }).First();
          }

          // declined proposal, update for other review (ping-pong)
          if (existingMatch.Winner != match.Winner)
          {
            return conn.Query<Models.Match>(
              "UPDATE Match SET Winner = @Winner, Loser = @Loser, IsWinnerConfirmed = @IsWinnerConfirmed, IsLoserConfirmed = @IsLoserConfirmed OUTPUT inserted.* WHERE Id = @Id",
              new { existingMatch.Id, match.Winner, match.Loser, match.IsWinnerConfirmed, match.IsLoserConfirmed }).First();
          }

          // same user updating same result - the user must want to delete it
          if ((!existingMatch.IsWinnerConfirmed && !match.IsWinnerConfirmed) || (!existingMatch.IsLoserConfirmed && !match.IsLoserConfirmed))
          {
            conn.Execute("DELETE Match WHERE Id = @Id AND (IsWinnerConfirmed = 0 OR IsLoserConfirmed = 0)", new { Id = existingMatch.Id });
            return existingMatch;
          }

          // update ELO
          User.Score(conn, match.Winner, match.Loser);

          // approved proposal
          return conn.Query<Models.Match>(
            "UPDATE Match SET IsWinnerConfirmed = @IsWinnerConfirmed, IsLoserConfirmed = @IsLoserConfirmed OUTPUT inserted.* WHERE Id = @Id",
            new { existingMatch.Id, IsWinnerConfirmed = match.IsWinnerConfirmed || existingMatch.IsWinnerConfirmed, IsLoserConfirmed = match.IsLoserConfirmed || existingMatch.IsLoserConfirmed }).First();
        }
      };

      Get["/"] = _ =>
      {
        this.RequiresAuthentication();

        using (var conn = Database.Connect())
        {
          return conn.Query("SELECT Winner, Loser, Date FROM Match WHERE IsWinnerConfirmed = 1 AND IsLoserConfirmed = 1 ORDER BY Id DESC").ToList();
        }
      };
    }
  }
}