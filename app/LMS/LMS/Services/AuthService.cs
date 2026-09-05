using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;

namespace LMS.Services
{
    public class AuthService
    {
        public static User? CurrentUser { get; private set; }

        public static bool IsLoggedIn => CurrentUser != null;

        public static bool IsAdmin => CurrentUser?.Role == UserRole.Administrator;

        public static bool HasPermission(string action)
        {
            if (CurrentUser == null) return false;
            if (CurrentUser.Role == UserRole.Administrator) return true;

            // Operator permissions
            if (CurrentUser.Role == UserRole.Operator)
            {
                if (action == "RestoreDatabase" || action == "ManageUsers" || action == "ViewAuditLogs" || action == "DeleteDatabase")
                {
                    return false;
                }
                return true;
            }

            // Viewer permissions
            if (CurrentUser.Role == UserRole.Viewer)
            {
                return action.StartsWith("View") || action == "PrintReport" || action == "PrintReceipt";
            }

            return false;
        }

        public static bool HasAnyAdmin()
        {
            using var db = new AppDbContext();
            return db.Users.Any(u => u.Role == UserRole.Administrator && u.IsActive);
        }

        public static (bool Success, string Message, User? User) Authenticate(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return (false, "Please enter both username and password.", null);
            }

            using var db = new AppDbContext();
            var user = db.Users.FirstOrDefault(u => u.Username.ToLower() == username.Trim().ToLower());

            if (user == null)
            {
                AuditService.Log("Failed Login", "User", username, $"Attempted login for non-existent username '{username}'.");
                return (false, "Invalid username or password.", null);
            }

            if (!user.IsActive)
            {
                AuditService.Log("Blocked Login", "User", user.Id.ToString(), $"Inactive account attempted login: {username}");
                return (false, "This user account has been disabled. Please contact the administrator.", null);
            }

            // Check lockout
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now)
            {
                var remainingMinutes = Math.Ceiling((user.LockoutEnd.Value - DateTime.Now).TotalMinutes);
                AuditService.Log("Locked Login", "User", user.Id.ToString(), $"Login attempt on locked account: {username}");
                return (false, $"Account is temporarily locked due to multiple failed attempts. Try again in {remainingMinutes} minutes.", null);
            }

            bool passwordValid = VerifyPassword(password, user.PasswordHash, user.PasswordSalt);

            if (!passwordValid)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.Now.AddMinutes(15);
                    user.FailedLoginAttempts = 0;
                    db.SaveChanges();
                    AuditService.Log("Account Locked", "User", user.Id.ToString(), $"Account locked after 5 failed attempts: {username}");
                    return (false, "Account locked for 15 minutes due to 5 consecutive failed login attempts.", null);
                }

                db.SaveChanges();
                AuditService.Log("Failed Login", "User", user.Id.ToString(), $"Incorrect password attempt for {username} (Attempt {user.FailedLoginAttempts}/5)");
                return (false, $"Invalid username or password. ({5 - user.FailedLoginAttempts} attempts remaining)", null);
            }

            // Successful login
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            user.LastLoginAt = DateTime.Now;
            db.SaveChanges();

            CurrentUser = user;
            AuditService.Log("User Login", "User", user.Id.ToString(), $"Successful login for user '{user.Username}' ({user.Role})");

            return (true, "Login successful.", user);
        }

        public static void Logout()
        {
            if (CurrentUser != null)
            {
                AuditService.Log("User Logout", "User", CurrentUser.Id.ToString(), $"User '{CurrentUser.Username}' logged out.");
                CurrentUser = null;
            }
        }

        public static (bool Success, string Message) CreateUser(string username, string password, string fullName, UserRole role)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Trim().Length < 3)
            {
                return (false, "Username must be at least 3 characters long.");
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
            {
                return (false, "Password must be at least 4 characters long.");
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                return (false, "Full name is required.");
            }

            using var db = new AppDbContext();
            string trimmedUsername = username.Trim();

            if (db.Users.Any(u => u.Username.ToLower() == trimmedUsername.ToLower()))
            {
                return (false, $"Username '{trimmedUsername}' already exists.");
            }

            var (hash, salt) = HashPassword(password);

            var user = new User
            {
                Username = trimmedUsername,
                FullName = fullName.Trim(),
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.Users.Add(user);
            db.SaveChanges();

            AuditService.Log("Create User", "User", user.Id.ToString(), $"Created new user '{user.Username}' with role '{role}'.");
            return (true, "User created successfully.");
        }

        public static (bool Success, string Message) UpdateUser(int userId, string fullName, UserRole role, bool isActive)
        {
            using var db = new AppDbContext();
            var user = db.Users.Find(userId);
            if (user == null) return (false, "User not found.");

            // Prevent deactivating the last active administrator
            if (!isActive && user.Role == UserRole.Administrator)
            {
                int activeAdminCount = db.Users.Count(u => u.Role == UserRole.Administrator && u.IsActive && u.Id != userId);
                if (activeAdminCount == 0)
                {
                    return (false, "Cannot deactivate the only active Administrator account.");
                }
            }

            user.FullName = fullName.Trim();
            user.Role = role;
            user.IsActive = isActive;
            db.SaveChanges();

            AuditService.Log("Update User", "User", user.Id.ToString(), $"Updated user '{user.Username}' (Role: {role}, Active: {isActive})");
            return (true, "User updated successfully.");
        }

        public static (bool Success, string Message) ChangePassword(int userId, string currentPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
            {
                return (false, "New password must be at least 4 characters long.");
            }

            using var db = new AppDbContext();
            var user = db.Users.Find(userId);
            if (user == null) return (false, "User not found.");

            if (!VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt))
            {
                return (false, "Current password is incorrect.");
            }

            var (hash, salt) = HashPassword(newPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            db.SaveChanges();

            AuditService.Log("Change Password", "User", user.Id.ToString(), $"Password changed for user '{user.Username}'.");
            return (true, "Password changed successfully.");
        }

        public static (bool Success, string Message) ResetPassword(int userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
            {
                return (false, "Password must be at least 4 characters long.");
            }

            using var db = new AppDbContext();
            var user = db.Users.Find(userId);
            if (user == null) return (false, "User not found.");

            var (hash, salt) = HashPassword(newPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            db.SaveChanges();

            AuditService.Log("Reset Password", "User", user.Id.ToString(), $"Password reset for user '{user.Username}' by admin.");
            return (true, "Password reset successfully.");
        }

        public static (string Hash, string Salt) HashPassword(string password)
        {
            byte[] saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }

            string salt = Convert.ToBase64String(saltBytes);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100000, HashAlgorithmName.SHA256);
            byte[] hashBytes = pbkdf2.GetBytes(32);
            string hash = Convert.ToBase64String(hashBytes);

            return (hash, salt);
        }

        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            try
            {
                byte[] saltBytes = Convert.FromBase64String(storedSalt);
                byte[] expectedHashBytes = Convert.FromBase64String(storedHash);

                using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100000, HashAlgorithmName.SHA256);
                byte[] actualHashBytes = pbkdf2.GetBytes(32);

                return CryptographicOperations.FixedTimeEquals(actualHashBytes, expectedHashBytes);
            }
            catch
            {
                return false;
            }
        }
    }
}
