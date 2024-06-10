// Licensed to the AiCodo.com under one or more agreements.
// The AiCodo.com licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 本程序文件开源，遵循MIT开源协议，如有疑问请联系作者（singba@163.com）
// 您可以私用、商用部分或全部代码，修改源码时，请保持原代码的完整性，以免因为版本升级导致问题。
using AiCodo.Data;
using Flora.Services;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AiCodo.Web.Services
{
    public class TokenService : ITokenService
    {
        IConfiguration _Configuration;

        string _SecurityKey = "AiCodo.Web.Services.TokenSecurityKey";
        string _Issuer = "AiCodo.Web";
        string _Audience = "AiCodo.Web";
        int _ExpiresMinutes = 30;

        public string Issuer { get { return _Issuer; } }

        public string SecurityKey { get { return _SecurityKey; } }

        public string Audience {  get { return _Audience; } }

        public int ExpiresMinutes { get { return _ExpiresMinutes; } }

        //这两个可以移到缓存或者数据库
        private Dictionary<string, Token> _Tokens = new Dictionary<string, Token>();

        private Dictionary<string, IUser> _Users = new Dictionary<string, IUser>();

        public TokenService(IConfiguration configuration)
        {
            _Configuration = configuration;
            var jwt = _Configuration.GetSection("Jwt");
            if (jwt != null)
            {
                _SecurityKey = jwt.GetValue("SecurityKey", "AiCodo.Web.Services.TokenSecurityKey");
                _Issuer = jwt.GetValue("Issuer", "AiCodo.Web.Services");
                _Audience = jwt.GetValue("Audience", "AiCodo.Web.Services");
                _ExpiresMinutes = jwt.GetValue("ExpiresMinutes", 30);
            }
        }

        public Token CreateToken(IUser user)
        {
            Claim[] claims = {
                new Claim(ClaimTypes.NameIdentifier, user.UserID),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            var token = CreateToken(claims);
            _Tokens.Add(token.AccessToken, token);
            _Users.Add(token.AccessToken, user);
            return token;
        }

        public IUser GetUser(string accessToken)
        {
            if (_Users.TryGetValue(accessToken, out var u))
            {
                return u;
            }
            return null;
        }

        private Token CreateToken(Claim[] claims)
        {
            var now = DateTime.Now;
            var expires = now.Add(TimeSpan.FromMinutes(_ExpiresMinutes));
            var token = new JwtSecurityToken(
                issuer: _Issuer,
                audience: _Audience,
                claims: claims,
                notBefore: now.AddSeconds(-1),
                expires: expires,
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_SecurityKey)), SecurityAlgorithms.HmacSha256));
            return new Token
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                TokenType = "Bearer",
                ExpiresAt = expires
            };
        }
    }
}
