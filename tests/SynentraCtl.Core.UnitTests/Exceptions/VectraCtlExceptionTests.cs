using SynentraCtl.Core.Exceptions;

namespace SynentraCtl.Core.UnitTests.Exceptions;

public class SynentraCtlExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var exception = new SynentraCtlException("test error");

        exception.Message.Should().Be("test error");
    }

    [Fact]
    public void Constructor_WithMessage_InnerExceptionIsNull()
    {
        var exception = new SynentraCtlException("test error");

        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndInner_SetsMessage()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new SynentraCtlException("outer error", inner);

        exception.Message.Should().Be("outer error");
    }

    [Fact]
    public void Constructor_WithMessageAndInner_SetsInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new SynentraCtlException("outer error", inner);

        exception.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void SynentraCtlException_IsAssignableFromException()
    {
        var exception = new SynentraCtlException("test");

        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void Constructor_WithEmptyMessage_SetsEmptyMessage()
    {
        var exception = new SynentraCtlException(string.Empty);

        exception.Message.Should().Be(string.Empty);
    }

    [Fact]
    public void ThrowingSynentraCtlException_CanBeCaughtAsException()
    {
        Action act = () => throw new SynentraCtlException("thrown");

        act.Should().Throw<SynentraCtlException>().WithMessage("thrown");
    }

    [Fact]
    public void ThrowingSynentraCtlException_WithInner_CanBeCaughtAndInnerIsPreserved()
    {
        var inner = new ArgumentException("arg");
        Action act = () => throw new SynentraCtlException("outer", inner);

        act.Should().Throw<SynentraCtlException>()
            .WithMessage("outer")
            .WithInnerException<ArgumentException>()
            .WithMessage("arg");
    }
}
