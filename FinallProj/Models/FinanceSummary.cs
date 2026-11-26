namespace FinallProj.Models
{
    internal class FinanceSummary
    {
        public decimal Expected { get; set; }
        public decimal Collected { get; set; }
        public decimal Pending { get; set; }
        public decimal Overdue { get; set; }
        public decimal LateFees { get; set; }

        public decimal GetOutstanding()
        {
            return Pending + Overdue + LateFees;
        }

        public double GetCollectionRate()
        {
            if (Expected == 0)
            {
                return 0;
            }

            return (double)(Collected / Expected) * 100;
        }
    }
}
