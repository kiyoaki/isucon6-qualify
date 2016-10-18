using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using isuda.Data;
using Dapper;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using isuda.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using isuda.Authentication;
using Isu.Shared;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace isuda.Controllers
{
    public class HomeController : Controller
    {
        private readonly Dictionary<string, string> _keywordSha1Hex = new Dictionary<string, string>();
        private MySqlConnection _connection;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            var connectionString = $"Server={Config.DbHost};" +
                                   $"userid={Config.DbUser};" +
                                   $"pwd={Config.DbPassword};" +
                                   $"port={Config.DbPort};" +
                                   "database=isuda";
            _connection = new MySqlConnection(connectionString);
            _connection.Execute("SET SESSION sql_mode = 'TRADITIONAL,NO_AUTO_VALUE_ON_ZERO,ONLY_FULL_GROUP_BY'");
            _connection.Execute("SET NAMES utf8mb4");

            long userId;
            if (HttpContext.Session.TryGetUserId(out userId))
            {
                var userName = _connection.QuerySingleOrDefault<string>("SELECT name FROM user WHERE id = @Id", new { Id = userId });
                if (userName == null)
                {
                    Response.StatusCode = 403;
                    context.Result = new ViewResult
                    {
                        ViewName = "StatusCodeOnly"
                    };
                    return;
                }
                ViewBag.UserName = userName;
            }
        }

        [HttpGet]
        [Route("/")]
        public IActionResult Index(int? page = null)
        {
            const int perPage = 10;
            var pageNumber = (page ?? 1);

            var param = new
            {
                Limit = perPage,
                Offset = perPage * (pageNumber - 1)
            };

            var entries = _connection.Query<Entry>("SELECT * FROM entry ORDER BY updated_at DESC LIMIT @Limit OFFSET @Offset", param).ToArray();
            var count = _connection.QuerySingleOrDefault<int?>("SELECT COUNT(*) AS count FROM entry") ?? 0;
            var lastPage = (int)Math.Ceiling((double)count / perPage);
            var pages = Enumerable.Range(Math.Max(1, pageNumber - 5), Math.Min(lastPage, pageNumber + 5) + 1).ToArray();

            var entryViewModels = entries.Select(x => new EntryViewModel
            {
                Keyword = x.keyword,
                Html = Htmlify(x.description),
                Stars = LoadStars(x.keyword)
            }).ToArray();

            var viewModel = new HomeIndexViewModel
            {
                Entries = entryViewModels,
                Page = pageNumber,
                LastPage = lastPage,
                Pages = pages
            };

            return View(viewModel);
        }

        [HttpGet]
        [Route("/robots.txt")]
        public IActionResult RobotTxt()
        {
            Response.StatusCode = 404;
            return View("StatusCodeOnly");
        }

        [HttpPost]
        [Route("/keyword")]
        public async Task<IActionResult> CreateKeyword(string keyword, string description)
        {
            long userId;
            if (!HttpContext.Session.TryGetUserId(out userId))
            {
                Response.StatusCode = 403;
                return View("StatusCodeOnly");
            }

            if (string.IsNullOrEmpty(keyword))
            {
                Response.StatusCode = 400;
                return View("StatusCodeOnly");
            }

            if (await IsSpamContents(description) || await IsSpamContents(keyword))
            {
                Response.StatusCode = 400;
                return View("StatusCodeOnly");
            }

            var param = new
            {
                AuthorId = userId,
                Keyword = keyword,
                Description = description,
            };

            const string sql = "INSERT INTO entry(author_id, keyword, description, created_at, updated_at)" +
                    "VALUES(@AuthorId,@Keyword,@Description, NOW(), NOW())" +
                    "ON DUPLICATE KEY UPDATE " +
                    "author_id = @AuthorId, keyword = @Keyword, description = @Description, updated_at = NOW()";

            _connection.Execute(sql, param);

            return new RedirectResult(Url.Content("~/"));
        }

        [HttpGet]
        [Route("/register")]
        public IActionResult GetRegister()
        {
            return View("Authenticate", "register");
        }

        [HttpPost]
        [Route("/register")]
        public IActionResult PostRegister(string name, string password)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(password))
            {
                Response.StatusCode = 400;
                return View("StatusCodeOnly");
            }

            var userId = Register(name, password);
            if (userId == null)
            {
                Response.StatusCode = 500;
                return View("StatusCodeOnly");
            }

            HttpContext.Session.SetUserId(userId.Value);
            return new RedirectResult(Url.Content("~/"));
        }

        [HttpGet]
        [Route("/login")]
        public IActionResult GetLogin()
        {
            return View("Authenticate", "login");
        }

        [HttpPost]
        [Route("/login")]
        public IActionResult PostLogin(string name, string password)
        {
            if (string.IsNullOrEmpty(name))
            {
                Response.StatusCode = 403;
                return View("StatusCodeOnly");
            }

            var user = _connection.QuerySingleOrDefault<User>("SELECT * FROM user WHERE name = @Name", new { Name = name });
            if (user == null || ToPasswordHash(user.salt, password) != user.password)
            {
                Response.StatusCode = 403;
                return View("StatusCodeOnly");
            }

            HttpContext.Session.SetUserId(user.id);
            return new RedirectResult(Url.Content("~/"));
        }

        [HttpGet]
        [Route("/logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.RemoveUserId();
            return new RedirectResult(Url.Content("~/"));
        }

        [HttpGet]
        [Route("/keyword/{keyword}")]
        public IActionResult GetKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                Response.StatusCode = 400;
                return View("StatusCodeOnly");
            }

            var entry = _connection.QuerySingleOrDefault<Entry>("SELECT * FROM entry WHERE keyword = @Keyword", new { Keyword = keyword });
            if (entry == null)
            {
                Response.StatusCode = 404;
                return View("StatusCodeOnly");
            }

            var viewModel = new EntryViewModel
            {
                Html = Htmlify(entry.description),
                Keyword = entry.keyword,
                Stars = LoadStars(entry.keyword)
            };

            return View("Keyword", viewModel);
        }

        [HttpPost]
        [Route("/keyword/{keyword}")]
        public IActionResult DeleteKeyword(string keyword)
        {
            long userId;
            if (!HttpContext.Session.TryGetUserId(out userId))
            {
                Response.StatusCode = 403;
                return View("StatusCodeOnly");
            }

            if (string.IsNullOrEmpty(keyword))
            {
                Response.StatusCode = 400;
                return View("StatusCodeOnly");
            }

            var entry = _connection.QuerySingleOrDefault<Entry>("SELECT * FROM entry WHERE keyword = @Keyword", new { Keyword = keyword });
            if (entry == null)
            {
                Response.StatusCode = 404;
                return View("StatusCodeOnly");
            }

            _connection.Execute("DELETE FROM entry WHERE keyword = @Keyword", new { Keyword = keyword });

            return new RedirectResult(Url.Content("~/"));
        }

        private long? Register(string name, string password)
        {
            var salt = RandomString(20);
            var param = new
            {
                Name = name,
                Salt = RandomString(20),
                Password = ToPasswordHash(salt, password)
            };
            const string sql = "INSERT INTO user (name, salt, password, created_at) VALUES (@Name, @Salt, @Password, NOW())";

            _connection.Execute(sql, param);
            var id = _connection.QuerySingleOrDefault<long?>("SELECT LAST_INSERT_ID() as lastInsertId");
            return id;
        }

        private string Htmlify(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return "";
            }

            var keywords = _connection.Query<Entry>("SELECT * FROM entry ORDER BY CHARACTER_LENGTH(keyword) DESC").ToArray();
            var re = new Regex(string.Join("|", keywords.Select(x => Regex.Escape(x.keyword))), RegexOptions.Multiline);
            var result = re.Replace(content, match =>
            {
                var keyword = match.ToString();
                using (var sha1 = SHA1.Create())
                {
                    var hex = sha1.ComputeHash(Encoding.UTF8.GetBytes(keyword)).ToHexString();
                    var sha1Hex = "isuda_" + hex;
                    _keywordSha1Hex[keyword] = sha1Hex;
                    return sha1Hex;
                }
            });

            foreach (var key in _keywordSha1Hex.Keys)
            {
                var url = "/keyword/" + WebUtility.UrlEncode(key);
                var link = string.Format("<a href=\"{0}\">{1}</a>", url, key);
                var linkRegex = new Regex(Regex.Escape(_keywordSha1Hex[key]), RegexOptions.Multiline);
                result = linkRegex.Replace(result, link);
            }
            result = result.Replace("\n", "<br />");
            return result;
        }

        private static StarViewModel[] LoadStars(string keyword)
        {
            var url = Config.IsutarOrigin + "/stars?keyword=" + WebUtility.UrlEncode(keyword);
            using (var httpClient = new HttpClient())
            {
                var res = httpClient.GetStringAsync(url).Result;
                return JsonConvert.DeserializeObject<StarViewModel[]>(res);
            }
        }

        private static async Task<bool> IsSpamContents(string content)
        {
            using (var httpClient = new HttpClient())
            {
                var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "content", WebUtility.UrlEncode(content) }
                });
                var responseMessage = await httpClient.PostAsync(Config.IsupamOrigin, requestContent);
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                var isupamResponse = JsonConvert.DeserializeObject<IsupamResponse>(responseContent);
                return isupamResponse.Data.Valid;
            }
        }

        private static string RandomString(int size)
        {
            var builder = new StringBuilder();
            var random = new Random();
            for (int i = 0; i < size; i++)
            {
                var ch = random.Next(32, 126);
                builder.Append(Convert.ToChar(ch));
            }

            return builder.ToString();
        }

        private static string ToPasswordHash(string salt, string password)
        {
            using (var sha1 = SHA1.Create())
            {
                return sha1.ComputeHash(Encoding.UTF8.GetBytes(salt + password)).ToHexString();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _connection?.Dispose();
        }
    }

    public class IsupamResponse
    {
        [JsonProperty("data")]
        public IsupamResponseDetail Data { get; set; }
    }

    public class IsupamResponseDetail
    {
        [JsonProperty("valid")]
        public bool Valid { get; set; }
    }
}
