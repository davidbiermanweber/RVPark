using System.ComponentModel.DataAnnotations;

namespace RvParkApp.Models
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }
        
        public string Name { get; set; }
        public string EmployeeId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int AccessLevel { get; set; }
    }
}