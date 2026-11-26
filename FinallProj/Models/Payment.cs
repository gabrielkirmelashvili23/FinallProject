using FinallProj.Helpers;
using FinallProj.Models.Enums;

namespace FinallProj.Models
{
    internal class Payment
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int Number { get; set; }
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsPaid { get; set; }
        public DateTime PaidDate { get; set; }

        public decimal LateFee { get; set; }

        public PaymentMethod Method { get; set; } = PaymentMethod.None;

        public Payment()
        {
        }

        public Payment(int id, int studentId, int number, decimal amount, DateTime dueDate)
        {
            Id = id;
            StudentId = studentId;
            Number = number;
            Amount = amount;
            DueDate = dueDate;
        }

        public PaymentStatus GetStatus(DateTime today)
        {
            if (IsPaid)
            {
                return PaymentStatus.Paid;
            }

            if (DueDate < today)
            {
                return PaymentStatus.Overdue;
            }

            return PaymentStatus.Pending;
        }

        public string ToFileLine()
        {
            string paidDate = IsPaid ? Format.Date(PaidDate) : "-";

            return $"{Id}|{StudentId}|{Number}|{Format.Number(Amount)}|{Format.Date(DueDate)}|{IsPaid}|{paidDate}|{Format.Number(LateFee)}|{Method}";
        }
    }
}
