using System;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Mini_driver.Client
{
    public partial class LoginForm : Form
    {
        public string LoggedInUsername { get; private set; }
        private readonly string _connString = "Server=localhost;Database=minidriverdb;Uid=root;Pwd=;";

        public LoginForm()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Vui lòng nhập tài khoản và mật khẩu!");
                return;
            }

            if (ValidateLogin(user, pass))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Tài khoản hoặc mật khẩu không chính xác!");
            }
        }

        private bool ValidateLogin(string user, string pass)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connString))
                {
                    conn.Open();
                    string sql = "SELECT Username FROM users WHERE Username = @user AND Password = @pass";

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@user", user);
                        cmd.Parameters.AddWithValue("@pass", pass);

                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            LoggedInUsername = result.ToString();
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối MySQL: " + ex.Message);
            }
            return false;
        }

        private void LoginForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult != DialogResult.OK)
            {
                Application.Exit();
            }
        }
    }
}