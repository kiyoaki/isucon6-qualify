using System.Net.Http;
using System.Threading.Tasks;
using Dapper;
using Isu.Shared;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace isuda.Controllers
{
    [Route("initialize")]
    public class InitializeController : Controller
    {
        [HttpGet]
        public async Task<ApiResult> Get()
        {
            var connectionString = $"Server={Config.DbHost};" +
                       $"userid={Config.DbUser};" +
                       $"pwd={Config.DbPassword};" +
                       $"port={Config.DbPort};" +
                       "database=isuda";

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Execute("DELETE FROM entry WHERE id > 7101");
            }

            using (var httpClient = new HttpClient())
            {
                await httpClient.GetAsync(Config.IsutarOrigin + "/initialize");
            }

            return new ApiResult { Result = "ok" };
        }
    }
}
