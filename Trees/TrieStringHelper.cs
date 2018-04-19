using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Trees
{
    public static class TrieStringHelper
    {
        public const int MIN_TERM_LENGTH = 2;
        private static char[] _treatAsSpaces = new char[0];
        private static HashSet<char> _acceptedCharacters = new HashSet<char>("0123456789abcdefghijklmnopqrstuvwxyzæøå_-".ToCharArray());

        private static Dictionary<char, char> _replaceBy = new Dictionary<char, char>
        {
            { 'à', 'a' },
            { 'á', 'a' },
            { 'ä', 'a' },
            { 'é', 'e' },
            { 'è', 'e' },
            { 'ï', 'i' },
            { 'ì', 'i' },
            { 'í', 'i' },
            { 'ó', 'o' },
            { 'ò', 'o' },
            { 'ö', 'o' },
            { 'ü', 'u' }
        };

        public static string[] Parse(string s, bool includeSuffixes = true)
        {
            s = s.ToLower().Trim();

            foreach (var c in _treatAsSpaces)
            {
                s = s.Replace(c, ' ');
            }

            var termCandidates = new List<string>();
            StringBuilder term = new StringBuilder();

            foreach (var c in s)
            {
                if (c == ' ')
                {
                    string termToAdd = term.ToString();

                    if (termToAdd.Length >= MIN_TERM_LENGTH)
                    {
                        termCandidates.Add(termToAdd);
                    }

                    term.Clear();
                }
                else if (_acceptedCharacters.Contains(c))
                {
                    term.Append(c);
                }
                else if (_replaceBy.ContainsKey(c))
                {
                    term.Append(_replaceBy[c]);
                }
                else
                {
                    // Ignore character
                }
            }

            string finalTermToAdd = term.ToString();

            if (finalTermToAdd.Length >= MIN_TERM_LENGTH)
            {
                termCandidates.Add(finalTermToAdd);
            }

            var terms = new HashSet<string>();

            foreach (var item in termCandidates)
            {
                if (item.Length >= MIN_TERM_LENGTH)
                {
                    terms.Add(item);
                }

                if (includeSuffixes)
                {
                    for (int i = 1; i < item.Length - (MIN_TERM_LENGTH - 1); i++)
                    {
                        terms.Add(item.Substring(i));
                    }
                }
            }

            return terms.ToArray();
        }
    }
}