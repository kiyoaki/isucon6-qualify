using System;
using System.Net;

namespace isuda.Models
{
    public class HomeIndexViewModel
    {
        public EntryViewModel[] Entries { get; set; }
        public int Page { get; set; }
        public int LastPage { get; set; }
        public int[] Pages { get; set; }
    }

    public class EntryViewModel
    {
        public string Keyword { get; set; }
        public string UriEncodedKeyword => WebUtility.UrlEncode(Keyword);
        public string Html { get; set; }
        public StarViewModel[] Stars { get; set; }
    }

    public class StarViewModel
    {
        public long id { get; set; }

        public string keyword { get; set; }

        public string user_name { get; set; }

        public DateTime created_at { get; set; }
    }
}
