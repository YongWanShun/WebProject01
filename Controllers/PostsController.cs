using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using WebProject.Models;

namespace WebProject.Controllers
{
    public class PostsController : Controller
    {
        private readonly WebProjectContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private static readonly TimeSpan ViewCountProtectionWindow = TimeSpan.FromMinutes(1);

        public PostsController(WebProjectContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Posts
        public async Task<IActionResult> Index()
        {
            var webProjectContext = _context.Posts.Include(p => p.Category).Include(p => p.User);
            return View(await webProjectContext.ToListAsync());
        }

        // GET: Posts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.Category)
                .Include(p => p.User)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User) // 順便抓留言者資料
                .Include(p => p.Likes)
                .FirstOrDefaultAsync(m => m.PostId == id);
            if (post == null)
            {
                return NotFound();
            }

            var cookieKey = "ViewedPosts"; // Cookie 名稱
            Request.Cookies.TryGetValue(cookieKey, out string viewedPostsCookie);

            var viewedIds = new HashSet<string>(
                (viewedPostsCookie ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
            );

            string currentId = id.Value.ToString();
            if (!viewedIds.Contains(currentId))
            {
                // 第一次在這個瀏覽器看這篇文章，瀏覽數 +1
                post.ViewCount = (post.ViewCount ?? 0) + 1;
                await _context.SaveChangesAsync();

                // 把這篇文章 ID 記錄到 Cookie
                viewedIds.Add(currentId);
                var newCookieValue = string.Join(",", viewedIds);

                Response.Cookies.Append(cookieKey, newCookieValue, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.Add(ViewCountProtectionWindow),
                    HttpOnly = true
                });
            }

            return View(post);
        }

        // GET: Posts/Create
        public IActionResult Create()
        {
            var categories = _context.Categories.ToList();
            ViewBag.CategoryList = new SelectList(categories, "CategoryId", "Description");
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId");
            return View();
        }

        // POST: Posts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PostCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // 表單驗證失敗：重新載入分類下拉選單並回傳同一視圖，以顯示驗證錯誤
                var categories = _context.Categories.ToList();
                ViewBag.CategoryList = new SelectList(categories, "CategoryId", "Description", model.CategoryId);
                return View(model);
            }

            // 驗證成功：建立 Post 實體並設定各屬性
            var post = new Post
            {
                Title = model.Title,
                Content = model.Content,
                CategoryId = model.CategoryId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,       // 記得設定 UpdatedAt 为当前時間
                UserId = 1                      // 暫時將作者 UserId 設為 1
            };

            // 處理圖片上傳
            if (model.Thumbnail != null && model.Thumbnail.Length > 0)
            {
                // 確保上傳目錄存在，若無則建立 /wwwroot/uploads
                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                // 產生不重複的檔名，保留原始副檔名
                string fileExt = Path.GetExtension(model.Thumbnail.FileName);
                string fileName = Guid.NewGuid().ToString() + fileExt;
                string filePath = Path.Combine(uploadsFolder, fileName);

                // 將檔案保存到伺服器路徑
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Thumbnail.CopyToAsync(stream);
                }

                // 將圖片的完整網站 URL 存入資料庫（例如：https://your-domain/uploads/xxx.jpg）
                string fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";
                post.ThumbnailUrl = fileUrl;
            }

            // 將新文章儲存至資料庫，並導向 Index 列表頁面
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Posts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryId", post.CategoryId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", post.UserId);
            return View(post);
        }

        // POST: Posts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PostId,Title,Content,UserId,CategoryId,CreatedAt,UpdatedAt,ViewCount,IsDeleted,ThumbnailUrl")] Post post)
        {
            if (id != post.PostId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(post);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PostExists(post.PostId))
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
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryId", post.CategoryId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", post.UserId);
            return View(post);
        }

        // GET: Posts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.Category)
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.PostId == id);
            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // POST: Posts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                _context.Posts.Remove(post);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PostExists(int id)
        {
            return _context.Posts.Any(e => e.PostId == id);
        }

        // ==========================================
        // 2. 新增 AddComment：處理留言提交
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int PostId, string Contents, int? ParentId)
        {
            // A. 檢查是否登入
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Users"); // 或是跳出 Alert
            }

            // B. 取得目前登入者的 ID
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Users");
            int userId = int.Parse(userIdStr);

            // C. 檢查帳號是否被停用 (配合您上一個功能的邏輯)
            // 這裡需要多查一次 DB 確保安全，或是您可以選擇信任 Cookie 裡的資訊
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.IsActive == false)
            {
                return Content("<script>alert('您的帳號已被停用，無法留言。'); window.location.href='/';</script>", "text/html; charset=utf-8");
            }

            // D. 建立留言
            var comment = new Comment
            {
                PostId = PostId,
                UserId = userId,
                ParentId = ParentId,
                Content = Contents,
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // E. 完成後，導回該文章的詳情頁，讓使用者看到自己的留言
            return RedirectToAction("Details", new { id = PostId });
        }

        // 2. 新增 ToggleLike 方法 (處理按讚)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike(int id)
        {
            // A. 檢查是否登入
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Users");
            }

            // B. 取得目前使用者 ID
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Users");
            int userId = int.Parse(userIdStr);

            // C. 檢查這個人有沒有按過這篇文章的讚
            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == id && l.UserId == userId);

            if (existingLike != null)
            {
                // 如果按過了 -> 取消讚 (刪除紀錄)
                _context.Likes.Remove(existingLike);
            }
            else
            {
                // 如果沒按過 -> 按讚 (新增紀錄)
                var like = new Like
                {
                    PostId = id,
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };
                _context.Likes.Add(like);
            }

            await _context.SaveChangesAsync();

            // D. 導回文章頁面 (顯示最新的讚數)
            return RedirectToAction("Details", new { id = id });
        }
    }
}
