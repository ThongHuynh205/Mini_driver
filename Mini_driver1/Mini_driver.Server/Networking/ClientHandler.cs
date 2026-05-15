using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Mini_driver.Server.Database;
using Mini_driver.Server.Models;

namespace Mini_driver.Server.Networking
{
    public class ClientHandler
    {
        // Danh sách tĩnh lưu trữ tập trung toàn bộ Client đang giữ kết nối TCP thông suốt
        private static readonly List<ClientHandler> ActiveClients = new List<ClientHandler>();

        private readonly TcpClient _client;
        private User _currentUser;
        private StreamWriter _writer;

        public ClientHandler(TcpClient client)
        {
            _client = client;
        }

        // Phát tin nhắn thông báo danh sách Online cập nhật cho TOÀN BỘ các máy đang online
        private static void BroadcastOnlineList()
        {
            StringBuilder sb = new StringBuilder("ONLINE_LIST");
            lock (ActiveClients)
            {
                foreach (var handler in ActiveClients)
                {
                    if (handler._currentUser != null)
                    {
                        sb.Append("|" + handler._currentUser.Username);
                    }
                }
            }

            string onlineListMsg = sb.ToString();

            lock (ActiveClients)
            {
                foreach (var handler in ActiveClients)
                {
                    try
                    {
                        if (handler._writer != null)
                        {
                            handler._writer.WriteLine(onlineListMsg);
                            handler._writer.Flush();
                        }
                    }
                    catch { } // Bỏ qua nếu lỗi đường truyền của một client cá biệt
                }
            }
        }

        // Hàm xử lý định dạng quét thuộc tính File chi tiết (Tên?Dung lượng?Ngày sửa)
        private string GetUserFilesPayload(string folderPath)
        {
            StringBuilder sb = new StringBuilder("FILE_LIST");
            if (Directory.Exists(folderPath))
            {
                string[] files = Directory.GetFiles(folderPath);
                foreach (string file in files)
                {
                    FileInfo fi = new FileInfo(file);
                    long sizeInKB = fi.Length / 1024;
                    if (sizeInKB == 0 && fi.Length > 0) sizeInKB = 1; // Làm tròn nếu file < 1KB

                    string dateStr = fi.LastWriteTime.ToString("dd/MM/yyyy HH:mm");

                    // Nối chuỗi quy ước: TênFile?DungLượng KB?NgàyGiờ
                    sb.Append("|" + fi.Name + "?" + sizeInKB + " KB?" + dateStr);
                }
            }
            return sb.ToString();
        }

