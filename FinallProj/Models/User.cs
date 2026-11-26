using FinallProj.Exceptions;
using FinallProj.Helpers;
using FinallProj.Models.Enums;
using static FinallProj.Helpers.Translator;

namespace FinallProj.Models
{
    internal class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; private set; } = string.Empty;
        public string SecurityQuestion { get; private set; } = string.Empty;
        public string SecurityAnswerHash { get; private set; } = string.Empty;

        public UserRole Role { get; private set; }

        public User()
        {
        }

        public User(UserRole role)
        {
            Role = role;
        }

        public User(int id, string name, string username, UserRole role)
        {
            Id = id;
            Name = name;
            Username = username;
            Role = role;
        }

        public void SetPassword(string password)
        {
            if (!PasswordHelper.IsValid(password))
                throw new WeakPasswordException(T("Password must have at least 8 characters, a letter and a digit!"));

            PasswordHash = PasswordHelper.Hash(password);
        }

        public bool CheckPassword(string password)
        {
            return PasswordHelper.Verify(password, PasswordHash);
        }

        public void SetSecurity(string question, string answer)
        {
            if (string.IsNullOrWhiteSpace(question) || answer.Trim().Length < 2)
                throw new ValidationException(T("The answer must have at least 2 characters!"));

            SecurityQuestion = question;
            SecurityAnswerHash = PasswordHelper.Hash(PasswordHelper.NormalizeAnswer(answer));
        }

        public bool HasSecurityQuestion()
        {
            return SecurityQuestion != string.Empty && SecurityAnswerHash != string.Empty;
        }

        public bool CheckSecurityAnswer(string answer)
        {
            return PasswordHelper.Verify(PasswordHelper.NormalizeAnswer(answer), SecurityAnswerHash);
        }

        public void LoadSecrets(string passwordHash, string question, string answerHash)
        {
            PasswordHash = passwordHash;
            SecurityQuestion = question;
            SecurityAnswerHash = answerHash;
        }

        public virtual string GetInfo()
        {
            return $"[{Id}] {Name} | {Username} | {T(Role.ToString())}";
        }

        public virtual string ToFileLine()
        {
            return $"{Role}|{Id}|{Name}|{Username}|{PasswordHash}|{SecurityQuestion}|{SecurityAnswerHash}";
        }

        public override string ToString()
        {
            return $"[{Id}]: {Name}, {Role}.";
        }
    }
}
