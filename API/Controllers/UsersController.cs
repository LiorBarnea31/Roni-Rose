
using DAL;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace API.Controllers
{
    // Controller שאחראי על ניהול המשתמשים במערכת.
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        // אובייקט שמאפשר לבצע פעולות על המשתמשים במסד הנתונים.
        private readonly UserDB userDB;

        // Constructor שמקבל את UserDB ומאתחל אותו.
        public UsersController(UserDB userDB)
        {
            this.userDB = userDB;
        }

        // שליפת כל המשתמשים ממסד הנתונים והסתרת הסיסמאות שלהם.
        // GET api/users
        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAll()
        {
            List<User> users = await userDB.GetAllUsersAsync();

            // מעבר על כל המשתמשים ומחיקת הסיסמה מהמידע שמוחזר.
            for (int i = 0; i < users.Count; i++)
            {
                users[i].Password = string.Empty;
            }

            return Ok(users);
        }

        // שליפת משתמש מסוים לפי המזהה שלו.
        // GET api/users/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<User>> GetById(int id)
        {
            User? user = await userDB.GetUserByIdAsync(id);

            // אם המשתמש לא נמצא, מחזירים שגיאה 404.
            if (user == null)
                return NotFound();

            // הסתרת הסיסמה לפני החזרת פרטי המשתמש.
            user.Password = string.Empty;

            return Ok(user);
        }

        // בדיקה האם שם משתמש מסוים כבר קיים במערכת.
        // GET api/users/exists/admin
        [HttpGet("exists/{username}")]
        public async Task<ActionResult<bool>> UsernameExists(string username)
        {
            return Ok(await userDB.UsernameExistsAsync(username));
        }

        // בדיקה האם כתובת אימייל מסוימת כבר קיימת במערכת.
        // GET api/users/email-exists/admin@example.com
        [HttpGet("email-exists/{email}")]
        public async Task<ActionResult<bool>> EmailExists(string email)
        {
            return Ok(await userDB.EmailExistsAsync(email));
        }

        // ספירת המשתמשים במערכת לפי התפקיד שלהם.
        // GET api/users/count-by-role
        [HttpGet("count-by-role")]
        public async Task<ActionResult<Dictionary<string, int>>> CountByRole()
        {
            return Ok(await userDB.CountUsersByRoleAsync());
        }

        // הוספת משתמש חדש למערכת.
        // POST api/users
        [HttpPost]
        public async Task<ActionResult<User>> Add(
            [FromBody] User user,
            [FromQuery] string roleName = "Customer")
        {
            // בדיקה שכל השדות הנדרשים מולאו.
            if (user == null ||
                string.IsNullOrWhiteSpace(user.Username) ||
                string.IsNullOrWhiteSpace(user.Email) ||
                string.IsNullOrWhiteSpace(user.Password))
            {
                return BadRequest("יש למלא את כל השדות הנדרשים.");
            }

            // בדיקת תקינות הנתונים באמצעות מחלקת UserValidator.
            List<string> errors = UserValidator.Validate(user);

            // אם נמצאו שגיאות, מחזירים קוד 400 עם רשימת השגיאות.
            if (errors.Count > 0)
            {
                Console.WriteLine($"[Register] Failed, invalid data: {user.Username}");

                return BadRequest(errors);
            }

            // הסרת רווחים מיותרים מתחילת וסוף שם המשתמש והאימייל.
            user.Username = user.Username.Trim();
            user.Email = user.Email.Trim();

            // בדיקה האם שם המשתמש כבר תפוס.
            if (await userDB.UsernameExistsAsync(user.Username))
                return Conflict("Username already exists.");

            // בדיקה האם כתובת האימייל כבר קיימת.
            if (await userDB.EmailExistsAsync(user.Email))
                return Conflict("Email already exists.");

            // ניסיון להוסיף את המשתמש למסד הנתונים עם התפקיד שהתקבל.
            try
            {
                await userDB.AddUserAsync(user, roleName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            // הסתרת הסיסמה לפני החזרת פרטי המשתמש.
            user.Password = string.Empty;

            // החזרת תשובה 201 שמציינת שהמשתמש נוצר בהצלחה.
            return CreatedAtAction(
                nameof(GetById),
                new { id = user.Id },
                user
            );
        }

        // התחברות משתמש למערכת באמצעות שם משתמש וסיסמה.
        // POST api/users/login
        [HttpPost("login")]
        public async Task<ActionResult<User>> Login([FromBody] User credentials)
        {
            // בדיקה שהתקבלו שם משתמש וסיסמה.
            if (credentials == null ||
                string.IsNullOrWhiteSpace(credentials.Username) ||
                string.IsNullOrWhiteSpace(credentials.Password))
            {
                return BadRequest("יש להזין שם משתמש וסיסמה.");
            }

            // בדיקת פרטי ההתחברות מול מסד הנתונים.
            User? user = await userDB.CheckLoginAsync(
                credentials.Username.Trim(),
                credentials.Password
            );

            // אם פרטי ההתחברות אינם נכונים, מחזירים שגיאה 401.
            if (user == null)
                return Unauthorized();

            // הסתרת הסיסמה לפני החזרת פרטי המשתמש.
            user.Password = string.Empty;

            return Ok(user);
        }

        // עדכון התפקיד של משתמש לפי המזהה שלו.
        // PUT api/users/5/role
        [HttpPut("{id:int}/role")]
        public async Task<IActionResult> UpdateRole(int id, [FromQuery] string roleName)
        {
            // אם העדכון הצליח מחזירים 204, אחרת מחזירים 404.
            return await userDB.UpdateUserRoleAsync(id, roleName)
                ? NoContent()
                : NotFound();
        }

        // שינוי הסיסמה של משתמש מסוים.
        // PUT api/users/5/password
        public async Task<IActionResult> UpdatePassword(int id, [FromBody] string newPassword)
        {
            // אם שינוי הסיסמה הצליח מחזירים 204, אחרת מחזירים 404.
            return await userDB.UpdatePasswordAsync(id, newPassword)
                ? NoContent()
                : NotFound();
        }

        // עדכון כתובת האימייל של משתמש.
        // PUT api/users/5/email
        [HttpPut("{id:int}/email")]
        public async Task<IActionResult> UpdateEmail(int id, [FromBody] string newEmail)
        {
            // בדיקה האם כתובת האימייל החדשה כבר קיימת אצל משתמש אחר.
            if (await userDB.EmailExistsAsync(newEmail))
                return Conflict("Email already exists.");

            // אם העדכון הצליח מחזירים 204, אחרת מחזירים 404.
            return await userDB.UpdateEmailAsync(id, newEmail)
                ? NoContent()
                : NotFound();
        }

        // מחיקת משתמש ממסד הנתונים לפי המזהה שלו.
        // DELETE api/users/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            // אם המחיקה הצליחה מחזירים 204, אחרת מחזירים 404.
            return await userDB.DeleteUserAsync(id)
                ? NoContent()
                : NotFound();
        }
    }
}
