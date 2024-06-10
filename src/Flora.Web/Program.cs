using AiCodo.Data;
using AiCodo.Flow;
using AiCodo.Web.Services;
using Flora.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Text;

namespace Flora.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var configuration = builder.Configuration;

            builder.Services.AddSingleton<IUserService, UserService>();
            var tokenService = new TokenService(configuration);
            builder.Services.AddSingleton<ITokenService>(tokenService);

            var usingWebSocket = configuration.GetValue("UsingWebSocket", false);
            var usingQueryAccessToken = configuration.GetValue("UsingQueryAccessToken", false);

            //基本jwt https://www.cnblogs.com/clis/p/16151872.html
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenService.SecurityKey));
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true, //是否验证Issuer
                    ValidIssuer = tokenService.Issuer, //发行人Issuer
                    ValidateAudience = true, //是否验证Audience
                    ValidAudience = tokenService.Audience, //订阅人Audience
                    ValidateIssuerSigningKey = true, //是否验证SecurityKey
                    IssuerSigningKey = securityKey, //SecurityKey
                    ValidateLifetime = true, //是否验证失效时间
                    ClockSkew = TimeSpan.FromSeconds(30), //过期时间容错值，解决服务器端时间不同步问题（秒）
                    RequireExpirationTime = true,
                };
                if (usingQueryAccessToken)
                {
                    options.Events = new JwtBearerEvents
                    {
                        //添加url的方式 https://www.eidias.com/blog/2022/8/11/how-to-pass-jwt-token-as-url-query-parameter-in-aspnet-core-6
                        OnMessageReceived = static context =>
                        {
                            if (context.Request.Query.TryGetValue("access_token", out var token))
                                context.Token = token;
                            return Task.CompletedTask;
                        }
                    };
                }
            });

            builder.Services.AddCors();
            // Add services to the container.
            builder.Services.AddControllers();

            //AiCodo:Set db provider
            DbProviderFactories.SetFactory("mysql", MySqlProvider.Instance);

            var app = builder.Build();

            ResetBaseFolder(app);
            // Configure the HTTP request pipeline.
            app.UseAuthentication();
            app.UseAuthorization();

            if (configuration.GetValue("UseCookies", false))
            {
                app.UseCookiePolicy();
            }

            app.UseCors(b =>
            {
                b.AllowAnyHeader();
                b.AllowAnyMethod();
                b.AllowAnyOrigin();
            });
            app.MapControllers();

            if (usingWebSocket)
            {
                app.UseWebSockets();
            }

            FuncService.Current.RegisterType(typeof(PageConfigService));
            ExpressionHelper.AddReferenceType(typeof(CommonFunctions));
            /* 后面会加命令处理
            //重新加载表结构
            SqlData.Current.ReloadTables();
            */

            app.Run();
        }

        private static void ResetBaseFolder(WebApplication app)
        {
            var env = app.Environment;
            ApplicationConfig.BaseDirectory = Process.GetCurrentProcess().MainModule.FileName.GetParentPath();
            ApplicationConfig.LocalDataFolder = System.IO.Path.Combine(env.ContentRootPath, "App_Data");
            ApplicationConfig.LocalConfigFolder = "Configs".FixedAppDataPath();
        }
    }
}
