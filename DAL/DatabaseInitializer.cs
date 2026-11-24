using System.Data;
using MySql.Data.MySqlClient;

namespace DAL
{
    // מחלקה סטטית שאחראית על יצירת מסד הנתונים, הטבלאות והכנסת נתוני התחלה.
    public static class DatabaseInitializer
    {
        // שמירת שם מסד הנתונים כקבוע כדי להשתמש בו בכל המחלקה.
        private const string DatabaseName = "RoniRoseDB";

        /// <summary>
        /// פעולת האתחול הראשית - נקראת בעת עליית האפליקציה
        /// </summary>
        // פעולה ראשית שמפעילה את שלושת שלבי האתחול לפי הסדר.
        public static async Task InitializeAsync(string serverConnectionString)
        {
            Console.WriteLine("[DB] Starting database initialization...");

            // שלב א': יצירת בסיס הנתונים אם אינו קיים
            await CreateDatabaseAsync(serverConnectionString);

            // מחרוזת חיבור מלאה הכוללת את שם בסיס הנתונים
            string dbConnectionString = $"{serverConnectionString}Database={DatabaseName};";

            // שלב ב': יצירת הטבלאות
            await CreateTablesAsync(dbConnectionString);

            // שלב ג': הכנסת נתוני ברירת מחדל
            await SeedInitialDataAsync(dbConnectionString);

            Console.WriteLine("[DB] Initialization finished, the server is ready for requests");
        }

        // יצירת מסד הנתונים אם הוא עדיין לא קיים.
        private static async Task CreateDatabaseAsync(string serverConnectionString)
        {
            // פתיחת חיבור לשרת MySQL וביצוע פקודת יצירת מסד הנתונים.
            using var connection = new MySqlConnection(serverConnectionString);

            Console.WriteLine("[DB] Connecting to the MySQL server...");
            await connection.OpenAsync();
            Console.WriteLine("[DB] Connected to the server");

            // בדיקה האם מסד הנתונים כבר קיים.
            string existsSql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = @Name;";
            using var existsCmd = new MySqlCommand(existsSql, connection);
            existsCmd.Parameters.AddWithValue("@Name", DatabaseName);

            long existing = Convert.ToInt64(await existsCmd.ExecuteScalarAsync());

            string sql = $"CREATE DATABASE IF NOT EXISTS `{DatabaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";

            using var command = new MySqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();

            if (existing > 0)
            {
                Console.WriteLine($"[DB] Database {DatabaseName} already exists");
            }
            else
            {
                Console.WriteLine($"[DB] Created new database: {DatabaseName}");
            }
        }

        // יצירת כל תשע הטבלאות הדרושות לפעילות חנות הפרחים.
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
                    IsActive BOOLEAN NOT NULL DEFAULT TRUE, 
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

            // פתיחת חיבור למסד הנתונים וביצוע פקודת יצירת כל הטבלאות.
            using var connection = new MySqlConnection(dbConnectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(createTablesSql, connection);
            await command.ExecuteNonQueryAsync();

            Console.WriteLine("[DB] All nine tables are ready");
        }

        // הכנסת נתוני התחלה למסד הנתונים במידת הצורך.
        private static async Task SeedInitialDataAsync(string dbConnectionString)
        {
            // פתיחת חיבור למסד הנתונים.
            using var connection = new MySqlConnection(dbConnectionString);
            await connection.OpenAsync();

            // בדיקה אם קיימות קטגוריות, אם לא - הכנסת נתוני פתיחה
            string checkSql = "SELECT COUNT(*) FROM Categories;";
            using var checkCmd = new MySqlCommand(checkSql, connection);
            long count = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());

            // אם אין קטגוריות במסד הנתונים, מכניסים שתי קטגוריות ברירת מחדל.
            if (count == 0)
            {
                string insertCategoriesSql = @"
                    INSERT INTO Categories (CategoryName) VALUES 
                    ('זרי פרחים'),
                    ('רכבי חתונה');
                ";

                using var insertCmd = new MySqlCommand(insertCategoriesSql, connection);
                await insertCmd.ExecuteNonQueryAsync();

                Console.WriteLine("[DB] Default categories added");
            }
            else
            {
                Console.WriteLine($"[DB] Categories table already has {count} categories, no seed categories added");
            }
        }
    }
}
