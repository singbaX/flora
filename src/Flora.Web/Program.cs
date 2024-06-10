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

            //����jwt https://www.cnblogs.com/clis/p/16151872.html
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenService.SecurityKey));
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true, //�Ƿ���֤Issuer
                    ValidIssuer = tokenService.Issuer, //������Issuer
                    ValidateAudience = true, //�Ƿ���֤Audience
                    ValidAudience = tokenService.Audience, //������Audience
                    ValidateIssuerSigningKey = true, //�Ƿ���֤SecurityKey
                    IssuerSigningKey = securityKey, //SecurityKey
                    ValidateLifetime = true, //�Ƿ���֤ʧЧʱ��
                    ClockSkew = TimeSpan.FromSeconds(30), //����ʱ���ݴ�ֵ�������������ʱ�䲻ͬ�����⣨�룩
                    RequireExpirationTime = true,
                };
                if (usingQueryAccessToken)
                {
                    options.Events = new JwtBearerEvents
                    {
                        //���url�ķ�ʽ https://www.eidias.com/blog/2022/8/11/how-to-pass-jwt-token-as-url-query-parameter-in-aspnet-core-6
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
            /* �����������
            //���¼��ر�ṹ
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
