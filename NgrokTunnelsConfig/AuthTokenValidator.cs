namespace NgrokTunnelsConfig;

internal static class AuthTokenValidator
{
    public static bool IsValid(string authToken)
    {
        if (authToken.Length != 49)
        {
            return false;
        }

        foreach (var ch in authToken)
        {
            var isLetter = (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');
            var isDigit = ch >= '0' && ch <= '9';
            if (!(isLetter || isDigit || ch == '_'))
            {
                return false;
            }
        }

        return true;
    }
}
