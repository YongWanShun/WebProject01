using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WebProject.Models
{
    public class PostCreateViewModel
    {
        [Required(ErrorMessage = "請輸入標題")]
        public string Title { get; set; }

        [Required(ErrorMessage = "請輸入內容")]
        public string Content { get; set; }

        [Required(ErrorMessage = "請選擇分類")]
        public int CategoryId { get; set; }

        // 上傳的縮圖圖片檔案
        public IFormFile? Thumbnail { get; set; }
    }
}
