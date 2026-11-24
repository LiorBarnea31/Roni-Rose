
using Model;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace DAL
{
    // מחלקה שאחראית על שליפת נתוני ההזמנות ממסד הנתונים לצורך ניהול המערכת.
    public class OrderDB
    {
        // שמירת מחרוזת החיבור למסד הנתונים.
        private readonly string connectionString;

        // הדפסת הודעות לוג למסוף.
        private static void Log(string message)
        {
            Console.WriteLine($"[ORDER DB] {DateTime.Now:HH:mm:ss} - {message}");
        }

        // Constructor שמקבל את מחרוזת החיבור ושומר אותה.
        public OrderDB(string connectionString)
        {
            this.connectionString = connectionString;
        }

        // ספירת מספר ההזמנות שבוצעו בחודש הנוכחי.
        public async Task<int> CountMonthlyOrdersAsync()
        {
            Log("Counting monthly orders...");

            string sql = @"
                SELECT COUNT(*)
                FROM Orders
                WHERE OrderDate >= @StartDate
                AND OrderDate < @EndDate;";

            DateTime startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTime endDate = startDate.AddMonths(1);

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);

            int count = Convert.ToInt32(await command.ExecuteScalarAsync());

            Log($"Monthly orders count completed. Total orders: {count}");

            return count;
        }

        // חישוב סך ההכנסות מכל ההזמנות בחודש הנוכחי.
        public async Task<decimal> GetMonthlyRevenueAsync()
        {
            Log("Calculating monthly revenue...");

            string sql = @"
                SELECT COALESCE(SUM(TotalPrice), 0)
                FROM Orders
                WHERE OrderDate >= @StartDate
                AND OrderDate < @EndDate;";

            DateTime startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTime endDate = startDate.AddMonths(1);

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);

            decimal revenue = Convert.ToDecimal(await command.ExecuteScalarAsync());

            Log($"Monthly revenue calculation completed. Revenue: {revenue}");

            return revenue;
        }

        // ספירת כל ההזמנות שהסטטוס שלהן הוא Pending.
        public async Task<int> CountPendingOrdersAsync()
        {
            Log("Counting pending orders...");

            string sql = @"
                SELECT COUNT(*)
                FROM Orders
                WHERE Status = 'Pending';";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            int count = Convert.ToInt32(await command.ExecuteScalarAsync());

            Log($"Pending orders count completed. Total pending orders: {count}");

            return count;
        }

        // שליפת ארבע ההזמנות האחרונות יחד עם שם הלקוח שביצע כל הזמנה.
        public async Task<List<AdminRecentOrder>> GetRecentOrdersAsync()
        {
            Log("Retrieving the four most recent orders...");

            string sql = @"
                SELECT
                    Orders.OrderID,
                    Users.FullName,
                    Orders.OrderDate,
                    Orders.TotalPrice,
                    Orders.Status
                FROM Orders
                INNER JOIN Users ON Orders.UserID = Users.UserID
                ORDER BY Orders.OrderDate DESC
                LIMIT 4;";

            List<AdminRecentOrder> orders = new List<AdminRecentOrder>();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                AdminRecentOrder order = new AdminRecentOrder
                {
                    OrderID = reader.GetInt32("OrderID"),
                    FullName = reader.GetString("FullName"),
                    OrderDate = reader.IsDBNull(reader.GetOrdinal("OrderDate"))
                        ? DateTime.MinValue
                        : reader.GetDateTime("OrderDate"),
                    TotalPrice = reader.GetDecimal("TotalPrice"),
                    Status = reader.IsDBNull(reader.GetOrdinal("Status"))
                        ? string.Empty
                        : reader.GetString("Status")
                };

                orders.Add(order);
            }

            Log($"Recent orders retrieved successfully. Total orders: {orders.Count}");

            return orders;
        }

        // שליפת מספר ההזמנות שבוצעו בכל אחד משבעת הימים האחרונים.
        public async Task<List<AdminWeeklyActivity>> GetWeeklyActivityAsync()
        {
            Log("Retrieving weekly order activity...");

            string sql = @"
                SELECT
                    DAYOFWEEK(OrderDate) AS DayNumber,
                    COUNT(*) AS OrderCount
                FROM Orders
                WHERE OrderDate >= @StartDate
                AND OrderDate < @EndDate
                GROUP BY DAYOFWEEK(OrderDate);";

            DateTime today = DateTime.Today;
            DateTime startDate = today.AddDays(-6);
            DateTime endDate = today.AddDays(1);

            Dictionary<int, int> dailyCounts = new Dictionary<int, int>();

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@StartDate", startDate);
            command.Parameters.AddWithValue("@EndDate", endDate);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int dayNumber = reader.GetInt32("DayNumber");
                int count = reader.GetInt32("OrderCount");

                dailyCounts[dayNumber] = count;
            }

            List<AdminWeeklyActivity> activity = new List<AdminWeeklyActivity>();

            for (int i = 0; i < 7; i++)
            {
                DateTime date = startDate.AddDays(i);

                // MySQL DAYOFWEEK: ראשון = 1, שבת = 7
                int dayNumber = (int)date.DayOfWeek + 1;

                string[] dayNames =
                {
                    "א׳", "ב׳", "ג׳", "ד׳", "ה׳", "ו׳", "ש׳"
                };

                int count = dailyCounts.ContainsKey(dayNumber)
                    ? dailyCounts[dayNumber]
                    : 0;

                activity.Add(new AdminWeeklyActivity
                {
                    DayName = dayNames[(int)date.DayOfWeek],
                    OrderCount = count
                });
            }

            Log("Weekly activity retrieved successfully.");

            foreach (var day in activity)
            {
                Log($"Day: {day.DayName}, Orders: {day.OrderCount}");
            }

            return activity;
        }
    }
}
