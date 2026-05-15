using System;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace Mini_driver.Client.Networking
{
    public class ClientSocket
    {
        private static ClientSocket _instance;

        // Fix lỗi CS8370 từ image_1f7ef7.png: Thay ??= bằng kiểm tra if truyền thống
        public static ClientSocket Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ClientSocket();
                }
                return _instance;
            }
        }

        public TcpClient Client;
        public NetworkStream Stream;
        public int CurrentUserId;
        public string CurrentUsername;

        private ClientSocket() { }

        // Kết nối tới Server
        public bool Connect(string ip, int port)
        {
            try
            {
                Client = new TcpClient(ip, port);
                Stream = Client.GetStream();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối: " + ex.Message);
                return false;
            }
        }

        // Lệnh 1: Đăng nhập
        public bool Login(string user, string pass)
        {
            try
            {
                Stream.WriteByte(1); // Gửi Command ID cho Login
                byte[] data = Encoding.UTF8.GetBytes(user + "|" + pass);
                Stream.Write(data, 0, data.Length);

                int response = Stream.ReadByte();
                if (response == 1)
                {
                    this.CurrentUsername = user;
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        // Lệnh 2: Tải file lên (Upload)
        public void UploadFile(string filePath, ProgressBar progressBar)
        {
            try
            {
                FileInfo fi = new FileInfo(filePath);
                Stream.WriteByte(2); // Mã lệnh Upload

                // Tạo Header 208 bytes (200 bytes tên file + 8 bytes kích thước)
                byte[] header = new byte[208];
                byte[] fileNameBytes = Encoding.UTF8.GetBytes(fi.Name);

                // Copy tên file vào 200 byte đầu
                Array.Copy(fileNameBytes, 0, header, 0, Math.Min(fileNameBytes.Length, 200));
                // Copy kích thước file (long) vào 8 byte cuối
                Array.Copy(BitConverter.GetBytes(fi.Length), 0, header, 200, 8);

                Stream.Write(header, 0, 208);

                // Gửi dữ liệu file theo từng khối (Chunk)
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[8192];
                    long totalSent = 0;
                    int bytesRead;

                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        Stream.Write(buffer, 0, bytesRead);
                        totalSent += bytesRead;

                        // Cập nhật ProgressBar an toàn từ luồng khác
                        if (progressBar != null)
                        {
                            progressBar.Invoke(new Action(() => {
                                progressBar.Value = (int)((totalSent * 100) / fi.Length);
                            }));
                        }
                    }
                }
                MessageBox.Show("Tải lên tệp tin thành công!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi trong quá trình tải lên: " + ex.Message);
            }
        }

        // Lệnh 4: Lấy danh sách người dùng đang Online
        public string GetOnlineUsers()
        {
            try
            {
                Stream.WriteByte(4);
                byte[] lenBuffer = new byte[4];
                int read = Stream.Read(lenBuffer, 0, 4);
                if (read < 4) return "";

                int len = BitConverter.ToInt32(lenBuffer, 0);
                if (len <= 0) return "";

                byte[] data = new byte[len];
                Stream.Read(data, 0, len);
                return Encoding.UTF8.GetString(data);
            }
            catch { return ""; }
        }

        // Lệnh 5: Cấp quyền cho User khác
        public bool GrantPermission(int targetUserId)
        {
            try
            {
                Stream.WriteByte(5);
                byte[] data = BitConverter.GetBytes(targetUserId);
                Stream.Write(data, 0, 4);

                int result = Stream.ReadByte();
                return result == 1;
            }
            catch { return false; }
        }

        // Đóng kết nối
        public void Disconnect()
        {
            try
            {
                Stream?.Close();
                Client?.Close();
            }
            catch { }
        }
    }
}