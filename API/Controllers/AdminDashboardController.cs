
using DAL;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly UserDB userDB;
        private readonly OrderDB orderDB;
        private readonly ILogger<AdminDashboardController> logger;

        public AdminDashboardController(
            UserDB userDB,
            OrderDB orderDB,
            ILogger<AdminDashboardController> logger)
        {
            this.userDB = userDB;
            this.orderDB = orderDB;
            this.logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<AdminDashboardData>> GetDashboardData()
        {
            try
            {
                AdminDashboardData data = new AdminDashboardData
                {
                    TotalUsers = await userDB.CountAllUsersAsync(),
                    MonthlyOrders = await orderDB.CountMonthlyOrdersAsync(),
                    MonthlyRevenue = await orderDB.GetMonthlyRevenueAsync(),
                    PendingOrders = await orderDB.CountPendingOrdersAsync(),
                    RecentOrders = await orderDB.GetRecentOrdersAsync(),
                    WeeklyActivity = await orderDB.GetWeeklyActivityAsync()
                };

                return Ok(data);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading admin dashboard data");

                return StatusCode(500, new
                {
                    message = ex.GetBaseException().Message
                });
            }
        }
    }
}
