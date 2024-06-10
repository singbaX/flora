using Microsoft.AspNetCore.Mvc;

namespace AiCodo.Web
{
    public static class HttpHelper
    {
        public static string GetIP(this HttpContext context)
        {
            if (context.Connection == null)
            {
                return "";
            }

            var c = context.Connection;
            return $"{c.RemoteIpAddress}:{c.RemotePort}";
        }

        public static ContentResult CreateOK<T>(this T data)
        {
            return new ContentResult()
            {
                Content = new ServiceResult { Data = data }.ToJson(),
                ContentType = "application/json",
                StatusCode = 200
            };
        }
    }
}
