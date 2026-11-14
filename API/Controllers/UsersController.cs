using DAL;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserDB userDB;

        public UsersController(UserDB userDB)
        {
            this.userDB = userDB;
        }

        // GET api/users
        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAll()
        {
            List<User> users = await userDB.GetAllUsersAsync();

            for (int i = 0; i < users.Count; i++)
            {
                users[i].Password = string.Empty;
            }

            return Ok(users);
        }

        // GET api/users/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<User>> GetById(int id)
        {
            User? user = await userDB.GetUserByIdAsync(id);

            if (user == null)
                return NotFound();

            user.Password = string.Empty;

            return Ok(user);
        }

        // GET api/users/exists/admin
        [HttpGet("exists/{username}")]
        public async Task<ActionResult<bool>> UsernameExists(string username)
        {
            return Ok(await userDB.UsernameExistsAsync(username));
        }

        // GET api/users/email-exists/admin@example.com
        [HttpGet("email-exists/{email}")]
        public async Task<ActionResult<bool>> EmailExists(string email)
        {
            return Ok(await userDB.EmailExistsAsync(email));
        }

        // GET api/users/count-by-role
        [HttpGet("count-by-role")]
        public async Task<ActionResult<Dictionary<string, int>>> CountByRole()
        {
            return Ok(await userDB.CountUsersByRoleAsync());
        }

        // POST api/users
        [HttpPost]
        public async Task<ActionResult<User>> Add(
            [FromBody] User user,
            [FromQuery] string roleName = "Customer")
        {
            if (user == null ||
                string.IsNullOrWhiteSpace(user.Username) ||
                string.IsNullOrWhiteSpace(user.Email) ||
                string.IsNullOrWhiteSpace(user.Password))
            {
                return BadRequest("יש למלא את כל השדות הנדרשים.");
            }

            user.Username = user.Username.Trim();
            user.Email = user.Email.Trim();

            if (await userDB.UsernameExistsAsync(user.Username))
                return Conflict("Username already exists.");

            if (await userDB.EmailExistsAsync(user.Email))
                return Conflict("Email already exists.");

            try
            {
                await userDB.AddUserAsync(user, roleName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            user.Password = string.Empty;

            return CreatedAtAction(
                nameof(GetById),
                new { id = user.Id },
                user
            );
        }

        // POST api/users/login
        [HttpPost("login")]
        public async Task<ActionResult<User>> Login([FromBody] User credentials)
        {
            if (credentials == null ||
                string.IsNullOrWhiteSpace(credentials.Username) ||
                string.IsNullOrWhiteSpace(credentials.Password))
            {
                return BadRequest("יש להזין שם משתמש וסיסמה.");
            }

            User? user = await userDB.CheckLoginAsync(
                credentials.Username.Trim(),
                credentials.Password
            );

            if (user == null)
                return Unauthorized();

            user.Password = string.Empty;

            return Ok(user);
        }

        // PUT api/users/5/role
        [HttpPut("{id:int}/role")]
        public async Task<IActionResult> UpdateRole(int id, [FromQuery] string roleName)
        {
            return await userDB.UpdateUserRoleAsync(id, roleName)
                ? NoContent()
                : NotFound();
        }

        // PUT api/users/5/password
        [HttpPut("{id:int}/password")]
        public async Task<IActionResult> UpdatePassword(int id, [FromBody] string newPassword)
        {
            return await userDB.UpdatePasswordAsync(id, newPassword)
                ? NoContent()
                : NotFound();
        }

        // PUT api/users/5/email
        [HttpPut("{id:int}/email")]
        public async Task<IActionResult> UpdateEmail(int id, [FromBody] string newEmail)
        {
            if (await userDB.EmailExistsAsync(newEmail))
                return Conflict("Email already exists.");

            return await userDB.UpdateEmailAsync(id, newEmail)
                ? NoContent()
                : NotFound();
        }

        // DELETE api/users/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            return await userDB.DeleteUserAsync(id)
                ? NoContent()
                : NotFound();
        }
    }
}

