using System;
using System.Linq;
using LMS.Data;
using LMS.Models;
using LMS.Services;
using Xunit;

namespace LMS.Tests
{
    public class SecurityAndAuthTests : TestBase
    {
        [Fact]
        public void PasswordHashing_GeneratesUniqueSaltsAndVerifiesCorrectly()
        {
            string rawPass = "Secret@123";
            var (hash1, salt1) = AuthService.HashPassword(rawPass);
            var (hash2, salt2) = AuthService.HashPassword(rawPass);

            // Salts must be unique even for same password
            Assert.NotEqual(salt1, salt2);
            Assert.NotEqual(hash1, hash2);

            // Verification must succeed for correct password
            Assert.True(AuthService.VerifyPassword(rawPass, hash1, salt1));
            Assert.True(AuthService.VerifyPassword(rawPass, hash2, salt2));

            // Verification must fail for incorrect password
            Assert.False(AuthService.VerifyPassword("WrongPass", hash1, salt1));
        }

        [Fact]
        public void InactiveUser_IsBlockedFromLogin()
        {
            AuthService.CreateUser("testoperator", "pass123", "Test Operator", UserRole.Operator);

            using (var db = new AppDbContext())
            {
                var user = db.Users.First(u => u.Username == "testoperator");
                user.IsActive = false;
                db.SaveChanges();
            }

            var auth = AuthService.Authenticate("testoperator", "pass123");
            Assert.False(auth.Success);
            Assert.Contains("disabled", auth.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void MultipleFailedAttempts_LocksAccountAfter5Attempts()
        {
            AuthService.CreateUser("lockoutuser", "correctpass", "Lockout User", UserRole.Operator);

            // 4 failed attempts
            for (int i = 0; i < 4; i++)
            {
                var res = AuthService.Authenticate("lockoutuser", "wrongpass");
                Assert.False(res.Success);
            }

            // 5th failed attempt -> locks account
            var fifthAttempt = AuthService.Authenticate("lockoutuser", "wrongpass");
            Assert.False(fifthAttempt.Success);
            Assert.Contains("locked", fifthAttempt.Message, StringComparison.OrdinalIgnoreCase);

            // Subsequent attempt even with correct password is still locked
            var lockedAttempt = AuthService.Authenticate("lockoutuser", "correctpass");
            Assert.False(lockedAttempt.Success);
            Assert.Contains("locked", lockedAttempt.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void RolePermissions_EnforcesStrictAccessBoundaries()
        {
            // Admin has all permissions
            AuthService.CreateUser("adminuser", "adminpass", "Admin User", UserRole.Administrator);
            var adminAuth = AuthService.Authenticate("adminuser", "adminpass");
            Assert.True(adminAuth.Success);
            Assert.True(AuthService.HasPermission("RestoreDatabase"));
            Assert.True(AuthService.HasPermission("ManageUsers"));
            Assert.True(AuthService.HasPermission("ViewAuditLogs"));

            // Operator is blocked from restore and user management
            AuthService.CreateUser("opuser", "oppass", "Operator User", UserRole.Operator);
            var opAuth = AuthService.Authenticate("opuser", "oppass");
            Assert.True(opAuth.Success);
            Assert.False(AuthService.HasPermission("RestoreDatabase"));
            Assert.False(AuthService.HasPermission("ManageUsers"));
            Assert.False(AuthService.HasPermission("ViewAuditLogs"));
            Assert.True(AuthService.HasPermission("RecordPayment"));
            Assert.True(AuthService.HasPermission("AddTenant"));

            // Viewer is restricted to view actions only
            AuthService.CreateUser("viewuser", "viewpass", "Viewer User", UserRole.Viewer);
            var viewAuth = AuthService.Authenticate("viewuser", "viewpass");
            Assert.True(viewAuth.Success);
            Assert.True(AuthService.HasPermission("ViewLedger"));
            Assert.True(AuthService.HasPermission("PrintReport"));
            Assert.False(AuthService.HasPermission("RecordPayment"));
            Assert.False(AuthService.HasPermission("AddTenant"));
        }
    }
}
