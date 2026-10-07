using System.Data;
using MySql.Data.MySqlClient;

namespace DAL
{
    public static class DatabaseInitializer
    {
        private const string DatabaseName = "RoniRoseDB";

        /// <summary>
        /// פעולת האתחול הראשית - נקראת בעת עליית האפליקציה
        /// </summary>
        public static async Task InitializeAsync(string serverConnectionString)
        {
            // שלב א': יצירת בסיס הנתונים אם אינו קיים
            await CreateDatabaseAsync(serverConnectionString);

            // מחרוזת חיבור מלאה הכוללת את שם בסיס הנתונים
            string dbConnectionString = $"{serverConnectionString}Database={DatabaseName};";

            // שלב ב': יצירת הטבלאות
            await CreateTablesAsync(dbConnectionString);

            // שלב ג': הכנסת נתוני ברירת מחדל
            await SeedInitialDataAsync(dbConnectionString);
        }

        private static async Task CreateDatabaseAsync(string serverConnectionString)
        {
            string sql = $"CREATE DATABASE IF NOT EXISTS `{DatabaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";

            using var connection = new MySqlConnection(serverConnectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private static async Task CreateTablesAsync(string dbConnectionString)
        {
            // שאילתת DDL המגדירה את 9 הטבלאות של חנות הפרחים
            string createTablesSql = @"
                -- 1. טבלת משתמשים
                CREATE TABLE IF NOT EXISTS Users (
                    UserID INT AUTO_INCREMENT PRIMARY KEY,
                    FullName VARCHAR(100) NOT NULL,
                    Email VARCHAR(100) NOT NULL UNIQUE,
                    Password VARCHAR(255) NOT NULL,
                    Phone VARCHAR(20) NULL,
                    Role VARCHAR(20) DEFAULT 'Customer'
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                -- 2. טבלת קטגוריות
                CREATE TABLE IF NOT EXISTS Categories (
                    CategoryID INT AUTO_INCREMENT PRIMARY KEY,
                    CategoryName VARCHAR(50) NOT NULL UNIQUE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                -- 3. טבלת מוצרים מוכנים (כולל BLOB ו-URL)
                CREATE TABLE IF NOT EXISTS Products (
                    ProductID INT AUTO_INCREMENT PRIMARY KEY,
                    ProductName VARCHAR(100) NOT NULL,
                    Description TEXT NULL,
                    Price DECIMAL(10,2) NOT NULL,
                    ProductImageBlob LONGBLOB NULL,
                    ImageUrl VARCHAR(500) NULL,
                    CategoryID INT NOT NULL,
                    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID) ON DELETE RESTRICT ON UPDATE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                -- 4. טבלת פרחים בודדים (כולל BLOB ו-URL)
                CREATE TABLE IF NOT EXISTS FlowerItems (
                    FlowerItemID INT AUTO_INCREMENT PRIMARY KEY,
                    FlowerName VARCHAR(100) NOT NULL,
                    Color VARCHAR(50) NULL,
                    PricePerUnit DECIMAL(10,2) NOT NULL,
                    FlowerImageBlob LONGBLOB NULL,
                    ImageUrl VARCHAR(500) NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                -- 5. טבלת הזמנות
                CREATE TABLE IF NOT EXISTS Orders (
                    OrderID INT AUTO_INCREMENT PRIMARY KEY,
                    UserID INT NOT NULL,
                    OrderDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    TotalPrice DECIMAL(10,2) NOT NULL,
                    Status VARCHAR(50) DEFAULT 'Pending',
                    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE RESTRICT ON UPDATE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                -- 6. טבלת איחוד: מוצרים מוכנים בהזמנה
                CREATE TABLE IF NOT EXISTS OrderDetails (
                    OrderDetailID INT AUTO_INCREMENT PRIMARY KEY,
                    OrderID INT NOT NULL,
                    ProductID INT NOT NULL,
                    Quantity INT NOT NULL,
                    UnitPrice DECIMAL(10,2) NOT NULL,
                    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID) ON DELETE CASCADE ON UPDATE CASCADE,
                    FOREIGN KEY (ProductID) REFERENCES Products(ProductID) ON DELETE RESTRICT ON UPDATE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                -- 7. טבלת איחוד: פרחים בזר אישי בהזמנה
                CREATE TABLE IF NOT EXISTS CustomBouquetDetails (
                    CustomID INT AUTO_INCREMENT PRIMARY KEY,
                    OrderID INT NOT NULL,
                    FlowerItemID INT NOT NULL,
                    Quantity INT NOT NULL,
                    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID) ON DELETE CASCADE ON UPDATE CASCADE,
                    FOREIGN KEY (FlowerItemID) REFERENCES FlowerItems(FlowerItemID) ON DELETE RESTRICT ON UPDATE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                -- 8. טבלת שאלות ותשובות לצ'אט בוט
                CREATE TABLE IF NOT EXISTS BotFAQs (
                    FAQID INT AUTO_INCREMENT PRIMARY KEY,
                    Question VARCHAR(255) NOT NULL,
                    Answer TEXT NOT NULL,
                    Category VARCHAR(50) NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

                -- 9. טבלת הזמנות מיוחדות: עיצוב רכב לחתונה
                CREATE TABLE IF NOT EXISTS SpecialCarOrders (
                    SpecialOrderID INT AUTO_INCREMENT PRIMARY KEY,
                    UserID INT NOT NULL,
                    CarModel VARCHAR(100) NOT NULL,
                    EventDate DATETIME NOT NULL,
                    DesignStyle VARCHAR(100) NOT NULL,
                    SpecialRequests TEXT NULL,
                    TotalPrice DECIMAL(10,2) NOT NULL,
                    Status VARCHAR(50) DEFAULT 'Pending',
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE ON UPDATE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ";

            using var connection = new MySqlConnection(dbConnectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(createTablesSql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private static async Task SeedInitialDataAsync(string dbConnectionString)
        {
            using var connection = new MySqlConnection(dbConnectionString);
            await connection.OpenAsync();

            // בדיקה אם קיימות קטגוריות, אם לא - הכנסת נתוני פתיחה
            string checkSql = "SELECT COUNT(*) FROM Categories;";
            using var checkCmd = new MySqlCommand(checkSql, connection);
            long count = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());

            if (count == 0)
            {
                string insertCategoriesSql = @"
                    INSERT INTO Categories (CategoryName) VALUES 
                    ('זרי פרחים'),
                    ('סידורי פרחים'),
                    ('עציצים וצמחים'),
                    ('מתנות ומארזים');
                ";
                using var insertCmd = new MySqlCommand(insertCategoriesSql, connection);
                await insertCmd.ExecuteNonQueryAsync();
            }
        }
    }
}