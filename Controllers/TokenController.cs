using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DeploymentRobotService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MyCommonHelper.EncryptionHelper;

namespace DeploymentRobotService.Controllers
{
    [Route("oauth/token")]
    [ApiController]
    public class TokenController: ControllerBase
    {
        private readonly ILogger<TokenController> _logger;

        public TokenController(ILogger<TokenController> logger)
        {
            _logger = logger;
        }

        [HttpPost] 
        public IActionResult Token([FromBody] TokenGenerateInfo tokenGenerateInfo)
        {
            if (ModelState.IsValid)//判断是否合法
            {
                if (!Appsetting.JwtConfig.OauthUsers.Contains(MyEncryption.CreateMD5Key(tokenGenerateInfo.UserIdentification)))
                {
                    return BadRequest("Create JWT failed that illegal user");
                }

                if(tokenGenerateInfo.Expire<=0)
                {
                    tokenGenerateInfo.Expire = 60;
                }
                string nowToken = CreateJWT(tokenGenerateInfo);
                return Ok(new
                {
                    Token = nowToken,
                    Expire = tokenGenerateInfo.Expire*60
                });
            }

            return BadRequest("Create JWT failed that parameter error");
        }

        private string CreateJWT(TokenGenerateInfo user)
        {
            var claim = new Claim[]{
                    new Claim(ClaimTypes.Name,user.UserName??"anonymous"),
                    new Claim(JwtRegisteredClaimNames.Sub, "build"),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")), //声明为JWT提供了唯一的标识符
                    new Claim(ClaimTypes.Role,"admin")
                };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Appsetting.JwtConfig.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                Appsetting.JwtConfig.Issuer,
                Appsetting.JwtConfig.Audience,
                claim,
                DateTime.Now,
                DateTime.Now.AddMinutes(user.Expire),
                creds);

           return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
