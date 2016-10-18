using System;

namespace isuda.Data
{
    public class User
    {
        public long id { get; set; }

        public string name { get; set; }

        public string salt { get; set; }

        public string password { get; set; }

        public DateTime created_at { get; set; }
    }
}
