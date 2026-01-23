using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KumasKaliteKontrol.Models
{
    public class Defect
    {
        public int Id { get; set; }
        public int FabricId { get; set; }
        public int StartMeter { get; set; }
        public int EndMeter { get; set; }
        public int PointType { get; set; } 
        public int Length { get; set; }   
    }
}

