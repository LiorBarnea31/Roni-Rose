
using DAL;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace API.Controllers
{
    // Controller שאחראי לספק את הנתונים של לוח הבקרה למנהל המערכת.
    [ApiController]
    [Route("api/[controller]")]
    public class AdminDashboardController : ControllerBase
    {
        // אובייקטים שמאפשרים גישה לנתוני המשתמשים וההזמנות במסד הנתונים.
        private readonly UserDB userDB;
        private readonly OrderDB orderDB;
        private readonly ILogger<AdminDashboardController> logger;

        // Constructor שמקבל את החיבורים למסד הנתונים ואת מערכת תיעוד השגיאות.
        public AdminDashboardController(
            UserDB userDB,
            OrderDB orderDB,
            ILogger<AdminDashboardController> logger)
        {
            this.userDB = userDB;
            this.orderDB = orderDB;
            this.logger = logger;
        }

        // פעולה שמחזירה את כל הנתונים הדרושים להצגת לוח הבקרה של המנהל.
        [HttpGet]
        public async Task<ActionResult<AdminDashboardData>> GetDashboardData()
        {
            try
            {
                // יצירת אובייקט שמרכז את כל הנתונים שיוצגו בלוח הבקרה.
                AdminDashboardData data = new AdminDashboardData
                {
                    TotalUsers = await userDB.CountAllUsersAsync(),
                    MonthlyOrders = await orderDB.CountMonthlyOrdersAsync(),
                    MonthlyRevenue = await orderDB.GetMonthlyRevenueAsync(),
                    PendingOrders = await orderDB.CountPendingOrdersAsync(),
                    RecentOrders = await orderDB.GetRecentOrdersAsync(),
                    WeeklyActivity = await orderDB.GetWeeklyActivityAsync()
                };

                // החזרת הנתונים בהצלחה עם קוד HTTP 200.
                return Ok(data);
            }
            // במקרה שמתרחשת שגיאה במהלך שליפת הנתונים.
            catch (Exception ex)
            {
                // רישום פרטי השגיאה ביומן המערכת.
                logger.LogError(ex, "Error loading admin dashboard data");

                // החזרת קוד שגיאה 500 יחד עם הודעת השגיאה.
                return StatusCode(500, new
                {
                    message = ex.GetBaseException().Message
                });
            }
        }
    }
}