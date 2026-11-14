using Model;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace DAL
{
    public class OrderDB
    {
        private readonly string connectionString;

        public OrderDB(string connectionString)
        {
            this.connectionString = connectionString;
        }

        // שליפת נתוני ההזמנות החודשיות
        public async Task<int> CountMonthlyOrdersAsync()
        {
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

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        // חישוב ההכנסות החודשיות
        public async Task<decimal> GetMonthlyRevenueAsync()
        {
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

            return Convert.ToDecimal(await command.ExecuteScalarAsync());
        }

        // ספירת הזמנות שממתינות לטיפול
        public async Task<int> CountPendingOrdersAsync()
        {
            string sql = @"
                SELECT COUNT(*)
                FROM Orders
                WHERE Status = 'Pending';";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        // שליפת ארבע ההזמנות האחרונות כולל שם הלקוח
        public async Task<List<AdminRecentOrder>> GetRecentOrdersAsync()
        {
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

            return orders;
        }

        // שליפת מספר ההזמנות בכל אחד משבעת הימים האחרונים
        public async Task<List<AdminWeeklyActivity>> GetWeeklyActivityAsync()
        {
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

            return activity;
        }
    }
}