using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebProject.Models;
using WebProject.Models.HomeView;

namespace WebProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly WebProjectContext _context;
        public HomeController(ILogger<HomeController> logger, WebProjectContext context)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? categoryId, string search, int page = 1)
        {

            int pageSize = 5; // 設定每頁顯示 5 筆文章

            var categories = await GetCategoriesAsync();

            var postData = await GetPagedPostsAsync(categoryId, search, page, 5);

            // 2. 準備查詢 (先不執行資料庫查詢，只是串接指令)
            //var postsQuery = _context.Posts
            //                         .Include(p => p.User)      // 包含作者資料
            //                         .Include(p => p.Category)  // 包含分類資料
            //                         .AsQueryable();
            //var posts = await _context.Posts
            //    .Include(p => p.User)
            //    .Include(p => p.Category)
            //    .OrderByDescending(p => p.CreatedAt) // 照時間倒序
            //    .ToListAsync();

            //var categories = await _context.Categories
            //    .Include(c => c.Posts)
            //    .ToListAsync();

            // 3. 計算總筆數 & 總頁數
            //int totalPosts = await postsQuery.CountAsync();
            //int totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);
            //var pagedPosts = await postsQuery
            //                .OrderByDescending(p => p.CreatedAt) // 讓最新的文章排在最上面
            //                .Skip((page - 1) * pageSize)
            //                .Take(pageSize)
            //                .ToListAsync();

            

            var viewModel = new HomeViewModel
            {

                Posts = postData.Posts,
                CurrentPage = postData.CurrentPage,
                TotalPages = postData.TotalPages,
                //Categories = categories
                CurrentCategoryId = categoryId
            };

            // 把搜尋字串存起來，讓 View 可以填回搜尋框，或是換頁時帶著走
            ViewData["CurrentSearch"] = search;

            return View(viewModel);

            
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        private async Task<List<Category>> GetCategoriesAsync()
        {
            return await _context.Categories
                                 .Include(c => c.Posts) // 順便算文章數
                                 .ToListAsync();
        }

        /// <summary>
        /// 專門負責抓取文章 (包含：分類篩選 + 分頁計算)
        /// </summary>
        private async Task<(List<Post> Posts, int TotalPages, int CurrentPage)> GetPagedPostsAsync(int? categoryId, string search, int page, int pageSize)
        {
            // A. 準備查詢
            var query = _context.Posts
                                .Include(p => p.User)
                                .Include(p => p.Category)
                                .Include(p => p.Comments)
                                .AsQueryable();

            //  【新增】搜尋邏輯 (標題 或 內容 包含關鍵字)
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Title.Contains(search) || p.Content.Contains(search));
            }

            // B. 如果有傳入分類 ID，就進行篩選 (WHERE)
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            // C. 計算分頁
            int totalPosts = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);

            // 防止頁碼超出範圍
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            // D. 執行查詢 (Skip & Take)
            var posts = await query
                                .OrderByDescending(p => p.CreatedAt)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToListAsync();

            // 回傳多個結果 (使用 C# Tuple 語法)
            return (posts, totalPages, page);
        }
    }
}
