namespace Neon.Test.Util;

public static class Aborter
{
    public static void Abort(int code)
    {
        Environment.Exit(code);
    }
}