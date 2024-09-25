namespace WC.Models.DTO.Response
{
    public class Counter
    {
        public int FileSize { get; set; } = 0;
        public int Duplicates { get; set; } = 0;
        public int Inserted { get; set; } = 0;
        public int Failed { get; set; } = 0;
    }
}
