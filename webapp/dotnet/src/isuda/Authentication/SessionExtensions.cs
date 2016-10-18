using System;
using Microsoft.AspNetCore.Http;

namespace isuda.Authentication
{
    public static class SessionExtensions
    {
        private const string UserIdSessionKey = "isuda:Session:UserId";

        public static void SetUserId(this ISession session, long userId)
        {
            session.Set(UserIdSessionKey, BitConverter.GetBytes(userId));
        }

        public static void RemoveUserId(this ISession session)
        {
            session.Remove(UserIdSessionKey);
        }

        public static bool TryGetUserId(this ISession session, out long userId)
        {
            byte[] b;
            if (session.TryGetValue(UserIdSessionKey, out b))
            {
                userId = BitConverter.ToInt32(b, 0);
                return true;
            }

            userId = 0;
            return false;
        }

        public static bool HasUserId(this ISession session)
        {
            byte[] b;
            if (session.TryGetValue(UserIdSessionKey, out b))
            {
                return true;
            }

            return false;
        }

        public static bool IsAuthenticated(this ISession session)
        {
            return session.HasUserId();
        }
    }
}
