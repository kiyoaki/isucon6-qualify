using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Isu.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MySql.Data.MySqlClient;
using Dapper;
using isutar.Data;

namespace isutar.Controllers
{
    [Route("stars")]
    public class StarsController : Controller
    {
        private MySqlConnection _connection;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            var connectionString = $"Server={Config.DbHost};" +
                                   $"userid={Config.DbUser};" +
                                   $"pwd={Config.DbPassword};" +
                                   $"port={Config.DbPort};" +
                                   "database=isutar";
            _connection = new MySqlConnection(connectionString);
            _connection.Execute("SET SESSION sql_mode = 'TRADITIONAL,NO_AUTO_VALUE_ON_ZERO,ONLY_FULL_GROUP_BY'");
            _connection.Execute("SET NAMES utf8mb4");
        }

        [HttpGet]
        public IEnumerable<Star> Get(string keyword)
        {
            return _connection.Query<Star>("SELECT * FROM star WHERE keyword = @Keyword", new { Keyword = keyword }).ToArray();
        }

        [HttpPost]
        public async Task<ApiResult> Post(PostParameter postParameter)
        {
            using (var httpClient = new HttpClient())
            {
                var result = await httpClient.GetAsync(Config.IsudaOrigin + "/keyword/" + postParameter.Keyword);
                if (result.StatusCode == HttpStatusCode.NotFound)
                {
                    HttpContext.Response.StatusCode = 404;
                    return null;
                }
                var param = new { Keyword = postParameter.Keyword, Name = postParameter.User };
                _connection.Execute("INSERT INTO star (keyword, user_name, created_at) VALUES (@Keyword, @Name, NOW())", param);
            }

            return new ApiResult { Result = "ok" };
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _connection?.Dispose();
        }
    }

    public class PostParameter
    {
        public string Keyword { get; set; }
        public string User { get; set; }
    }
}
