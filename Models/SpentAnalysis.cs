namespace KEOPBackend.Models
{
    public class SpentAnalysis
    {
        public int spent_id { get; set; }
        public int user_id { get; set; }
        public string spent_data { get; set; }
        public DateTime? created_on { get; set; }
        public DateTime? modified_on { get; set; }
    }
}
