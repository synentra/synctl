namespace SynentraCtl.Core.Exceptions;

public class SynentraCtlException : Exception
{
    public SynentraCtlException(string message) : base(message) { }
    public SynentraCtlException(string message, Exception inner) : base(message, inner) { }
}