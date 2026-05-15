using System;
using System.Data;
using MySql.Data.MySqlClient;
using Mini_driver.Server.Models;

namespace Mini_driver.Server.Database
{
    public class DatabaseManager
    {
        private static DatabaseManager _instance;
        private readonly string connectionString = "Server=localhost;Database=mini_driverdb;Uid=root;Pwd=;";

        public static DatabaseManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DatabaseManager();
                }
                return _instance;
            }
        }

        private DatabaseManager() { }

        public User ValidateUser(string username, string password)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT UserID, Username, UserFolder FROM Users WHERE Username = @user AND Password = @pass";
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@user", username);
                        cmd.Parameters.AddWithValue("@pass", password);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int id = Convert.ToInt32(reader["UserID"]);
                                string name = reader["Username"].ToString();
                                string folder = reader["UserFolder"].ToString();
                                return new User(id, name, folder);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[DB ERROR] " + ex.Message);
            }
            return null;
        }

        public int CheckLogin(string username, string password)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT UserID FROM Users WHERE Username = @user AND Password = @pass";
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@user", username);
                        cmd.Parameters.AddWithValue("@pass", password);
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            return Convert.ToInt32(result);
                        }
                    }
                }
            }
            catch { }
            return -1;
        }

        public bool SaveFileInfo(string fileName, long fileSize, int ownerId, string localPath)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "INSERT INTO SharedFiles (FileName, FileSize, OwnerID, LocalPath, UploadDate) " +
                                 "VALUES (@name, @size, @owner, @path, NOW())";
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", fileName);
                        cmd.Parameters.AddWithValue("@size", fileSize);
                        cmd.Parameters.AddWithValue("@owner", ownerId);
                        cmd.Parameters.AddWithValue("@path", localPath);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch { return false; }
        }

        public bool GrantPermission(int ownerId, int sharedWithId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "INSERT INTO UserPermissions (OwnerID, SharedWithID) VALUES (@owner, @shared) " +
                                 "ON DUPLICATE KEY UPDATE OwnerID = OwnerID";
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@owner", ownerId);
                        cmd.Parameters.AddWithValue("@shared", sharedWithId);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch { return false; }
        }

        public DataTable GetSharedFiles(int myUserId)
        {
            DataTable dt = new DataTable();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT f.FileName, f.FileSize, u.Username AS OwnerName, f.LocalPath 
                                   FROM SharedFiles f
                                   JOIN UserPermissions p ON f.OwnerID = p.OwnerID
                                   JOIN Users u ON f.OwnerID = u.UserID
                                   WHERE p.SharedWithID = @myId";
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@myId", myUserId);
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                    }
                }
            }
            catch { }
            return dt;
        }
    }
}