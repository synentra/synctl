using VectraCtl.Core.Exceptions;

namespace VectraCtl.Core.UnitTests.Exceptions;

public class VectraCtlExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var exception = new VectraCtlException("test error");

        exception.Message.Should().Be("test error");
    }

    [Fact]
    public void Constructor_WithMessage_InnerExceptionIsNull()
    {
        var exception = new VectraCtlException("test error");

        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndInner_SetsMessage()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new VectraCtlException("outer error", inner);

        exception.Message.Should().Be("outer error");
    }

    [Fact]
    public void Constructor_WithMessageAndInner_SetsInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new VectraCtlException("outer error", inner);

        exception.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void VectraCtlException_IsAssignableFromException()
    {
        var exception = new VectraCtlException("test");

        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void Constructor_WithEmptyMessage_SetsEmptyMessage()
    {
        var exception = new VectraCtlException(string.Empty);

        exception.Message.Should().Be(string.Empty);
    }

    [Fact]
    public void ThrowingVectraCtlException_CanBeCaughtAsException()
    {
        Action act = () => throw new VectraCtlException("thrown");

        act.Should().Throw<VectraCtlException>().WithMessage("thrown");
    }

    [Fact]
    public void ThrowingVectraCtlException_WithInner_CanBeCaughtAndInnerIsPreserved()
    {
        var inner = new ArgumentException("arg");
        Action act = () => throw new VectraCtlException("outer", inner);

        act.Should().Throw<VectraCtlException>()
            .WithMessage("outer")
            .WithInnerException<ArgumentException>()
            .WithMessage("arg");
    }
}
