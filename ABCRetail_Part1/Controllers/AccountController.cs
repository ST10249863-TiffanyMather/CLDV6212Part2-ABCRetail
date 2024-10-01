using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;
using System.Security.Claims;
using ABCRetail_Part1.Services;
using ABCRetail_Part1.Models;

namespace ABCRetail_Part1.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        //constructor to initialize IAuthService
        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        //handle registration form submission
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            //check if the form data is valid
            if (ModelState.IsValid)
            {
                //call AuthService to register the user
                bool success = await _authService.RegisterAsync(model.Email, model.Password, model.FullName);

                if (success)
                    return RedirectToAction("Login");

                ModelState.AddModelError("", "User already exists.");
            }

            return View(model);
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        //handle login form submission
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            //check if the form data is valid
            if (ModelState.IsValid)
            {
                //call AuthService to attempt login
                var user = await _authService.LoginAsync(model.Email, model.Password);

                if (user != null)
                {
                    //create a list of claims to store user info in the cookie
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), 
                        new Claim(ClaimTypes.Name, user.UserName), 
                        new Claim(ClaimTypes.Email, user.Email) 
                    };

                    //assign the "Admin" role 
                    if (user.Email == "mick@goat.com")
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "Admin")); 
                    }

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    //sign in the user 
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity)
                    );

                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Invalid login attempt.");
            }

            return View(model);
        }

        // POST: /Account/Logout
        //handle user logout
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            //sign out the user 
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login", "Account");
        }
    }
}
