using System;

namespace Mini_driver.Server.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string UserFolder { get; set; }

        public User(int userId, string username, string userFolder)
        {
            UserID = userId;
            Username = username;
            UserFolder = userFolder;
        }
    }
}