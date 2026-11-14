using System;
using System.Collections.Generic;

namespace Model
{
    public class AdminDashboardData
    {
        public int TotalUsers { get; set; }
        public int MonthlyOrders { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int PendingOrders { get; set; }

        public List<AdminRecentOrder> RecentOrders { get; set; } = new List<AdminRecentOrder>();
        public List<AdminWeeklyActivity> WeeklyActivity { get; set; } = new List<AdminWeeklyActivity>();
    }

    public class AdminRecentOrder
    {
        public int OrderID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class AdminWeeklyActivity
    {
        public string DayName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
    }
}