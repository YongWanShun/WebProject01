using System.ComponentModel.DataAnnotations;

namespace WebProject.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "請輸入 Email 或 帳號")]
        [Display(Name = "帳號 / Email")]
        public string Account { get; set; }

        [Required(ErrorMessage = "請輸入密碼")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}