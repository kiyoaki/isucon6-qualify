using System;

namespace Isu.Shared
{
    public static class Config
    {
        public static string DbHost => Environment.GetEnvironmentVariable("ISUDA_DB_HOST") ?? "localhost";
        public static string DbPort => Environment.GetEnvironmentVariable("ISUDA_DB_PORT") ?? "3306";
        public static string DbUser => Environment.GetEnvironmentVariable("ISUDA_DB_USER") ?? "root";
        public static string DbPassword => Environment.GetEnvironmentVariable("ISUDA_DB_PASSWORD") ?? "";
        public static string IsudaOrigin => Environment.GetEnvironmentVariable("ISUDA_ORIGIN") ?? "http://localhost:5000";
        public static string IsutarOrigin => Environment.GetEnvironmentVariable("ISUTAR_ORIGIN") ?? "http://localhost:5001";
        public static string IsupamOrigin => Environment.GetEnvironmentVariable("ISUPAM_ORIGIN") ?? "http://localhost:5050";
    }
}
