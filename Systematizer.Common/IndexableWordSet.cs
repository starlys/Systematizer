using System.Text.RegularExpressions;

#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.

namespace Systematizer.Common;

/// <summary>
/// Collection of words for use with Word table
/// </summary>
public class IndexableWordSet
{
    static readonly Regex BADLETTERS = new("[^a-zA-Z0-9]");

    static readonly string[] STOPWORDS = new[] {  "A", "ALL", "AM", "AN", "AND", "ANY", "ARE", "ARENT", "AS", "AT", "BE", "BEEN", "BEFORE", "BEING", "BELOW", "BOTH", "BUT", "BY",
        "CANT", "CANNOT", "COULD", "COULDNT", "DID", "DIDNT", "DO", "DOES", "DOESNT", "DOING", "DONT", "DOWN", "DURING", "EACH", "FEW", "FOR", "FROM", "HAD", "HADNT",
        "HAS", "HASNT", "HAVE", "HAVENT", "HAVING", "HE", "HED", "HES", "HER", "HERE", "HERES", "HERS", "HIM", "HIS", "HOW", "HOWS", "I", "ID", "ILL", "IM", "IVE", "IF",
        "IN", "INTO", "IS", "ISNT", "IT", "ITS", "LETS", "ME", "MORE", "MOST", "MY", "NO", "NOR", "NOT", "OF", "OFF", "ON", "ONCE", "ONLY", "OR", "OTHER", "OUGHT", "OUR",
        "OURS", "OUT", "OVER", "OWN", "SAME", "SHE", "SHED", "SHELL", "SHES", "SHOULD", "SHOULDNT", "SO", "SOME", "SUCH", "THAN", "THAT", "THATS", "THE", "THEIR", "THEIRS",
        "THEM", "THEN", "THERE", "THERES", "THESE", "THEY", "THEYD", "THEYLL", "THEYRE", "THEYVE", "THIS", "THOSE", "THROUGH", "TO", "TOO", "UNDER", "UNTIL", "UP",
        "VERY", "WAS", "WASNT", "WE", "WED", "WELL", "WERE", "WEVE", "WERE", "WERENT", "WHAT", "WHATS", "WHEN", "WHENS", "WHERE", "WHERES", "WHICH", "WHILE", "WHO",
        "WHOS", "WHOM", "WHY", "WHYS", "WITH", "WONT", "WOULD", "WOULDNT", "YOU", "YOUD", "YOULL", "YOURE", "YOUVE", "YOUR", "YOURS" };

    readonly HashSet<string> Words = new(); //uppercase, max 8 chars

    /// <summary>
    /// Add arbitrarily long text
    /// </summary>
    /// <param name="s"></param>
    public void AddUserField(string s)
    {
        string[] words = (s ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words) AddWord(word);
    }

    /// <summary>
    /// ensure word is truncated at 8 chars, uppercase, and not one of the stop words; return null if not usable
    /// </summary>
    public static string NormalizeWord(string s)
    {
        //only keep letters and digits
        if (s == null) return null;
        s = BADLETTERS.Replace(s, "");

        //only keep 8 chars and omit stopwords
        if (s.Length > 8) s = s[..8];
        s = s.ToUpperInvariant();
        if (s.Length == 0 || STOPWORDS.Contains(s)) return null;

        return s;
    }

    public void AddWord(string s)
    {
        s = NormalizeWord(s);
        if (s != null)
            Words.Add(s);
    }

    /// <summary>
    /// Get words that should be added to persistent index
    /// </summary>
    public List<string> GetIndexable()
    {
        return Words.ToList();
    }
}

