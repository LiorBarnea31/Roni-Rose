using System;
using System.Collections.Generic;
using System.Text;

namespace Model
{
    public class User : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // 1. קונסטרוקטור ריק (חובה ל-EF / יצירת מופעים ריקים)
        public User() { }

        // 2. עבור הרשמה (ללא מזהה)
        public User(string username, string email, string password)
        {
            Username = username;
            Email = email;
            Password = password;
        }

        // 3. עבור הרשמה מלאה כולל שם וטלפון
        public User(string fullName, string username, string email, string phone, string password)
        {
            FullName = fullName;
            Username = username;
            Email = email;
            Phone = phone;
            Password = password;
        }

        // 4. שליפה ישנה מהמסד (7 פרמטרים)
        public User(int id, string username, string email, string password,
                    int roleId, string roleName, DateTime createdAt) : base(id)
        {
            Username = username;
            Email = email;
            Password = password;
            RoleId = roleId;
            RoleName = roleName;
            CreatedAt = createdAt;
        }

        // 5. שליפה מלאה מהמסד (9 פרמטרים) - כולל שם מלא וטלפון
        public User(int id, string fullName, string username, string email, string phone, string password,
                    int roleId, string roleName, DateTime createdAt) : base(id)
        {
            FullName = fullName;
            Username = username;
            Email = email;
            Phone = phone;
            Password = password;
            RoleId = roleId;
            RoleName = roleName;
            CreatedAt = createdAt;
        }

        public override string ToString() => string.IsNullOrEmpty(FullName) ? $"{Username} ({RoleName})" : $"{FullName} ({Username})";
    }
}