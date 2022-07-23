namespace App.Core
{
    public class Headers
    {
        /// <summary>
        /// Headers that Unity can set. YOu can set custom ones.
        /// See full list here https://docs.microsoft.com/en-us/dotnet/api/system.net.httprequestheader?view=net-5.0
        /// </summary>
        public class Keys
        {
            public const string Cookie = "Cookie";
            public const string ContentType = "Content-Type";
            public const string Authorization = "Authorization";
            public const string Connection = "Connection";
            public const string Date = "Date";
            public const string KeepAlive = "KeepAlive";
            public const string Upgrade = "Upgrade";
        }
        
        /// <summary>
        /// See full list here https://www.geeksforgeeks.org/http-headers-content-type/
        /// </summary>
        public class ContentType
        {
            public const string textPlain = "text/plain";
            public const string json = "application/json";
            public const string urlEncoded = "application/x-www-form-urlencoded";
        }

        public class AuthorizationType
        {
            public const string Token = "Bearer";
            public const string UserAndPass = "Basic";
        }
    }
}