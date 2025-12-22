using System.Collections.Generic;
using WebProject.Models;
namespace WebProject.Models.HomeView
{
    public class HomeViewModel
    {
        // 1. 文章列表
        public List<Post> Posts { get; set; }

        // 2. 分類列表 (取代原本的 ViewBag)
        public List<Category> Categories { get; set; }

        // 3. (新功能) 最新註冊的會員列表
        public List<User> LatestUsers { get; set; }
        // --- 新增分頁資訊 ---
        public int CurrentPage { get; set; }  // 目前頁數
        public int TotalPages { get; set; }   // 總頁數
        public int? CurrentCategoryId { get; set; } // 目前選擇的分類
    }
}
