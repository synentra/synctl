namespace VectraCtl.Core.Exceptions;

public class VectraCtlException : Exception
{
    public VectraCtlException(string message) : base(message) { }
    public VectraCtlException(string message, Exception inner) : base(message, inner) { }
}