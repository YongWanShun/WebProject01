using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebProject.Models;

namespace WebProject.Controllers
{
    public class UsersController : Controller
    {
        private readonly WebProjectContext _context;

        public UsersController(WebProjectContext context)
        {
            _context = context;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,Username,Email,Password,Role,RegisterDate,IsActive")] User user)
        {
            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,Username,Email,Password,Role,RegisterDate,IsActive")] User user)
        {
            if (id != user.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }


        // GET: 顯示登入頁面
        public IActionResult Login()
        {
            return View();
        }

        // POST: Users/Login (處理 Modal 傳來的登入請求)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string account, string password) // 這裡直接接參數，比較簡單
        {
            // 1. 去資料庫找人 (比對 Username 或 Email)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => (u.Username == account || u.Email == account) && u.Password == password);

            if (user == null)
            {
                // 登入失敗：回傳一段 JS 讓前端跳 Alert (因為我們是在 Modal 裡，這樣處理最快)
                return Content("<script>alert('賬戶/密碼錯誤'); window.location.href='/';</script>", "text/html;charset=utf-8");
            }

            if (user.IsActive == false)
            {
                return Content("<script>alert('此帳號已被停用，無法登入。'); window.location.href='/';</script>", "text/html ;charset=utf-8");
            }

            // 2. 【關鍵】建立身分證 (Claims)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                
                // ★ 這裡決定權限！對應資料庫的 Role 欄位 (admin / user)
                new Claim(ClaimTypes.Role, user.Role ?? "user"),

                new Claim("UserId", user.UserId.ToString()) // 存 ID 方便以後留言用
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            // 3. 發送 Cookie (登入成功)
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            // 4. 重新整理頁面 (或是導回首頁)
            // Request.Headers["Referer"] 可以讓使用者回到原本所在的頁面
            string referer = Request.Headers["Referer"].ToString();
            return Redirect(string.IsNullOrEmpty(referer) ? "/" : referer);
        }

        // GET: Users/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> ToggleActive(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // 切換狀態：如果是 true 變 false，如果是 false 變 true
            // 注意：因為 IsActive 是 bool? (Nullable)，我們用 GetGetValueOrDefault() 來處理
            bool currentStatus = user.IsActive.GetValueOrDefault(true);
            user.IsActive = !currentStatus;

            await _context.SaveChangesAsync();

            // 切換完後，導回列表頁
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DisableUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // 切換狀態：如果是 true 變 false，如果是 false 變 true
            // 注意：因為 IsActive 是 bool? (Nullable)，我們用 GetGetValueOrDefault() 來處理
            bool currentStatus = user.IsActive.GetValueOrDefault(true);
            user.IsActive = !currentStatus;

            await _context.SaveChangesAsync();

            // 切換完後，導回列表頁
            return RedirectToAction(nameof(Index));
        }
    }
}
