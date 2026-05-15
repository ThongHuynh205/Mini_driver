using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Mini_driver.Client
{
    public partial class MainForm : Form
    {
        private readonly string _user;
        private TcpClient _client;
        private NetworkStream _stream;
        private StreamWriter _writer;
        private StreamReader _reader;
        private Thread _listenThread;
        private bool _isConnected;

        public MainForm(string user)
        {
            InitializeComponent();
            _user = user;
            Text = "Mini Driver - Đang đăng nhập: " + _user;
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            lblStatus.Text = "Chào " + _user + "!";
            txtIPServer.Text = "127.0.0.1";

            // Đảm bảo ListView hiển thị dạng bảng (Details) để thấy được Size và Date
            lvwFiles.View = View.Details;
        }

        private void ExecuteConnect()
        {
            try
            {
                lblBottomInfo.Text = "Đang kết nối đến Server...";

                _client = new TcpClient(txtIPServer.Text, 8888);
                _stream = _client.GetStream();

                _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
                _reader = new StreamReader(_stream, Encoding.UTF8);

                // Gửi lệnh định danh tài khoản lên Server
                _writer.WriteLine("CONNECT|" + _user);

                string response = _reader.ReadLine();

                if (response != null && response.StartsWith("CONNECT_SUCCESS"))
                {
                    _isConnected = true;
                    lblStatus.Text = "🟢 Connected";
                    lblStatus.ForeColor = Color.Green;
                    lblBottomInfo.Text = "Đã kết nối tới Server: " + txtIPServer.Text;
                    MessageBox.Show("Kết nối Server thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Yêu cầu Server gửi danh sách file cũ về nạp vào giao diện
                    _writer.WriteLine("GET_FILES");

                    // Khởi tạo luồng phụ chạy ngầm lắng nghe tín hiệu liên tục từ Server
                    _listenThread = new Thread(ListenToServer) { IsBackground = true };
                    _listenThread.Start();
                }
                else
                {
                    lblStatus.Text = "🔴 Kết nối lỗi";
                    lblStatus.ForeColor = Color.Red;
                    MessageBox.Show("Server từ chối kết nối!", "Thông báo lỗi");
                    CloseConnection();
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "🔴 Ngắt kết nối";
                lblStatus.ForeColor = Color.Red;
                MessageBox.Show("Không thể kết nối đến Server: " + ex.Message, "Lỗi");
            }
        }

        // LUỒNG NGẦM: Chuyên xử lý đọc và phân tích các gói tin chữ từ Server trả về Real-time
        private void ListenToServer()
        {
            try
            {
                string response;
                while (_isConnected && (response = _reader.ReadLine()) != null)
                {
                    string[] parts = response.Split('|');
                    string command = parts[0];

                    // 1. Cập nhật danh sách tài khoản đang Online thực tế
                    if (command == "ONLINE_LIST")
                    {
                        Invoke(new Action(() =>
                        {
                            if (Controls.Find("lstOnline", true).Length > 0)
                            {
                                ListBox lstOnline = (ListBox)Controls.Find("lstOnline", true)[0];
                                lstOnline.Items.Clear();
                                for (int i = 1; i < parts.Length; i++)
                                {
                                    if (!string.IsNullOrEmpty(parts[i])) lstOnline.Items.Add(parts[i]);
                                }
                            }
                        }));
                    }
                    // 2. Cập nhật danh sách File (bao gồm cả Tên, Size, Ngày giờ)
                    else if (command == "FILE_LIST")
                    {
                        Invoke(new Action(() =>
                        {
                            lvwFiles.Items.Clear();
                            for (int i = 1; i < parts.Length; i++)
                            {
                                if (!string.IsNullOrEmpty(parts[i]))
                                {
                                    string[] fileData = parts[i].Split('?');
                                    string name = fileData[0];
                                    string size = fileData.Length > 1 ? fileData[1] : "0 KB";
                                    string date = fileData.Length > 2 ? fileData[2] : "";

                                    ListViewItem item = new ListViewItem(name);
                                    item.SubItems.Add(size);
                                    item.SubItems.Add(date);
                                    lvwFiles.Items.Add(item);
                                }
                            }
                        }));
                    }
                    // 3. Nhận yêu cầu được Share file từ một máy khác đổ về
                    else if (command == "SHARE_REQUEST")
                    {
                        string senderUser = parts[1];
                        string sharedFileName = parts[2];

                        Invoke(new Action(() =>
                        {
                            DialogResult result = MessageBox.Show(
                                $"Người dùng [{senderUser}] muốn chia sẻ file [{sharedFileName}] cho bạn.\nBạn có đồng ý nhận file này không?",
                                "Yêu cầu nhận file chia sẻ",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question);

                            if (result == DialogResult.Yes)
                            {
                                _writer.WriteLine($"SHARE_ACCEPT|{senderUser}|{sharedFileName}");
                            }
                            else
                            {
                                _writer.WriteLine($"SHARE_DENY|{senderUser}|{sharedFileName}");
                            }
                        }));
                    }
                    // 4. Nhận các thông báo trạng thái văn bản chung từ hệ thống Server
                    else if (command == "SHARE_NOTIFY")
                    {
                        string message = parts[1];
                        Invoke(new Action(() =>
                        {
                            MessageBox.Show(message, "Thông báo hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }));
                    }
                }
            }
            catch
            {
                Invoke(new Action(() =>
                {
                    lblStatus.Text = "🔴 Mất kết nối mạng";
                    lblStatus.ForeColor = Color.Red;
                }));
            }
        }

        private void ExecuteUpload()
        {
            if (_client == null || !_client.Connected)
            {
                MessageBox.Show("Vui lòng bấm kết nối Server trước!", "Nhắc nhở");
                return;
            }

            using (OpenFileDialog ofd = new OpenFileDialog { Filter = "All Files (*.*)|*.*" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string filePath = ofd.FileName;
                        string fileName = ofd.SafeFileName;
                        long fileSize = new FileInfo(filePath).Length;

                        lblBottomInfo.Text = "Đang gửi yêu cầu upload: " + fileName;
                        _writer.WriteLine("UPLOAD|" + fileName + "|" + fileSize);

                        // Tạm ngắt luồng đọc chữ ngầm để dành trọn Stream truyền Byte dữ liệu File lớn
                        _isConnected = false;

                        string serverSignal = _reader.ReadLine();
                        if (serverSignal == "READY_TO_RECEIVE")
                        {
                            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                            {
                                byte[] buffer = new byte[8192];
                                int bytesRead;
                                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    _stream.Write(buffer, 0, bytesRead);
                                }
                            }

                            string result = _reader.ReadLine();
                            if (result != null && result.StartsWith("UPLOAD_SUCCESS"))
                            {
                                MessageBox.Show("Upload file thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                _writer.WriteLine("GET_FILES"); // Đòi lại danh sách file mới
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi upload: " + ex.Message, "Lỗi");
                    }
                    finally
                    {
                        // Kích hoạt hoạt động lại cho luồng đọc tin nhắn chữ ngầm
                        _isConnected = true;
                        _listenThread = new Thread(ListenToServer) { IsBackground = true };
                        _listenThread.Start();
                    }
                }
            }
        }

        private void ExecuteDownload()
        {
            if (_client == null || !_client.Connected || lvwFiles.SelectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng kết nối Server và chọn file cần tải!", "Nhắc nhở");
                return;
            }

            string fileName = lvwFiles.SelectedItems[0].Text;

            using (SaveFileDialog sfd = new SaveFileDialog { FileName = fileName })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _writer.WriteLine("DOWNLOAD|" + fileName);
                        _isConnected = false;

                        string response = _reader.ReadLine();
                        if (response != null && response.StartsWith("START_DOWNLOAD"))
                        {
                            string[] parts = response.Split('|');
                            long fileSize = Convert.ToInt64(parts[2]);

                            using (FileStream fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write))
                            {
                                byte[] buffer = new byte[8192];
                                long totalRead = 0;
                                int bytesRead;

                                while (totalRead < fileSize && (bytesRead = _stream.Read(buffer, 0, (int)Math.Min(buffer.Length, fileSize - totalRead))) > 0)
                                {
                                    fs.Write(buffer, 0, bytesRead);
                                    totalRead += bytesRead;
                                }
                            }
                            MessageBox.Show("Tải file thành công về máy!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            lblBottomInfo.Text = "Đã tải xong file: " + fileName;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi tải file: " + ex.Message, "Lỗi");
                    }
                    finally
                    {
                        _isConnected = true;
                        _listenThread = new Thread(ListenToServer) { IsBackground = true };
                        _listenThread.Start();
                    }
                }
            }
        }

        // NÚT XÓA FILE
        private void button3_Click(object sender, EventArgs e)
        {
            if (lvwFiles.SelectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn file cần xóa!", "Nhắc nhở");
                return;
            }

            string fileName = lvwFiles.SelectedItems[0].Text;
            DialogResult result = MessageBox.Show("Bạn có chắc chắn muốn xóa file '" + fileName + "' không?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                _writer.WriteLine("DELETE|" + fileName);
            }
        }

        // NÚT CHIA SẺ FILE (SHARE)
        private void button4_Click(object sender, EventArgs e)
        {
            if (lvwFiles.SelectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một file để chia sẻ!", "Nhắc nhở");
                return;
            }

            string fileName = lvwFiles.SelectedItems[0].Text;
            string targetUser = Microsoft.VisualBasic.Interaction.InputBox("Nhập chính xác tên tài khoản bạn muốn share file này:", "Chia sẻ file", "");

            if (string.IsNullOrEmpty(targetUser.Trim())) return;

            if (targetUser.Trim().Equals(_user, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Bạn không thể tự chia sẻ file cho chính bản thân mình!", "Lỗi");
                return;
            }

            _writer.WriteLine("SHARE|" + fileName + "|" + targetUser.Trim());
        }

        private void CloseConnection()
        {
            _isConnected = false;
            if (_client != null) _client.Close();
            _client = null;
        }

        private void btnConnect_Click(object sender, EventArgs e) { ExecuteConnect(); }
        private void btnConnect_Click_1(object sender, EventArgs e) { ExecuteConnect(); }
        private void btnUpload_Click(object sender, EventArgs e) { ExecuteUpload(); }
        private void btnUpload_Click_1(object sender, EventArgs e) { ExecuteUpload(); }
        private void btnDownload_Click(object sender, EventArgs e) { ExecuteDownload(); }
        private void btnDownload_Click_1(object sender, EventArgs e) { ExecuteDownload(); }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) { CloseConnection(); }

        private void label1_Click(object sender, EventArgs e) { }
        private void label1_Click_1(object sender, EventArgs e) { }
        private void panel2_Paint(object sender, PaintEventArgs e) { }
        private void panel3_Paint(object sender, PaintEventArgs e) { }
        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e) { }
        private void lvwFiles_SelectedIndexChanged(object sender, EventArgs e) { }
    }
}