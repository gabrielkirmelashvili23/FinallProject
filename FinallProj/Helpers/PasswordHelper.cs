using System.Security.Cryptography;

namespace FinallProj.Helpers
{
    internal static class PasswordHelper
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        public static string Hash(string text)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(text, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

            return Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
        }

        public static bool Verify(string text, string stored)
        {
            string[] parts = stored.Split('$');

            if (parts.Length != 2)
            {
                return false;
            }

            try
            {
                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] expected = Convert.FromBase64String(parts[1]);
                byte[] actual = Rfc2898DeriveBytes.Pbkdf2(text, salt, Iterations, HashAlgorithmName.SHA256, expected.Length);

                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static int GetStrength(string password)
        {
            int score = 0;

            if (password.Length >= 8)
            {
                score++;
            }

            if (password.Any(char.IsUpper) && password.Any(char.IsLower))
            {
                score++;
            }

            if (password.Any(char.IsDigit))
            {
                score++;
            }

            if (password.Any(symbol => !char.IsLetterOrDigit(symbol)))
            {
                score++;
            }

            return score;
        }

        public static bool IsValid(string password)
        {
            return password.Length >= 8
                && password.Any(char.IsLetter)
                && password.Any(char.IsDigit);
        }

        public static string NormalizeAnswer(string answer)
        {
            return answer.Trim().ToLower();
        }

        public static string GenerateTemporary()
        {
            string symbols = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            string password;

            do
            {
                password = string.Empty;

                for (int i = 0; i < 10; i++)
                {
                    password += symbols[RandomNumberGenerator.GetInt32(symbols.Length)];
                }
            } while (!IsValid(password));

            return password;
        }
    }
}
