
using System;
using System.Collections.Generic;
using System.Data.Common;
using Model;
using MySql.Data.MySqlClient;

namespace DAL
{
    public class UserDB
    {
        private readonly string connectionString;

        private const string SelectUserSql = @"
            SELECT
                UserID AS ID,
                FullName,
                Username,
                Email,
                PasswordHash AS Password,
                Phone,
                Role AS RoleName
            FROM Users";

        public UserDB(string connectionString)
        {
            this.connectionString = connectionString;
        }

        // הוספת משתמש חדש
        public async Task<int> AddUserAsync(User user, string roleName = "Customer")
        {
            string sql = @"
                INSERT INTO Users
                (FullName, Username, Email, PasswordHash, Phone, Role)
                VALUES
                (@FullName, @Username, @Email, @Password, @Phone, @Role);";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@FullName", user.FullName);
            command.Parameters.AddWithValue("@Username", user.Username);
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@Password", user.Password);
            command.Parameters.AddWithValue("@Phone", user.Phone ?? "");
            command.Parameters.AddWithValue(
                "@Role",
                string.IsNullOrWhiteSpace(roleName) ? "Customer" : roleName
            );

            await command.ExecuteNonQueryAsync();

            user.Id = (int)command.LastInsertedId;

            return user.Id;
        }

        // בדיקה האם שם משתמש קיים
        public async Task<bool> UsernameExistsAsync(string username)
        {
            string sql = @"
                SELECT EXISTS(
                    SELECT 1
                    FROM Users
                    WHERE Username = @Username
                );";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Username", username);

            return Convert.ToInt64(await command.ExecuteScalarAsync()) == 1;
        }

        // בדיקה האם אימייל קיים
        public async Task<bool> EmailExistsAsync(string email)
        {
            string sql = @"
                SELECT EXISTS(
                    SELECT 1
                    FROM Users
                    WHERE Email = @Email
                );";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Email", email);

            return Convert.ToInt64(await command.ExecuteScalarAsync()) == 1;
        }

        // התחברות משתמש
        public async Task<User?> CheckLoginAsync(string username, string password)
        {
            string sql = SelectUserSql + @"
                WHERE Username = @Username
                AND PasswordHash = @Password;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Username", username);
            command.Parameters.AddWithValue("@Password", password);

            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return ReadUser(reader);
        }

        // שליפת משתמש לפי מזהה
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            string sql = SelectUserSql + " WHERE UserID = @ID;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@ID", userId);

            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return ReadUser(reader);
        }

        // שליפת כל המשתמשים
        public async Task<List<User>> GetAllUsersAsync()
        {
            string sql = SelectUserSql + " ORDER BY Username;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            List<User> users = new List<User>();

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(ReadUser(reader));
            }

            return users;
        }

        // ספירת כל המשתמשים הרשומים
        public async Task<int> CountAllUsersAsync()
        {
            string sql = "SELECT COUNT(*) FROM Users;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        // ספירת משתמשים לפי תפקיד
        public async Task<Dictionary<string, int>> CountUsersByRoleAsync()
        {
            string sql = @"
                SELECT Role AS RoleName, COUNT(UserID) AS UserCount
                FROM Users
                GROUP BY Role
                ORDER BY Role;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            Dictionary<string, int> counts = new Dictionary<string, int>();

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                string roleName = reader.IsDBNull(reader.GetOrdinal("RoleName"))
                    ? "Customer"
                    : reader.GetString(reader.GetOrdinal("RoleName"));

                counts[roleName] = Convert.ToInt32(reader["UserCount"]);
            }

            return counts;
        }

        // עדכון תפקיד משתמש
        public async Task<bool> UpdateUserRoleAsync(int userId, string roleName)
        {
            string sql = @"
                UPDATE Users
                SET Role = @Role
                WHERE UserID = @UserID;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Role", roleName);
            command.Parameters.AddWithValue("@UserID", userId);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        // עדכון סיסמה
        public async Task<bool> UpdatePasswordAsync(int userId, string newPassword)
        {
            string sql = @"
                UPDATE Users
                SET PasswordHash = @Password
                WHERE UserID = @ID;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Password", newPassword);
            command.Parameters.AddWithValue("@ID", userId);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        // עדכון אימייל
        public async Task<bool> UpdateEmailAsync(int userId, string newEmail)
        {
            string sql = @"
                UPDATE Users
                SET Email = @Email
                WHERE UserID = @ID;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Email", newEmail);
            command.Parameters.AddWithValue("@ID", userId);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        // מחיקת משתמש
        public async Task<bool> DeleteUserAsync(int userId)
        {
            string sql = @"
                DELETE FROM Users
                WHERE UserID = @ID;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@ID", userId);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        // המרת נתונים מהמסד לאובייקט User
        private static User ReadUser(DbDataReader reader)
        {
            return new User(
                reader.GetInt32(reader.GetOrdinal("ID")),

                reader.IsDBNull(reader.GetOrdinal("FullName"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("FullName")),

                reader.GetString(reader.GetOrdinal("Username")),

                reader.GetString(reader.GetOrdinal("Email")),

                reader.IsDBNull(reader.GetOrdinal("Phone"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("Phone")),

                reader.IsDBNull(reader.GetOrdinal("Password"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("Password")),

                0,

                reader.IsDBNull(reader.GetOrdinal("RoleName"))
                    ? "Customer"
                    : reader.GetString(reader.GetOrdinal("RoleName")),

                DateTime.MinValue
            );
        }
    }
}

