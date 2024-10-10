using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Enums = WC.Models.Enums;

namespace WC.Models.DTO.Request
{
    public class UserPlanRequest
    {
        public int UserId { get; set; }
        public Enums.Plans Plan { get; set; }
        public DateTime PlanStart { get; set; } = DateTime.Now;
        public DateTime PlanEnd { get; set; } = DateTime.Now.AddMonths(1); //TODO: Plans on monthly basis or?
        public int IsActive { get; set; } = 1; //TODO: Change once payment process is implemnted. IsActive = 1 once payment is done, until then, IsActive = 0;
    }
}
