using System.ComponentModel.DataAnnotations;

namespace KEOPBackend.Models
{
    public class Notes
    {
        public int note_id { get; set; }
        [Required]
        public int user_id { get; set; }
        [Required]
        public string title { get; set; }
        [Required]
        public string value { get; set; }
        public DateTime? created_on { get; set; }
        public DateTime? modified_on { get; set; }
    }
}