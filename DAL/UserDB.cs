
using System;
using System.Collections.Generic;
using System.Data.Common;
using Model;
using MySql.Data.MySqlClient;

namespace DAL
{
    public class UserDB
    {
        // מחרוזת החיבור למסד הנתונים
        private readonly string connectionString;

        // שאילתת SQL בסיסית לשליפת פרטי משתמש.
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

        // הדפסת הודעות לוג למסוף
        private static void Log(string message)
        {
            Console.WriteLine($"[USER DB] {DateTime.Now:HH:mm:ss} - {message}");
        }

        // הבנאי מקבל את מחרוזת החיבור ושומר אותה לשימוש בפעולות המחלקה
        public UserDB(string connectionString)
        {
            this.connectionString = connectionString;
        }

        // הוספת משתמש חדש למסד הנתונים
        public async Task<int> AddUserAsync(User user, string roleName = "Customer")
        {
            Log("Starting user creation...");

            string sql = @"
                INSERT INTO Users
                (FullName, Username, Email, PasswordHash, Phone, Role)
                VALUES
                (@FullName, @Username, @Email, @Password, @Phone, @Role);";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            Log("Connected to database. Inserting new user...");

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

            Log($"User created successfully. User ID: {user.Id}, Role: {roleName}");

            return user.Id;
        }

        // בדיקה האם שם משתמש קיים כבר במסד הנתונים
        public async Task<bool> UsernameExistsAsync(string username)
        {
            Log("Checking if username exists...");

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

            bool exists = Convert.ToInt64(await command.ExecuteScalarAsync()) == 1;

            Log($"Username existence check completed. Exists: {exists}");

            return exists;
        }

        // בדיקה האם כתובת אימייל קיימת כבר במסד הנתונים
        public async Task<bool> EmailExistsAsync(string email)
        {
            Log("Checking if email exists...");

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

            bool exists = Convert.ToInt64(await command.ExecuteScalarAsync()) == 1;

            Log($"Email existence check completed. Exists: {exists}");

            return exists;
        }

        // בדיקת פרטי התחברות של משתמש
        public async Task<User?> CheckLoginAsync(string username, string password)
        {
            Log("Checking user login...");

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
            {
                Log("Login failed. User was not found or credentials do not match.");
                return null;
            }

            User user = ReadUser(reader);

            Log($"Login successful. User ID: {user.Id}");

            return user;
        }

        // שליפת משתמש מסוים לפי המזהה הייחודי שלו
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            Log($"Searching for user. User ID: {userId}");

            string sql = SelectUserSql + " WHERE UserID = @ID;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ID", userId);

            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                Log($"User not found. User ID: {userId}");
                return null;
            }

            User user = ReadUser(reader);

            Log($"User retrieved successfully. User ID: {userId}");

            return user;
        }

        // שליפת כל המשתמשים ממסד הנתונים
        public async Task<List<User>> GetAllUsersAsync()
        {
            Log("Retrieving all users...");

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

            Log($"All users retrieved successfully. Total users: {users.Count}");

            return users;
        }

        // ספירת מספר המשתמשים הכולל במסד הנתונים
        public async Task<int> CountAllUsersAsync()
        {
            Log("Counting all users...");

            string sql = "SELECT COUNT(*) FROM Users;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            int count = Convert.ToInt32(await command.ExecuteScalarAsync());

            Log($"User count completed. Total users: {count}");

            return count;
        }

        // ספירת המשתמשים לפי התפקיד שלהם
        public async Task<Dictionary<string, int>> CountUsersByRoleAsync()
        {
            Log("Counting users by role...");

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

            Log("User count by role completed.");

            foreach (var item in counts)
            {
                Log($"Role: {item.Key}, Users: {item.Value}");
            }

            return counts;
        }

        // עדכון התפקיד של משתמש מסוים
        public async Task<bool> UpdateUserRoleAsync(int userId, string roleName)
        {
            Log($"Updating user role. User ID: {userId}, New role: {roleName}");

            string sql = @"
                UPDATE Users
                SET Role = @Role
                WHERE UserID = @UserID;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Role", roleName);
            command.Parameters.AddWithValue("@UserID", userId);

            int affectedRows = await command.ExecuteNonQueryAsync();
            bool success = affectedRows > 0;

            Log($"User role update completed. User ID: {userId}, Success: {success}, Affected rows: {affectedRows}");

            return success;
        }

        // עדכון הסיסמה של משתמש מסוים
        public async Task<bool> UpdatePasswordAsync(int userId, string newPassword)
        {
            Log($"Updating password. User ID: {userId}");

            string sql = @"
                UPDATE Users
                SET PasswordHash = @Password
                WHERE UserID = @ID;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Password", newPassword);
            command.Parameters.AddWithValue("@ID", userId);

            int affectedRows = await command.ExecuteNonQueryAsync();
            bool success = affectedRows > 0;

            Log($"Password update completed. User ID: {userId}, Success: {success}");

            return success;
        }

        // עדכון כתובת האימייל של משתמש מסוים
        public async Task<bool> UpdateEmailAsync(int userId, string newEmail)
        {
            Log($"Updating email. User ID: {userId}");

            string sql = @"
                UPDATE Users
                SET Email = @Email
                WHERE UserID = @ID;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Email", newEmail);
            command.Parameters.AddWithValue("@ID", userId);

            int affectedRows = await command.ExecuteNonQueryAsync();
            bool success = affectedRows > 0;

            Log($"Email update completed. User ID: {userId}, Success: {success}");

            return success;
        }

        // מחיקת משתמש ממסד הנתונים לפי המזהה שלו
        public async Task<bool> DeleteUserAsync(int userId)
        {
            Log($"Deleting user. User ID: {userId}");

            string sql = @"
                DELETE FROM Users
                WHERE UserID = @ID;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@ID", userId);

            int affectedRows = await command.ExecuteNonQueryAsync();
            bool success = affectedRows > 0;

            Log($"User deletion completed. User ID: {userId}, Success: {success}");

            return success;
        }

        // המרת נתונים שהתקבלו ממסד הנתונים לאובייקט מסוג User
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
