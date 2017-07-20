using System;

namespace Ranked.Models
{
  public class Match
  {
    public int Id;
    public string Winner;
    public string Loser;
    public DateTime Date;
    public bool IsWinnerConfirmed;
    public bool IsLoserConfirmed;
  }
}