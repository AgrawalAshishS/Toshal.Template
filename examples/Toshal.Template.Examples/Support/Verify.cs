namespace Toshal.Template.Examples.Support;

/// <summary>The examples check their own output, so running them is also a test. In your code these are your assertions.</summary>
public static class Verify
{
    public static void That(bool ok, string what)
    {
        if (!ok) throw new InvalidOperationException("Example check failed: " + what);
    }

    public static void Equal(string expected, string actual, string what)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException($"Example check failed: {what}{Environment.NewLine}Expected:{Environment.NewLine}{expected}{Environment.NewLine}Actual:{Environment.NewLine}{actual}");
        }
    }
}
