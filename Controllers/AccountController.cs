using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using RvParkApp.Models;

namespace RvParkApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IPasswordService _passwords;

        public AccountController(AppDbContext db, IPasswordService passwords)
        {
            _db = db;
            _passwords = passwords;
        }

        // GET: /Account/Register
        public IActionResult Register() => View();

        // POST: /Account/Register
        [HttpPost]
        public IActionResult Register(Employee employee)
        {
            if (!string.IsNullOrWhiteSpace(employee.Name) &&
                !string.IsNullOrWhiteSpace(employee.EmployeeId) &&
                !string.IsNullOrWhiteSpace(employee.Username) &&
                !string.IsNullOrWhiteSpace(employee.Password))
            {
                employee.Password = _passwords.Hash(employee.Password);
                employee.AccessLevel = Math.Clamp(employee.AccessLevel, 1, 3);
                employee.IsLocked = false;
                _db.Employees.Add(employee);
                _db.SaveChanges();
                return RedirectToAction("Login");
            }

            return View(employee);
        }

        // GET: /Account/Login
        public IActionResult Login() => View();

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = _db.Employees.FirstOrDefault(u => u.Username == username);

            if (user != null && user.IsLocked)
            {
                ViewBag.Error = "This employee account has been locked.";
                return View();
            }

            // Verify against the (possibly legacy-plaintext) stored password. On success
            // with a legacy value, rehash it now so plaintext is retired on next login.
            if (user != null && _passwords.Verify(user.Password, password, out bool needsUpgrade))
            {
                if (needsUpgrade)
                {
                    user.Password = _passwords.Hash(password);
                    await _db.SaveChangesAsync();
                }

                // Create the user's "Identity" (their claims/data saved in the cookie)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim("AccessLevel", user.AccessLevel.ToString()),
                    new Claim("Name", user.Name ?? "Employee"),
                    new Claim("Role", "Employee")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Dashboard");
            }

            ViewBag.Error = "Invalid username or password";
            return View();
        }

        // GET: /Account/Dashboard
        [Authorize] // Requires the user to be logged in!
        public IActionResult Dashboard()
        {
            return View();
        }

        // POST: /Account/Logout
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // GET: /Account/ManageEmployees
        [Authorize]
        public IActionResult ManageEmployees()
        {
            if (User.FindFirst("AccessLevel")?.Value != "3")
            {
                return Forbid();
            }

            var employees = _db.Employees.OrderBy(e => e.Name).ToList();
            return View(employees);
        }

        // GET: /Account/CreateEmployee
        [Authorize]
        public IActionResult CreateEmployee()
        {
            if (User.FindFirst("AccessLevel")?.Value != "3") return Forbid();
            return View(new Employee());
        }

        // POST: /Account/CreateEmployee
        [HttpPost]
        [Authorize]
        public IActionResult CreateEmployee(Employee employee)
        {
            if (User.FindFirst("AccessLevel")?.Value != "3") return Forbid();

            if (string.IsNullOrWhiteSpace(employee.Name) ||
                string.IsNullOrWhiteSpace(employee.EmployeeId) ||
                string.IsNullOrWhiteSpace(employee.Username) ||
                string.IsNullOrWhiteSpace(employee.Password))
            {
                ModelState.AddModelError(string.Empty, "All employee fields are required.");
                return View(employee);
            }

            if (_db.Employees.Any(e => e.Username == employee.Username))
            {
                ModelState.AddModelError(nameof(employee.Username), "That username is already in use.");
                return View(employee);
            }

            employee.Password = _passwords.Hash(employee.Password);
            employee.AccessLevel = Math.Clamp(employee.AccessLevel, 1, 3);
            employee.IsLocked = false;
            _db.Employees.Add(employee);
            _db.SaveChanges();

            return RedirectToAction("ManageEmployees");
        }

        // GET: /Account/EditEmployee/{id}
        [Authorize]
        public IActionResult EditEmployee(int id)
        {
            if (User.FindFirst("AccessLevel")?.Value != "3") return Forbid();

            var employee = _db.Employees.Find(id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: /Account/EditEmployee
        [HttpPost]
        [Authorize]
        public IActionResult EditEmployee(int id, Employee employee, string? newPassword)
        {
            if (User.FindFirst("AccessLevel")?.Value != "3") return Forbid();

            var existingEmployee = _db.Employees.Find(id);
            if (existingEmployee == null) return NotFound();

            if (string.IsNullOrWhiteSpace(employee.Name) ||
                string.IsNullOrWhiteSpace(employee.EmployeeId) ||
                string.IsNullOrWhiteSpace(employee.Username))
            {
                ModelState.AddModelError(string.Empty, "Name, employee ID and username are required.");
                return View(existingEmployee);
            }

            if (_db.Employees.Any(e => e.Username == employee.Username && e.Id != id))
            {
                ModelState.AddModelError(nameof(employee.Username), "That username is already in use.");
                return View(existingEmployee);
            }

            existingEmployee.Name = employee.Name;
            existingEmployee.EmployeeId = employee.EmployeeId;
            existingEmployee.Username = employee.Username;
            existingEmployee.AccessLevel = Math.Clamp(employee.AccessLevel, 1, 3);

            if (existingEmployee.AccessLevel == 3)
            {
                employee.IsLocked = false;
            }

            existingEmployee.IsLocked = employee.IsLocked;

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                existingEmployee.Password = _passwords.Hash(newPassword);
            }

            _db.SaveChanges();
            return RedirectToAction("ManageEmployees");
        }

        // POST: /Account/DeleteEmployee/{id}
        [HttpPost]
        [Authorize]
        public IActionResult DeleteEmployee(int id)
        {
            if (User.FindFirst("AccessLevel")?.Value != "3") return Forbid();

            var employee = _db.Employees.Find(id);
            if (employee != null)
            {
                _db.Employees.Remove(employee);
                _db.SaveChanges();
            }

            return RedirectToAction("ManageEmployees");
        }

        // POST: /Account/ToggleLockEmployee/{id}
        [HttpPost]
        [Authorize]
        public IActionResult ToggleLockEmployee(int id)
        {
            if (User.FindFirst("AccessLevel")?.Value != "3") return Forbid();

            var employee = _db.Employees.Find(id);
            if (employee != null)
            {
                if (employee.AccessLevel == 3)
                {
                    TempData["ErrorMessage"] = "Administrators cannot be locked.";
                    return RedirectToAction("ManageEmployees");
                }

                employee.IsLocked = !employee.IsLocked;
                _db.SaveChanges();
            }

            return RedirectToAction("ManageEmployees");
        }

        // GET: /Account/EditAccessLevel/{id}
        [Authorize]
        public IActionResult EditAccessLevel(int id)
        {
            if (User.FindFirst("AccessLevel")?.Value != "3") return Forbid();

            var employee = _db.Employees.Find(id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: /Account/EditAccessLevel
        [HttpPost]
        [Authorize]
        public IActionResult EditAccessLevel(int id, int accessLevel)
        {
            if (User.FindFirst("AccessLevel")?.Value != "3") return Forbid();

            var employee = _db.Employees.Find(id);
            if (employee != null)
            {
                employee.AccessLevel = Math.Clamp(accessLevel, 1, 3);
                _db.SaveChanges();
            }

            return RedirectToAction("ManageEmployees");
        }
    }
}