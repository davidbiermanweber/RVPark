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

        public AccountController(AppDbContext db)
        {
            _db = db;
        }

        // GET: /Account/Register
        public IActionResult Register() => View();

        // POST: /Account/Register
        [HttpPost]
        public IActionResult Register(Employee employee)
        {
            if (ModelState.IsValid)
            {
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
            var user = _db.Employees.FirstOrDefault(u => u.Username == username && u.Password == password);

            if (user != null)
            {
                // Create the user's "Identity" (their claims/data saved in the cookie)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim("AccessLevel", user.AccessLevel.ToString()),
                    new Claim("Name", user.Name ?? "Employee")
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
            // Security Check: Kick them out if they aren't Level 3
            if (User.FindFirst("AccessLevel")?.Value != "3")
            {
                return Forbid();
            }

            var employees = _db.Employees.ToList();
            return View(employees);
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

        // POST: /Account/DeleteEmployee
        [HttpPost]
        [Authorize]
        public IActionResult DeleteEmployee(int id)
        {
            // Security Check: Kick them out if they aren't Level 3
            if (User.FindFirst("AccessLevel")?.Value != "3")
            {
                return Forbid();
            }

            var employee = _db.Employees.Find(id);
            
            // Prevent the admin from deleting themselves!
            var currentUsername = User.Identity?.Name;
            if (employee != null && employee.Username == currentUsername)
            {
                // Optional: You could pass an error message back to the view here
                return RedirectToAction("ManageEmployees"); 
            }

            if (employee != null)
            {
                _db.Employees.Remove(employee);
                _db.SaveChanges();
            }
            
            return RedirectToAction("ManageEmployees");
        }


    }
    
}