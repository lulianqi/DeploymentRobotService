using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace DeploymentRobotService
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        private ILogger _logger;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            SetAppsetting(Configuration);
        } 

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services )
        {
            //blazor
            services.AddRazorPages();
            services.AddServerSideBlazor();
            //默认浏览器上报数据超过32kb后会导致ws断开
            services.AddAntDesign();
            services.AddSignalR(e => { e.MaximumReceiveMessageSize = 512 * 1024; });
            //--------------------------------------------------------


            services.AddControllers();
            //add for session
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.Cookie.Name = "DeploymentRobot.Session";
                options.IdleTimeout = TimeSpan.FromSeconds(6000);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

          
            //配置JWT
            services.AddAuthentication(options => {
                //认证middleware配置
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o => {
                //主要是jwt  token参数设置
                o.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    //Token颁发机构
                    ValidIssuer = Appsetting.JwtConfig.Issuer,
                    //颁发给谁
                    ValidAudience = Appsetting.JwtConfig.Audience,
                    //这里的key要进行加密，需要引用Microsoft.IdentityModel.Tokens
                    IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Appsetting.JwtConfig.SecretKey)),
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = true,
                    ////是否验证Token有效期，使用当前时间与Token的Claims中的NotBefore和Expires对比
                    ValidateLifetime=true,
                    ////允许的服务器时间偏移量
                    //ClockSkew=TimeSpan.Zero
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env , ILogger<Startup> logger)
        {
            //blazor
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }
            app.UseStaticFiles();
            app.UseRouting();
            //因为需要混合Controllers，下面还有些配置，要设置完成后再UseEndpoints
            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapBlazorHub();
            //    endpoints.MapFallbackToPage("/_Host");
            //    endpoints.MapControllers();
            //});
            //-------------------------------------------------


            MyHelper.MyLogger.Logger = _logger = logger;
            MyHelper.MyLogger.Logger.LogInformation("Configure");
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //private/saas
                _ = DeploymentService.ApplicationRobot.InitMyDeployment("conf.dev.json");
                //_ = DeploymentService.MyDeployment.InitMyDeployment("conf.private.json");
               _= MyDeploymentMonitor.ExecuteHelper.ExecuteTimePredict.LoadData();
            }
            else
            {
                _ = DeploymentService.ApplicationRobot.InitMyDeployment("conf.json");
                _ = MyDeploymentMonitor.ExecuteHelper.ExecuteTimePredict.LoadData();
            }

            //app.UseHttpsRedirection();

            //add for session
            app.UseSession();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            //blazor
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
                endpoints.MapControllers();
            });

            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapControllers();
            //});
        }


        private void SetAppsetting(IConfiguration configuration)
        {
            Appsetting.WxConfig.MessageToken = configuration.GetSection("WxConfig")["MessageToken"];
            Appsetting.WxConfig.MessageEncodingAESKey = configuration.GetSection("WxConfig")["MessageEncodingAESKey"];
            Appsetting.WxConfig.CorpID = configuration.GetSection("WxConfig")["CorpID"];
            Appsetting.WxConfig.Corpsecret = configuration.GetSection("WxConfig")["Corpsecret"];
            Appsetting.WxConfig.Agentid = int.Parse(configuration.GetSection("WxConfig")["Agentid"]);
            Appsetting.WxConfig.OAuthDomain = configuration.GetSection("WxConfig")["OAuthDomain"];

            Appsetting.FsConfig.ApiBaseUrl = configuration.GetSection("FsConfig")["ApiBaseUrl"];
            Appsetting.FsConfig.AppID = configuration.GetSection("FsConfig")["AppID"];
            Appsetting.FsConfig.AppSecret = configuration.GetSection("FsConfig")["AppSecret"];
            Appsetting.FsConfig.OAuthDomain = configuration.GetSection("FsConfig")["OAuthDomain"];


            Appsetting.RobotConfig.HelpDoc = configuration.GetSection("RobotConfig")["HelpDoc"];
            Appsetting.RobotConfig.BuildLink = configuration.GetSection("RobotConfig")["BuildLink"];
            Appsetting.RobotConfig.CancleLink = configuration.GetSection("RobotConfig")["CancleLink"];
            Appsetting.RobotConfig.ExecuteTokenLink = configuration.GetSection("RobotConfig")["ExecuteTokenLink"];


            Appsetting.AliOssConfig.Endpoint= configuration.GetSection("AliOssConfig")["Endpoint"];
            Appsetting.AliOssConfig.AccessKeyId = configuration.GetSection("AliOssConfig")["AccessKeyId"];
            Appsetting.AliOssConfig.AccessKeySecret = configuration.GetSection("AliOssConfig")["AccessKeySecret"];
            Appsetting.AliOssConfig.BucketName = configuration.GetSection("AliOssConfig")["BucketName"];
            Appsetting.AliOssConfig.BaseFileUrl = configuration.GetSection("AliOssConfig")["BaseFileUrl"];

            Appsetting.JwtConfig.Issuer = configuration.GetSection("JwtConfig")["Issuer"];
            Appsetting.JwtConfig.Audience = configuration.GetSection("JwtConfig")["Audience"];
            Appsetting.JwtConfig.SecretKey = configuration.GetSection("JwtConfig")["SecretKey"];
            Appsetting.JwtConfig.OauthUsers = configuration.GetSection("JwtConfig")?.GetSection("OauthUsers")?.GetChildren().Select((section) => section.Value)?.ToList<string>();
        }

    }
}
