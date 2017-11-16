using FanyiNetwork.Models;
using Marten;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FanyiNetwork.Controllers
{
    public class AccountController : Controller
    {
        private readonly IDocumentStore _documentStore;
        public AccountController(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Login()
        {
            ViewBag.Title = "登录 - 凡易单号管理系统";

            return View();
        }
        public IActionResult Register(string key)
        {
            ViewBag.Title = "注册 - 凡易单号管理系统";

            if (key == null || key != "2017fanyi")
            {
                return RedirectToAction("Login");
            }
            else
            {
                return View();
            }
        }
        public IActionResult Register2(string key)
        {
            ViewBag.Title = "注册 - 凡易单号管理系统";

            if (key == null || key != "2017fanyi")
            {
                return RedirectToAction("Login");
            }
            else
            {
                return View();
            }
        }
        public IActionResult Logout()
        {
            LoggingOut();

            return View();
        }

        [HttpPost]
        public string GetRole()
        {
            if (User.HasClaim(x => x.Type == ClaimTypes.Sid))
            {
                return User.Claims.SingleOrDefault(x => x.Type == ClaimTypes.Role).Value.ToString();
            }
            else
            {
                return "";
            }
        }

        private async Task<IActionResult> LoggingIn(User user) {

            if (!user.status)
            {
                return BadRequest("账号尚未审核！请联系管理员审核后即可登录");
            }
            else
            {
                var claims = new List<Claim>() {
                    new Claim(ClaimTypes.Sid, user.id.ToString()),
                    new Claim(ClaimTypes.Name, user.name),
                    new Claim(ClaimTypes.Role, user.group)
                };

                var userPrinciple = new ClaimsPrincipal(new ClaimsIdentity(claims, "SecureLogin"));

                await HttpContext.Authentication.SignInAsync("Cookie", userPrinciple, new Microsoft.AspNetCore.Http.Authentication.AuthenticationProperties
                {
                    ExpiresUtc = DateTime.Now.AddDays(7),
                    IsPersistent = false,
                    AllowRefresh = false
                });

                using (var session = _documentStore.LightweightSession())
                {
                    LoginHistory lh = new LoginHistory();
                    lh.userID = user.id;
                    lh.ipAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                    lh.addTime = DateTime.Now;

                    session.Store<LoginHistory>(lh);
                    session.SaveChanges();
                }

                return Ok();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] User user)
        {
            if (user != null)
            {
                using (var session = _documentStore.LightweightSession())
                {
                    var existed = session.Query<User>().SingleOrDefault(x => x.account == user.account && x.password == user.password);
                    if (existed != null)
                    {
                        return await LoggingIn(existed);
                    }
                    else {
                        return BadRequest("用户名或密码不正确！");
                    }
                }
            }
            else
            {
                return BadRequest("信息填写不完整！");
            }
        }

        public async Task<IActionResult> LoggingOut()
        {
            await HttpContext.Authentication.SignOutAsync("Cookie");

            return Ok();
        }
        
        [HttpPost]
        public async Task<IActionResult> Register([FromBody]User model)
        {
            if (model != null)
            {
                using (var session = _documentStore.LightweightSession())
                {
                    var existed = session.Query<User>().SingleOrDefault(x=>x.account == model.account);
                    if (existed != null)
                    {
                        return BadRequest("用户名已存在!");
                    }

                    model.status = false;

                    if (model.name == "吕诗丛")
                    {
                        model.status = true;
                    }

                    session.Store<User>(model);
                    session.SaveChanges();

                    return Ok();
                }
            }
            else
            {
                return BadRequest("信息填写不完整！");
            }
        }
        
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ChangePassword(string NewPassword, string RepeatPwd)
        {
            //判断密码不能为空
            if (NewPassword != null && NewPassword != "")
            {
                //判断两次输入密码是否相同
                if (NewPassword == RepeatPwd)
                {
                    //获取用户id
                    int uid = int.Parse(User.FindFirst(ClaimTypes.Sid).Value);
                    using (var session = _documentStore.LightweightSession())
                    {
                        //查询用户信息
                        var user = session.Query<User>().SingleOrDefault(x => x.id == uid);
                        //修改密码
                        user.password = NewPassword;
                        session.Store<User>(user);
                        //保存更新密码
                        session.SaveChanges();
                        return Ok();
                    }

                }
                //输入密码不同
                else
                {
                    return BadRequest("两次输入的密码不一致！");
                }
            }

            else
            {
                return BadRequest("密码不能为空！");
            }
        }
    }
}