        public void Handle()
        {
            string clientEndPoint = _client.Client.RemoteEndPoint.ToString();
            Console.WriteLine("[KẾT NỐI] Thiết bị mới kết nối từ địa chỉ: " + clientEndPoint);

            try
            {
                using (NetworkStream stream = _client.GetStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    _writer = writer;
                    _writer.AutoFlush = true;
                    string request;

                    // Thêm kết nối hiện tại vào danh sách quản lý
                    lock (ActiveClients)
                    {
                        ActiveClients.Add(this);
                    }

                    while ((request = reader.ReadLine()) != null)
                    {
                        Console.WriteLine("[DỮ LIỆU NHẬN] Từ (" + clientEndPoint + "): " + request);
                        string[] parts = request.Split('|');
                        string command = parts[0];

                        // XỬ LÝ LỆNH ĐĂNG NHẬP / KẾT NỐI
                        if (command == "CONNECT")
                        {
                            string username = parts[1];
                            _currentUser = new User(0, username, username + "_folder");

                            _writer.WriteLine("CONNECT_SUCCESS|" + _currentUser.Username);
                            _writer.Flush();

                            // Tạo thư mục riêng cho User trên Server nếu chưa tồn tại
                            string serverStoragePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", _currentUser.UserFolder);
                            if (!Directory.Exists(serverStoragePath))
                            {
                                Directory.CreateDirectory(serverStoragePath);
                            }

                            // Cập nhật danh sách online diện rộng
                            BroadcastOnlineList();
                        }
                        // XỬ LÝ LỆNH ĐÒI DANH SÁCH FILE CŨ
                        else if (command == "GET_FILES")
                        {
                            if (_currentUser == null) continue;

                            string targetFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", _currentUser.UserFolder);
                            _writer.WriteLine(GetUserFilesPayload(targetFolder));
                            _writer.Flush();
                        }
                        // XỬ LÝ LỆNH UPLOAD FILE
                        else if (command == "UPLOAD")
                        {
                            if (_currentUser == null) continue;

                            string fileName = parts[1];
                            long fileSize = Convert.ToInt64(parts[2]);

                            _writer.WriteLine("READY_TO_RECEIVE");
                            _writer.Flush();

                            string targetFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", _currentUser.UserFolder);
                            string fullSavePath = Path.Combine(targetFolder, fileName);

                            bool isSuccess = FileTransferService.ReceiveFile(stream, fullSavePath, fileSize);
                            if (isSuccess)
                            {
                                _writer.WriteLine("UPLOAD_SUCCESS|" + fileName);
                                _writer.Flush();
                                Console.WriteLine("[UPLOAD THÀNH CÔNG] Đã lưu file: " + fileName);
                            }
                        }
                        // XỬ LÝ LỆNH DOWNLOAD FILE
                        else if (command == "DOWNLOAD")
                        {
                            if (_currentUser == null) continue;

                            string fileName = parts[1];
                            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", _currentUser.UserFolder, fileName);

                            if (File.Exists(fullPath))
                            {
                                long size = new FileInfo(fullPath).Length;
                                _writer.WriteLine("START_DOWNLOAD|" + fileName + "|" + size);
                                _writer.Flush();

                                FileTransferService.SendFile(stream, fullPath);
                                Console.WriteLine("[DOWNLOAD THÀNH CÔNG] Đã gửi xong file '" + fileName + "'");
                            }
                        }
                        // XỬ LÝ LỆNH XÓA FILE THỰC TẾ
                        else if (command == "DELETE")
                        {
                            if (_currentUser == null) continue;

                            string fileName = parts[1];
                            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", _currentUser.UserFolder, fileName);

                            if (File.Exists(fullPath))
                            {
                                File.Delete(fullPath);
                                Console.WriteLine("[HỆ THỐNG] Đã xóa file thực tế: " + fullPath);

                                // Trả lại danh sách file mới sau khi xóa cho Client cập nhật giao diện
                                string targetFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", _currentUser.UserFolder);
                                _writer.WriteLine(GetUserFilesPayload(targetFolder));
                                _writer.Flush();
                            }
                        }
                        // XỬ LÝ LỆNH CHIA SẺ FILE (NGƯỜI GỬI ĐỀ XUẤT)
                        else if (command == "SHARE")
                        {
                            if (_currentUser == null) continue;

                            string fileName = parts[1];
                            string targetUser = parts[2];

                            bool isTargetOnline = false;
                            lock (ActiveClients)
                            {
                                foreach (var handler in ActiveClients)
                                {
                                    // Tìm xem người nhận có đang online thực tế hay không
                                    if (handler._currentUser != null && handler._currentUser.Username.Equals(targetUser, StringComparison.OrdinalIgnoreCase))
                                    {
                                        // Gửi lời mời SHARE_REQUEST thẳng sang màn hình người nhận
                                        handler._writer.WriteLine($"SHARE_REQUEST|{_currentUser.Username}|{fileName}");
                                        handler._writer.Flush();
                                        isTargetOnline = true;
                                        break;
                                    }
                                }
                            }

                            if (!isTargetOnline)
                            {
                                _writer.WriteLine($"SHARE_NOTIFY|Tài khoản [{targetUser}] hiện không online. Không thể chia sẻ real-time!");
                                _writer.Flush();
                            }
                        }
                        // NGƯỜI NHẬN BẤM ĐỒNG Ý NHẬN FILE CHIA SẺ
                        else if (command == "SHARE_ACCEPT")
                        {
                            if (_currentUser == null) continue;

                            string senderUser = parts[1];
                            string fileName = parts[2];

                            string sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", senderUser + "_folder", fileName);
                            string targetFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", _currentUser.UserFolder);

                            if (File.Exists(sourcePath))
                            {
                                string destPath = Path.Combine(targetFolder, fileName);
                                File.Copy(sourcePath, destPath, true); // Sao chép file đè nếu trùng tên

                                // 1. Refresh lại bảng file cho bên NGƯỜI NHẬN ngay lập tức
                                _writer.WriteLine(GetUserFilesPayload(targetFolder));
                                _writer.Flush();

                                // 2. Báo tin vui về cho máy của NGƯỜI GỬI biết
                                lock (ActiveClients)
                                {
                                    foreach (var handler in ActiveClients)
                                    {
                                        if (handler._currentUser != null && handler._currentUser.Username.Equals(senderUser, StringComparison.OrdinalIgnoreCase))
                                        {
                                            handler._writer.WriteLine($"SHARE_NOTIFY|Người dùng [{_currentUser.Username}] ĐÃ ĐỒNG Ý nhận file [{fileName}] của bạn!");
                                            handler._writer.Flush();
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        // NGƯỜI NHẬN BẤM TỪ CHỐI NHẬN FILE CHIA SẺ
                        else if (command == "SHARE_DENY")
                        {
                            if (_currentUser == null) continue;

                            string senderUser = parts[1];
                            string fileName = parts[2];

                            lock (ActiveClients)
                            {
                                foreach (var handler in ActiveClients)
                                {
                                    if (handler._currentUser != null && handler._currentUser.Username.Equals(senderUser, StringComparison.OrdinalIgnoreCase))
                                    {
                                        handler._writer.WriteLine($"SHARE_NOTIFY|Người dùng [{_currentUser.Username}] đã TỪ CHỐI nhận file [{fileName}] của bạn.");
                                        handler._writer.Flush();
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string identity = (_currentUser != null) ? _currentUser.Username : clientEndPoint;
                Console.WriteLine("[NGẮT KẾT NỐI] Máy của [" + identity + "] đã thoát. Chi tiết: " + ex.Message);
            }
            finally
            {
                // Khi client mất kết nối, dọn dẹp sạch sẽ khỏi bộ nhớ Server
                lock (ActiveClients)
                {
                    ActiveClients.Remove(this);
                }
                // Phát lệnh cập nhật danh sách online mới vì có người vừa rời mạng
                BroadcastOnlineList();
                _client.Close();
            }
        }
    }
}