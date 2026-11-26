namespace FinallProj.Exceptions
{
    internal class DuplicateUsernameException : AppException
    {
        public DuplicateUsernameException(string message) : base(message)
        {
        }
    }
}
