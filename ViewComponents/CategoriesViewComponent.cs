using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebProject.Models;

namespace WebProject.ViewComponents
{
    // 繼承 ViewComponent
    public class CategoriesViewComponent : ViewComponent
    {
        private readonly WebProjectContext _context;

        public CategoriesViewComponent(WebProjectContext context)
        {
            _context = context;
        }

        //這就是它的 "Action"，類似 Controller 的 Index
        public async Task<IViewComponentResult> InvokeAsync()
        {
            // 獨立的資料讀取邏輯
            var categories = await _context.Categories
                                           .Include(c => c.Posts) // 順便算文章數
                                           .ToListAsync();

            return View("CategoriesSideBar",categories); // 預設會找 Default.cshtml
        }
    }
}
