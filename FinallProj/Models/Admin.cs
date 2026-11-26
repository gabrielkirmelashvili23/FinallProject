using FinallProj.Models.Enums;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Models
{
    internal class Admin : User
    {
        public Admin() : base(UserRole.Admin)
        {
        }

        public Admin(int id, string name, string username)
            : base(id, name, username, UserRole.Admin)
        {
        }

        public override string GetInfo()
        {
            return $"[{T("Admin")}] {Name} | {Username}";
        }
    }
}
