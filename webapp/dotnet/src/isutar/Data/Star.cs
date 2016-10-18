using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace isutar.Data
{
    public class Star
    {
        public long id { get; set; }

        public string keyword { get; set; }

        public string user_name { get; set; }

        public DateTime created_at { get; set; }
    }
}
