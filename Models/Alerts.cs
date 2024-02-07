﻿using System.ComponentModel.DataAnnotations;

namespace KEOPBackend.Models
{
    public class Alerts
    {
        public int alert_id { get; set; }
        [Required]
        public int user_id { get; set; }
        [Required]
        public string title { get; set; }
        [Required]
        public string date { get; set; }
        [Required]
        public string time { get; set; }
        public DateTime created_on { get; set; }
        public DateTime modified_on { get; set; }
    }
}
