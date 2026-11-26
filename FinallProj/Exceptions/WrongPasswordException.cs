namespace FinallProj.Exceptions
{
    internal class WrongPasswordException : AppException
    {
        public WrongPasswordException(string message) : base(message)
        {
        }
    }
}
