using Enums = WC.Models.Enums;

namespace WC.Models.DTO.Request
{
    public class UserPlanHistoryRequest
    {
        public required int UserId { get; set; }
        public Enums.Plans OldPlan { get; set; }
        public required Enums.Plans NewPlan { get; set;}
        public DateTime DateTime { get; set; } = DateTime.Now;
    }
}
