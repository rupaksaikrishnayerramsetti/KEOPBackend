using System.ComponentModel.DataAnnotations;

namespace KEOPBackend.Models
{
    public class Users
    {
        public int user_id { get; set; }
        [Required]
        public string user_name { get; set; }
        [Required]
        public string email { get; set; }
        [Required]
        public string gender { get; set; }
        [Required]
        public string occupation { get; set; }
        [Required]
        public string phone_number { get; set; }
        [Required]
        public int salary { get; set; } 
        public string? password_digest { get; set; }
        public DateTime? created_on { get; set; }
        public DateTime? modified_on { get; set; }
    }
}
