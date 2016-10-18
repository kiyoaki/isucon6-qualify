using Isu.Shared;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Dapper;

namespace isutar.Controllers
{
    [Route("initialize")]
    public class InitializeController : Controller
    {
        [HttpGet]
        public ApiResult Get()
        {
            var connectionString = $"Server={Config.DbHost};" +
                       $"userid={Config.DbUser};" +
                       $"pwd={Config.DbPassword};" +
                       $"port={Config.DbPort};" +
                       "database=isutar";

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Execute("TRUNCATE star");
            }

            return new ApiResult { Result = "ok" };
        }
    }
}
