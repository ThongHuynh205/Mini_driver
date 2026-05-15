namespace Mini_driver.Client.Models
{
    public class User
    {
        // Tương ứng với cột UserID (Primary Key) trong MySQL
        public int UserID { get; set; }

        // Tên đăng nhập
        public string Username { get; set; } = string.Empty;

        // Mật khẩu (Trong thực tế nên được mã hóa Hash)
        public string Password { get; set; } = string.Empty;

        // Tên thư mục riêng của User trên Server (ví dụ: "User_1")
        public string UserFolder { get; set; } = string.Empty;
    }
}