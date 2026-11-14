using Model;

namespace View.Services
{

    public class UserSession
    {
        // פרטי המשתמש המחובר. אפשר לקרוא אותם מכל דף, אבל לשנות אותם רק דרך הפעולות של המחלקה.
        // הסיסמה לא נשמרת כאן, כי אין בה צורך אחרי הכניסה
        public int UserId { get; private set; }
        public string Username { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public int RoleId { get; private set; }
        public string RoleName { get; private set; } = string.Empty;

        // האם יש משתמש מחובר. מזהה של משתמש אמיתי תמיד גדול מ-0
        public bool IsLoggedIn => UserId > 0;

        // אירוע שמודיע לכל מי שנרשם אליו שהמשתמש המחובר השתנה: מישהו נכנס או יצא.
        // למשל, הסרגל העליון נרשם לאירוע, וכך הוא מתעדכן מיד אחרי כניסה או יציאה.
        // סימן השאלה אומר שייתכן שאף אחד לא נרשם לאירוע
        public event Action? OnChange;

        // שמירת פרטי המשתמש אחרי כניסה מוצלחת
        public void Login(User user)
        {
            UserId = user.Id;
            Username = user.Username;
            Email = user.Email;
            RoleId = user.RoleId;
            RoleName = user.RoleName;

            // מפעילים את האירוע: כל רכיב שנרשם אליו, למשל הסרגל העליון, מקבל הודעה שמשתמש נכנס ומתעדכן.
            // קודם בודקים שמישהו נרשם לאירוע. אם אף אחד לא נרשם, האירוע ריק, והפעלה שלו הייתה זורקת חריגה
            if (OnChange != null)
            {
                OnChange.Invoke();
            }
        }

        // ניקוי פרטי המשתמש ביציאה מהחשבון
        public void Logout()
        {
            UserId = 0;
            Username = string.Empty;
            Email = string.Empty;
            RoleId = 0;
            RoleName = string.Empty;

            // מפעילים את האירוע, כדי שכל מי שנרשם אליו יידע שהמשתמש יצא. גם כאן בודקים קודם שמישהו נרשם
            if (OnChange != null)
            {
                OnChange.Invoke();
            }
        }
    }
}
