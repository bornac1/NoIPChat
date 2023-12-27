namespace Messages
{
    public class StringProcessing
    {
        public static ReadOnlySpan<char> GetServer(string username)
        {
            ReadOnlySpan<char> str = username.AsSpan();
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '@')
                {
                    if (i + 1 < str.Length)
                    {
                        return str[(i + 1)..];
                    }
                    break;
                }
            }
            //it's already just server
            return str;
        }
        public static IEnumerable<string> GetReceivers(string receivers)
        {
            ReadOnlyMemory<char> str = receivers.AsMemory();
            int j = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (str.Span[i] == ';')
                {
                    yield return str[j..i].ToString();
                    j = i + 1;
                }
            }
            if (j < str.Length)
            {
                yield return str[j..].ToString();
            }
            yield break;
        }
        public static IEnumerable<string> GetUsersServer(string users)
        {
            return GetReceivers(users);
        }
    }
}
