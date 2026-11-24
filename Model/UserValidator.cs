
using System;
using System.Collections.Generic;
using System.Net.Mail;

namespace Model
{
    // מחלקה שבודקת אם הנתונים של משתמש תקינים
    public static class UserValidator
    {
        // קבועים שמגדירים את אורך השם והסיסמה
        public const int UsernameMinLength = 3;
        public const int UsernameMaxLength = 20;
        public const int PasswordMinLength = 8;

        // בדיקת שם משתמש
        public static string ValidateUsername(string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return "יש להקליד שם משתמש";
            }

            string value = username.Trim();

            if (value.Length < UsernameMinLength ||
                value.Length > UsernameMaxLength)
            {
                return $"שם המשתמש צריך להכיל בין {UsernameMinLength} ל-{UsernameMaxLength} תווים";
            }

            foreach (char c in value)
            {
                bool isEnglishLetter =
                    (c >= 'a' && c <= 'z') ||
                    (c >= 'A' && c <= 'Z');

                bool isDigit = c >= '0' && c <= '9';

                if (!isEnglishLetter && !isDigit && c != '_')
                {
                    return "שם המשתמש יכול להכיל רק אותיות באנגלית, ספרות וקו תחתון";
                }
            }

            return "";
        }

        // בדיקת אימייל
        public static string ValidateEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return "יש להקליד אימייל";
            }

            string value = email.Trim();

            if (!MailAddress.TryCreate(value, out MailAddress? address))
            {
                return "כתובת האימייל לא תקינה";
            }

            if (address.Address != value)
            {
                return "כתובת האימייל לא תקינה";
            }

            int atIndex = value.IndexOf('@');

            if (value.IndexOf('.', atIndex) == -1)
            {
                return "כתובת האימייל לא תקינה";
            }

            return "";
        }

        // בדיקת סיסמה
        public static string ValidatePassword(string? password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return "יש להקליד סיסמה";
            }

            if (password.Length < PasswordMinLength)
            {
                return $"הסיסמה צריכה להכיל לפחות {PasswordMinLength} תווים";
            }

            bool hasLetter = false;
            bool hasDigit = false;

            foreach (char c in password)
            {
                if (char.IsLetter(c))
                {
                    hasLetter = true;
                }

                if (char.IsDigit(c))
                {
                    hasDigit = true;
                }
            }

            if (!hasLetter || !hasDigit)
            {
                return "הסיסמה צריכה להכיל לפחות אות אחת וספרה אחת";
            }

            return "";
        }

        // בדיקת משתמש שלם
        public static List<string> Validate(User user)
        {
            List<string> errors = new List<string>();

            string error = ValidateUsername(user.Username);
            if (error != "")
            {
                errors.Add(error);
            }

            error = ValidateEmail(user.Email);
            if (error != "")
            {
                errors.Add(error);
            }

            error = ValidatePassword(user.Password);
            if (error != "")
            {
                errors.Add(error);
            }

            return errors;
        }
    }
}

