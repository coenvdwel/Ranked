using Dapper;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace Ranked.Utility
{
  public static class Database
  {
    public static IDbConnection Connect()
    {
      var cs = ConfigurationManager.ConnectionStrings["Db"].ConnectionString;
      var conn = new SqlConnection(cs);

      conn.Open();
      return conn;
    }

    public static bool TryExecute(this IDbConnection db, string sql, object param = null, int ok = 1)
    {
      try
      {
        return db.Execute(sql, param) == ok;
      }
      catch (Exception ex)
      {
#if DEBUG
        throw ex;
#else
        return false;
#endif
      }
    }

    public static bool TryScalar<T>(this IDbConnection db, string sql, object param, T ok)
    {
      try
      {
        return db.ExecuteScalar<T>(sql, param).Equals(ok);
      }
      catch (Exception ex)
      {
#if DEBUG
        throw ex;
#else
        return false;
#endif
      }
    }
  }
}