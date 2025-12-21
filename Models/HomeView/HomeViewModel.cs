using System.Collections.Generic;
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
    }
}
