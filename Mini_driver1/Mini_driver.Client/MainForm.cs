using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
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
        }

        private void ExecuteConnect()
        {
            try
            {
                lblBottomInfo.Text = "Đang kết nối đến Server...";

                _client = new TcpClient(txtIPServer.Text, 8888);
                _stream = _client.GetStream();

                _writer = new StreamWriter(_stream, Encoding.UTF8);
                _writer.AutoFlush = true;
                _reader = new StreamReader(_stream, Encoding.UTF8);

                _writer.WriteLine("CONNECT|" + _user);

                string response = _reader.ReadLine();

                if (response != null && response.StartsWith("CONNECT_SUCCESS"))
                {
                    lblStatus.Text = "🟢 Connected";
                    lblStatus.ForeColor = Color.Green;
                    lblBottomInfo.Text = "Đã kết nối tới Server: " + txtIPServer.Text;
                    MessageBox.Show("Kết nối Server thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // --- ĐOẠN THÊM MỚI: YÊU CẦU SERVER GỬI DANH SÁCH FILE ---
                    _writer.WriteLine("GET_FILES");
                    string fileListResponse = _reader.ReadLine();

                    if (fileListResponse != null && fileListResponse.StartsWith("FILE_LIST"))
                    {
                        lvwFiles.Items.Clear(); // Xóa trắng danh sách cũ trên giao diện
                        string[] parts = fileListResponse.Split('|');

                        // Chạy từ index 1 vì index 0 là chữ "FILE_LIST"
                        for (int i = 1; i < parts.Length; i++)
                        {
                            if (!string.IsNullOrEmpty(parts[i]))
                            {
                                lvwFiles.Items.Add(new ListViewItem(parts[i]));
                            }
                        }
                    }
                }
                else
                {
                    lblStatus.Text = "🔴 Kết nối lỗi";
                    lblStatus.ForeColor = Color.Red;
                    MessageBox.Show("Server từ chối kết nối!", "Thông báo lỗi");

                    if (_client != null) _client.Close();
                    _client = null;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "🔴 Ngắt kết nối";
                lblStatus.ForeColor = Color.Red;
                MessageBox.Show("Không thể kết nối đến Server: " + ex.Message, "Lỗi");
            }
        }

        private void ExecuteUpload()
        {
            if (_client == null || !_client.Connected)
            {
                MessageBox.Show("Vui lòng bấm nút KẾT NỐI Server trước!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string filePath = ofd.FileName;
                        string fileName = ofd.SafeFileName;
                        long fileSize = new FileInfo(filePath).Length;

                        lblBottomInfo.Text = "Đang gửi yêu cầu upload: " + fileName;

                        _writer.WriteLine("UPLOAD|" + fileName + "|" + fileSize);

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
                                lblBottomInfo.Text = "Tải lên thành công: " + fileName;
                                MessageBox.Show("Upload file thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                lvwFiles.Items.Add(new ListViewItem(fileName));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi trong quá trình truyền file: " + ex.Message, "Lỗi");
                    }
                }
            }
        }

        private void ExecuteDownload()
        {
            if (_client == null || !_client.Connected)
            {
                MessageBox.Show("Vui lòng kết nối Server trước!", "Nhắc nhở");
                return;
            }

            if (lvwFiles.SelectedItems.Count > 0)
            {
                string fileName = lvwFiles.SelectedItems[0].Text;

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.FileName = fileName;
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            _writer.WriteLine("DOWNLOAD|" + fileName);

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

                                lblBottomInfo.Text = "Đã tải xong file: " + fileName;
                                MessageBox.Show("Tải file thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                MessageBox.Show("Server báo lỗi hoặc file không tồn tại!", "Lỗi");
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Lỗi khi tải file: " + ex.Message, "Lỗi");
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một file trong danh sách để tải!", "Nhắc nhở");
            }
        }

        private void btnConnect_Click(object sender, EventArgs e) { ExecuteConnect(); }
        private void btnConnect_Click_1(object sender, EventArgs e) { ExecuteConnect(); }
        private void btnUpload_Click(object sender, EventArgs e) { ExecuteUpload(); }
        private void btnUpload_Click_1(object sender, EventArgs e) { ExecuteUpload(); }
        private void btnDownload_Click(object sender, EventArgs e) { ExecuteDownload(); }
        private void btnDownload_Click_1(object sender, EventArgs e) { ExecuteDownload(); }

        private void label1_Click(object sender, EventArgs e) { }
        private void label1_Click_1(object sender, EventArgs e) { }
        private void panel2_Paint(object sender, PaintEventArgs e) { }
        private void panel3_Paint(object sender, PaintEventArgs e) { }
        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e) { }
        private void lvwFiles_SelectedIndexChanged(object sender, EventArgs e) { }

        private void button3_Click(object sender, EventArgs e)
        {
            if (lvwFiles.SelectedItems.Count > 0) lblBottomInfo.Text = "Đã gửi yêu cầu xóa file.";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (lvwFiles.SelectedItems.Count > 0)
            {
                string fileName = lvwFiles.SelectedItems[0].Text;
                string targetUser = Microsoft.VisualBasic.Interaction.InputBox("Nhập tên tài khoản bạn muốn chia sẻ file này:", "Chia sẻ file", "");

                if (!string.IsNullOrEmpty(targetUser))
                {
                    _writer.WriteLine("SHARE|" + fileName + "|" + targetUser.Trim());
                    string response = _reader.ReadLine();
                    if (response != null && response == "SHARE_SUCCESS")
                    {
                        MessageBox.Show("Đã chia sẻ file thành công cho " + targetUser, "Thông báo");
                    }
                    else
                    {
                        MessageBox.Show("Không thể chia sẻ!", "Lỗi");
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một file để chia sẻ!", "Nhắc nhở");
            }
        }
    }
}