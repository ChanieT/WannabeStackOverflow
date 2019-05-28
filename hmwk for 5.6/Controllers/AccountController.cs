using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace hmwk_for_5._6.Controllers
{
    public class AccountController : Controller
    {
        private string _conn;
        public AccountController(IConfiguration configuration)
        {
            _conn = configuration.GetConnectionString("ConStr");
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(User user)
        {
            var repository = new QuestionsRepository(_conn);
            User u = repository.GetUser(user);
            if (u == null)
            {
                return Redirect("/account/login");
            }

            else if (!repository.Match(user.Password, u.Password))
            {
                return Redirect("/account/login");
            }

            var claims = new List<Claim>
                {
                    new Claim("user", user.Email)
                };
            HttpContext.SignInAsync(new ClaimsPrincipal(
                new ClaimsIdentity(claims, "Cookies", "user", "role"))).Wait();


            return Redirect("/home/index");
        }

        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SignUp(User user)
        {
            var repository = new QuestionsRepository(_conn);
            repository.AddUser(user);
            return Redirect("/account/login");
        }

        public IActionResult Logout()
        {
            HttpContext.SignOutAsync().Wait();
            return Redirect("/account/login");
        }
    }
}