using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FanyiNetwork.Models
{
    public class CheifStatsVM
    {
        public int userid { get; set; }
        public string name { get; set; }
        public string group { get; set; }
        public int totalAssign { get; set; }
        public int assignoverdue { get; set; }
        public double assignoverduePercentage { get; set; }
        public int reviewoverdue { get; set; }
        public double reviewoverduePercentage { get; set; }
    }

    public class EditorStatsVM
    {
        public int userid { get; set; }
        public string name { get; set; }
        public string group { get; set; }
        public int totalOrder { get; set; }
        public int totalWord { get; set; }
        public string efficiency { get; set; }
        public int late { get; set; }
        public double latePercentage { get; set; }
        public int overdue { get; set; }
        public double overduePercentage { get; set; }
        public double review { get; set; }
        public double rating { get; set; }
    }
}
