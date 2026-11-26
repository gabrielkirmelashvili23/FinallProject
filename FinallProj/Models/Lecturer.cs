using FinallProj.Models.Enums;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Models
{
    internal class Lecturer : User
    {
        public string Subject { get; set; } = string.Empty;

        public Lecturer() : base(UserRole.Lecturer)
        {
        }

        public Lecturer(int id, string name, string username, string subject)
            : base(id, name, username, UserRole.Lecturer)
        {
            Subject = subject;
        }

        public override string GetInfo()
        {
            return base.GetInfo() + $" | {T("Subject")}: {Subject}";
        }

        public override string ToFileLine()
        {
            return base.ToFileLine() + $"|{Subject}";
        }
    }
}
