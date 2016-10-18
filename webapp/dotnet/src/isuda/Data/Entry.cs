using System;

namespace isuda.Data
{
    public class Entry
    {
        public long id { get; set; }

        public long author_id { get; set; }

        public string keyword { get; set; }

        public string description { get; set; }

        public DateTime updated_at { get; set; }

        public DateTime created_at { get; set; }
    }
}
