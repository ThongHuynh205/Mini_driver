using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Mini_driver.Server.Database;
using Mini_driver.Server.Models;

namespace Mini_driver.Server.Networking
{
    public class ClientHandler
    {
        private readonly TcpClient _client;
        private User _currentUser;

        public ClientHandler(TcpClient client)
        {
            _client = client;
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
                    writer.AutoFlush = true;
                    string request;

                    while ((request = reader.ReadLine()) != null)
                    {
                        Console.WriteLine("[DỮ LIỆU NHẬN] Từ (" + clientEndPoint + "): " + request);
                        string[] parts = request.Split('|');
                        string command = parts[0];

                        if (command == "CONNECT")
                        {
                            string username = parts[1];
                            Console.WriteLine("[THAO TÁC] Tài khoản '" + username + "' yêu cầu kết nối mạng...");

                            _currentUser = new User(0, username, username + "_folder");

                            writer.WriteLine("CONNECT_SUCCESS|" + _currentUser.Username);
                            writer.Flush();

                            string serverStoragePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", _currentUser.UserFolder);
                            if (!Directory.Exists(serverStoragePath))
                            {
                                Directory.CreateDirectory(serverStoragePath);
                            }
                        }
                        // --- ĐOẠN THÊM MỚI: XỬ LÝ LỆCH QUÉT FILE TRONG THƯ MỤC CỦA USER ---
                        else if (command == "GET_FILES")
                        {
                            if (_currentUser == null) continue;

                            string targetFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", _currentUser.UserFolder);
                            StringBuilder sb = new StringBuilder("FILE_LIST");

                            if (Directory.Exists(targetFolder))
                            {
                                string[] files = Directory.GetFiles(targetFolder);
                                foreach (string file in files)
                                {
                                    sb.Append("|" + Path.GetFileName(file));
                                }
                            }

                            writer.WriteLine(sb.ToString());
                            writer.Flush();
                            Console.WriteLine("[HỆ THỐNG] Đã gửi danh sách file hiện có cho: " + _currentUser.Username);
                        }
                        else if (command == "UPLOAD")
                        {
                            if (_currentUser == null) continue;

                            string fileName = parts[1];
                            long fileSize = Convert.ToInt64(parts[2]);

                            writer.WriteLine("READY_TO_RECEIVE");
                            writer.Flush();

                            string targetFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", _currentUser.UserFolder);
                            string fullSavePath = Path.Combine(targetFolder, fileName);

                            bool isSuccess = FileTransferService.ReceiveFile(stream, fullSavePath, fileSize);
                            if (isSuccess)
                            {
                                writer.WriteLine("UPLOAD_SUCCESS|" + fileName);
                                writer.Flush();
                                Console.WriteLine("[UPLOAD THÀNH CÔNG] Đã lưu file: " + fileName);
                            }
                        }
                        else if (command == "DOWNLOAD")
                        {
                            if (_currentUser == null) continue;

                            string fileName = parts[1];
                            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", _currentUser.UserFolder, fileName);

                            if (File.Exists(fullPath))
                            {
                                long size = new FileInfo(fullPath).Length;
                                writer.WriteLine("START_DOWNLOAD|" + fileName + "|" + size);
                                writer.Flush();

                                FileTransferService.SendFile(stream, fullPath);
                                Console.WriteLine("[DOWNLOAD THÀNH CÔNG] Đã gửi xong file '" + fileName + "'");
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
                _client.Close();
            }
        }
    }
}