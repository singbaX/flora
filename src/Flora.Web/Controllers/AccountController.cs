// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using AiCodo;
using AiCodo.Data;
using AiCodo.Web;
using AiCodo.Web.Services;
using Flora.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Flora.Web.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ServiceControllerBase
    {
        IUserService _UserService;
        ITokenService _TokenService;
        IConfiguration _Configuration;
        public AccountController(IUserService userService, ITokenService tokenService,
            IConfiguration configuration)
        {
            _UserService = userService;
            _TokenService = tokenService;
            _Configuration = configuration;
        }

        [HttpGet]
        [Authorize]
        [Route("user")]
        public IActionResult GetUser()
        {
            if (TryGetUserInfo(out var userID, out var userName))
            {
                var result = SqlService.ExecuteQuery<DynamicEntity>("sys_user.GetInfo", "ID", userID).FirstOrDefault();
                if (result != null)
                {
                    return result.CreateOK();
                }
            }
            return Ok(true);
        }

        #region cookies
        [HttpPost]
        [Route("login")]
        public IActionResult Login()
        {
            try
            {
                DynamicEntity data = Request.Body.ReadToEnd();
                var userName = data.GetString("UserName");
                var password = data.GetString("Password");
                var user = _UserService.Login(userName, password);
                if (user == null)
                {
                    return Unauthorized();
                }

                if (_Configuration.GetValue("UseCookies", false))
                {
                    var userID = user.UserID;
                    ClaimsIdentity identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                    identity.AddClaim(new Claim("UserID", userID.ToString()));
                    identity.AddClaim(new Claim(ClaimTypes.Name, userName));

                    var principal = new ClaimsPrincipal(identity);

                    HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                        new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.Now.AddMinutes(30)
                        }).Wait();
                }
                return Ok(user.ToJson());
            }
            catch (Exception ex)
            {
                return Unauthorized();
            }
        }

        [Route("loginout")]
        public IActionResult LoginOut()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).Wait();
            return Ok();
        }
        #endregion

        #region token
        [HttpPost("token")]
        public IActionResult RequestToken()
        {
            DynamicEntity data = Request.Body.ReadToEnd();
            data.ResetKeys(k => k.ToLower());
            var userName = data.GetString("username");
            var password = data.GetString("password");
            var user = _UserService.Login(userName, password);
            if (user == null)
            {
                return Unauthorized();
            }

            var token = _TokenService.CreateToken(user);
            return token.CreateOK();
        }
        #endregion
    }
}
