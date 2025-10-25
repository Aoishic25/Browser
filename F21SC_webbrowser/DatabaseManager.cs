using F21SC_webbrowser;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;

namespace F21SC_webbrowser
{

    public class DatabaseManager
    {
        private string connString;
        public int? CurrentID { get; private set; } // Store current user ID after login
        public string CurrentUsername { get; private set; } // Store current username after login
        public DatabaseManager()
        {
            connString = ConfigurationManager.ConnectionStrings["BrowserDB"].ConnectionString;
        }

        #region User Authentication Methods
        // Register a new user
        public bool RegisterUser(string username, string password)
        {
            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();

                // Check if user exists
                var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE username=@username", conn);
                checkCmd.Parameters.AddWithValue("@username", username);
                long count = (long)checkCmd.ExecuteScalar();

                if (count > 0)
                    return false; // Username exists

                // Hash password
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

                // Insert new user
                var insertCmd = new MySqlCommand("INSERT INTO users (username, passwordHash) VALUES (@username, @password)", conn);
                insertCmd.Parameters.AddWithValue("@username", username);
                insertCmd.Parameters.AddWithValue("@password", hashedPassword);
                insertCmd.ExecuteNonQuery();

                return true;
            }
        }

        // Login an existing user
        public bool LoginUser(string username, string password)
        {
            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT id, passwordHash FROM users WHERE username=@username", conn);
                cmd.Parameters.AddWithValue("@username", username);
                using (var reader = cmd.ExecuteReader()) // Using reader to fetch data
                {
                    if (reader.Read())
                    {
                        string storedHash = reader.GetString("passwordHash");
                        if (BCrypt.Net.BCrypt.Verify(password, storedHash))
                        {
                            CurrentID = reader.GetInt32("id");
                            CurrentUsername = username; // Store current username
                            return true; // Login successful
                        }
                    }
                }
            }
            return false; // Login failed
        }

        // Logout current user
        public void LogoutUser()
        {
            CurrentID = null;
        }

        //Add homepage for current user
        public void SetHomepageForCurrentUser(string url)
        {
            if (CurrentID == null) return; // no user logged in

            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();
                string query = @"INSERT INTO homepage (username, url) VALUES (@username, @url) ON DUPLICATE KEY UPDATE url=@url;";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", CurrentUsername);
                    cmd.Parameters.AddWithValue("@url", url);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        //Get homepage for current user
        public string GetHomepageForCurrentUser()
        {
            if (CurrentID == null)
                return "https://www.hw.ac.uk/";

            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();
                string query = "SELECT url FROM homepage WHERE username=@username LIMIT 1";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username",CurrentUsername);
                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "https://www.hw.ac.uk/";
                }
            }
        }

        //storing the bookmakrs for the current user
        public void AddBookmarkForCurrentUser(string name, string url)
        {
            if (CurrentID == null) return;
            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();
                string query = "INSERT INTO bookmarks (name, url, username) VALUES (@name, @url, @username)";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@url", url);
                    cmd.Parameters.AddWithValue("@username", CurrentUsername);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        //retrieving bookmarks for the current user
        public List<(int id, string Name, string Url)> GetBookmarksForCurrentUser()
        {
            var list = new List<(int, string, string)>(); // (id, name, url)
            if (CurrentID == null) return list; // no user logged in

            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();
                string query = "SELECT id, name, url FROM bookmarks WHERE username=@username ORDER BY name";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", CurrentUsername);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add((reader.GetInt32("id"), reader.GetString("name"), reader.GetString("url")));
                        }
                    }
                }
            }
            return list; // return bookmarks list
        }

        //storing the history for the current user
        public void AddHistoryForCurrentUser(string title, string url)
        {
            if (CurrentID == null) return;

            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();
                string query = @"INSERT INTO history (title, url, visited_at, username) VALUES (@title, @url, NOW(), @username)";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@title", title);
                    cmd.Parameters.AddWithValue("@url", url);
                    cmd.Parameters.AddWithValue("@username",CurrentUsername);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        //retrieving history for the current user
        public List<(int id, string Title, string Url, DateTime VisitedAt)> GetHistoryForCurrentUser()
        {
            var list = new List<(int, string, string, DateTime)>();
            if (CurrentID == null) return list;

            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();
                string query = "SELECT id, title, url, visited_at FROM history WHERE username=@username ORDER BY visited_at DESC";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", CurrentUsername);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add((
                                reader.GetInt32("id"),
                                reader.GetString("title"),
                                reader.GetString("url"),
                                reader.GetDateTime("visited_at")
                            ));
                        }
                    }
                }
            }
            return list;
        }
        #endregion

        #region Homepage Methods
        public string GetHomePage()
        {
            try
            {
                using (var conn = new MySqlConnection(connString))
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT url FROM homepage LIMIT 1", conn);
                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "https://www.hw.ac.uk/";
                }
            }
            catch
            {
                return "https://www.hw.ac.uk/";
            }
        }

        public void SetHomePage(string url)
        {
            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();
                //REPLACE ensure id=1 exists
                var cmd = new MySqlCommand("REPLACE INTO homepage (id, url) VALUES (1, @url)", conn);
                cmd.Parameters.AddWithValue("@url", url);
                cmd.ExecuteNonQuery();
            }
        }
        #endregion

        #region Bookmarks Methods
        public void AddBookmark(string name,string url)
        {
            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();
                //Prevent duplicate bookmarks
                var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM bookmarks WHERE url=@url", conn);
                checkCmd.Parameters.AddWithValue("@url", url);
                long count = (long)checkCmd.ExecuteScalar();
                if (count == 0)
                {
                    var cmd = new MySqlCommand("INSERT INTO bookmarks (name, url) VALUES (@name, @url)", conn);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@url", url);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateBookmark(int id,string name,string url)
        {
            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();
                var cmd = new MySqlCommand("UPDATE bookmarks SET name=@name, url=@url WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@url", url);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteBookmark(int id)
        {
            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();
                var cmd = new MySqlCommand("DELETE FROM bookmarks WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public List<(int id,string Name,string Url)> GetBookmarks()
        {
            var list=new List<(int,string,string)>();
            using (var conn=new MySqlConnection(connString))
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT id,name,url FROM bookmarks ORDER BY name", conn);
                using (var reader=cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
                    }
                }
            }
            return list;
        }
        #endregion

        #region History methods
        public void AddHistory(string title, string url)
        {
            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();
                //checks last history item to eliminate consecutive duplicates
                var lastCmd = new MySqlCommand("SELECT url FROM history ORDER BY id DESC LIMIT 1", conn);
                var lastUrl = lastCmd.ExecuteScalar()?.ToString();
                if (lastUrl != url)
                {
                    var cmd = new MySqlCommand("INSERT INTO history (title,url,visited_at) VALUES (@title,@url,NOW())", conn);
                    cmd.Parameters.AddWithValue("@title", title);
                    cmd.Parameters.AddWithValue("@url", url);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<(int id,string Title,string Url,DateTime VisitedAt)> GetHistory()
        {
            var list = new List<(int, string, string, DateTime)>();
            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT id,title,url,visited_at FROM history ORDER BY visited_at DESC", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetDateTime(3)));
                    }
                }
            }
            return list;
        }

        public void ClearHistory()
        {
            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();
                var cmd = new MySqlCommand("DELETE FROM history", conn);
                cmd.ExecuteNonQuery();
            }
        }
        #endregion
    }
}