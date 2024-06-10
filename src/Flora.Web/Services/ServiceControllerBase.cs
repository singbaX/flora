using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Flora.Web
{
    public class ServiceControllerBase : ControllerBase
    {
        protected bool TryGetUserInfo(out int userID, out string userName)
        {
            var claims = HttpContext.User.Claims;
            var id = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
            userID = id == null ? 0 : id.Value.ToInt32();
            userName = name == null ? string.Empty : name.Value;
            return true;
        }
    }
}
