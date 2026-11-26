using FinallProj.Exceptions;
using FinallProj.Helpers;
using FinallProj.Models;
using FinallProj.Models.Enums;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Managers
{
    internal class PaymentManager
    {
        public const decimal EnrollmentFee = 50m;
        public const decimal LateFee = 25m;

        private List<Payment> _payments;

        public PaymentManager(List<Payment> payments)
        {
            _payments = payments;
        }

        public bool HasPlan(int studentId)
        {
            return _payments.Any(payment => payment.StudentId == studentId);
        }

        private int GetNextId()
        {
            if (_payments.Count == 0)
            {
                return 1;
            }

            return _payments.Max(payment => payment.Id) + 1;
        }

        public decimal GetNetTuition(Student student, Course course)
        {
            decimal net = course.Tuition * (100 - student.ScholarshipPercent) / 100m;

            return Math.Round(net, 2);
        }

        public List<Payment> CreatePlan(Student student, Group group, Course course, DateTime enrollDate)
        {
            if (HasPlan(student.Id))
                throw new PaymentException(T("The student already has a payment plan!"));

            DateTime baseDate = group.StartDate > enrollDate ? group.StartDate : enrollDate;

            decimal net = GetNetTuition(student, course);
            decimal part = Math.Round(net / course.Installments, 2);

            List<Payment> plan = new List<Payment>();
            int nextId = GetNextId();

            plan.Add(new Payment(nextId, student.Id, 0, EnrollmentFee, baseDate));
            nextId++;

            for (int i = 1; i <= course.Installments; i++)
            {
                decimal amount = part;

                if (i == course.Installments)
                {
                    amount = net - part * (course.Installments - 1);
                }

                plan.Add(new Payment(nextId, student.Id, i, amount, baseDate.AddMonths(i - 1)));
                nextId++;
            }

            MarkFreeAsPaid(plan, baseDate);
            _payments.AddRange(plan);

            return plan;
        }

        public void RecalculatePlan(Student student, Course course)
        {
            List<Payment> unpaid = GetPaymentsOf(student.Id)
                .Where(payment => payment.Number > 0 && !payment.IsPaid)
                .ToList();

            if (unpaid.Count == 0)
            {
                return;
            }

            decimal paidTuition = GetPaymentsOf(student.Id)
                .Where(payment => payment.Number > 0 && payment.IsPaid)
                .Sum(payment => payment.Amount);

            decimal remaining = GetNetTuition(student, course) - paidTuition;

            if (remaining < 0)
            {
                remaining = 0;
            }

            decimal part = Math.Round(remaining / unpaid.Count, 2);

            for (int i = 0; i < unpaid.Count; i++)
            {
                unpaid[i].Amount = part;

                if (i == unpaid.Count - 1)
                {
                    unpaid[i].Amount = remaining - part * (unpaid.Count - 1);
                }
            }

            MarkFreeAsPaid(unpaid, Clock.Today);
        }

        private void MarkFreeAsPaid(List<Payment> payments, DateTime date)
        {
            foreach (Payment payment in payments)
            {
                if (payment.Amount == 0 && !payment.IsPaid)
                {
                    payment.IsPaid = true;
                    payment.PaidDate = date;
                    payment.Method = PaymentMethod.None;
                }
            }
        }

        public List<Payment> GetPaymentsOf(int studentId)
        {
            return _payments
                .Where(payment => payment.StudentId == studentId)
                .OrderBy(payment => payment.Number)
                .ToList();
        }

        public Payment? GetNextUnpaid(int studentId)
        {
            return GetPaymentsOf(studentId).FirstOrDefault(payment => !payment.IsPaid);
        }

        public Payment Pay(int paymentId, Student student, PaymentMethod method, DateTime today)
        {
            Payment? payment = _payments.FirstOrDefault(item => item.Id == paymentId && item.StudentId == student.Id);

            if (payment == null)
                throw new NotFoundException(T("Payment not found!"));

            if (payment.IsPaid)
                throw new PaymentException(T("This payment is already paid!"));

            Payment? next = GetNextUnpaid(student.Id);

            if (next != null && next.Id != payment.Id)
                throw new PaymentException(T("Pay the earlier payments first!"));

            if (payment.GetStatus(today) == PaymentStatus.Overdue)
            {
                payment.LateFee = LateFee;
            }

            payment.IsPaid = true;
            payment.PaidDate = today;
            payment.Method = method;

            return payment;
        }

        public decimal GetOutstanding(int studentId, DateTime today)
        {
            decimal total = 0;

            foreach (Payment payment in GetPaymentsOf(studentId))
            {
                if (!payment.IsPaid)
                {
                    total += payment.Amount;

                    if (payment.GetStatus(today) == PaymentStatus.Overdue)
                    {
                        total += LateFee;
                    }
                }
            }

            return total;
        }

        public bool HasOverdue(int studentId, DateTime today)
        {
            return GetPaymentsOf(studentId).Any(payment => payment.GetStatus(today) == PaymentStatus.Overdue);
        }

        public FinanceSummary GetSummary(DateTime today)
        {
            FinanceSummary summary = new FinanceSummary();

            foreach (Payment payment in _payments)
            {
                summary.Expected += payment.Amount;

                PaymentStatus status = payment.GetStatus(today);

                if (status == PaymentStatus.Paid)
                {
                    summary.Collected += payment.Amount + payment.LateFee;
                }
                else if (status == PaymentStatus.Overdue)
                {
                    summary.Overdue += payment.Amount;
                    summary.LateFees += LateFee;
                }
                else
                {
                    summary.Pending += payment.Amount;
                }
            }

            return summary;
        }

        public List<Payment> GetAllPayments()
        {
            return _payments.OrderBy(payment => payment.StudentId).ThenBy(payment => payment.Number).ToList();
        }

        public void DeletePlan(int studentId)
        {
            if (GetPaymentsOf(studentId).Any(payment => payment.IsPaid && payment.Amount > 0))
                throw new PaymentException(T("The student already made payments, the plan cannot be removed!"));

            _payments.RemoveAll(payment => payment.StudentId == studentId);
        }

        public void RemoveAllOf(int studentId)
        {
            _payments.RemoveAll(payment => payment.StudentId == studentId);
        }
    }
}
