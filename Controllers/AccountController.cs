using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // 必須引用
using WebProject.Models;

namespace WebProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly WebProjectContext _context;

        public AccountController(WebProjectContext context)
        {
            _context = context;
        }

        // GET: 顯示登入頁面
        public IActionResult Login()
        {
            return View();
        }

        // POST: 處理登入邏輯
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. 找使用者 (比對 Username 或 Email)
            // 注意：這裡先抓出來，密碼比對在後面做
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Account || u.Username == model.Account);

            // 2. 檢查帳號是否存在，以及密碼是否正確
            // TODO: 實際專案中，資料庫的密碼應該是雜湊過的 (Hashed)，這裡暫時示範明碼比對
            if (user == null || user.Password != model.Password)
            {
                ModelState.AddModelError(string.Empty, "帳號或密碼錯誤");
                return View(model);
            }

            // 3. 建立身分證 (Claims)
            // 這些資料會被加密存在 Cookie 裡，以後你在任何 Controller 都能讀取
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username), // User.Identity.Name
                new Claim(ClaimTypes.Role, user.Role ?? "User"), // User.IsInRole("Admin")
                new Claim("UserId", user.UserId.ToString()) // 自定義欄位：User.FindFirst("UserId")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true // 記住我 (關掉瀏覽器下次還在)
            };

            // 4. 登入 (發送 Cookie)
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            return RedirectToAction("Index", "Home");
        }

        // 登出
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}