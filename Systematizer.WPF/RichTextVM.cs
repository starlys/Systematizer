using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.

namespace Systematizer.WPF;

/// <summary>
/// Model for RichTextView
/// </summary>
class RichTextVM : EditableVM
{
    const string HEADINGPREFIX = "--", BULLETPREFIX = "* ";
    static readonly FontWeight BOLD = FontWeight.FromOpenTypeWeight(800);
    static readonly Regex URL_REGEX = new(@"((http|https|mailto|file):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?)");
    static readonly Regex BULLET_REGEX = new(@".\t");

    /// <summary>
    /// Hack! This gets set by the code-behind when text box got focus
    /// </summary>
    public RichTextBox TextBox;

    bool IsUpdatingText;

    public RichTextVM()
    {
        RemoveFormat = new SimpleCommand(_ =>
        {
            var range = GetCurrentParagraphRange(false);
            if (range == null) return;
            range.ApplyPropertyValue(TextElement.FontSizeProperty, 12.0);
            range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
            FocusTextBox();
        });

        HeadingFormat = new SimpleCommand(_ =>
        {
            var range = GetCurrentParagraphRange(true);
            if (range == null) return;
            range.ApplyPropertyValue(TextElement.FontSizeProperty, 14.0);
            range.ApplyPropertyValue(TextElement.FontWeightProperty, BOLD);
            FocusTextBox();
        });
        BulletFormat = new SimpleCommand(_ =>
        {
            //fails: EditingCommands.ToggleBullets.Execute(null, TextBox);
            var range = GetCurrentParagraphRange(false);
            if (range == null) return;
            string plain = RangeToText(range);
            range.Text = "";
            var list = new List(new ListItem(new Paragraph(new Run(plain))));
            Block newPara = range.Start.Paragraph;
            if (newPara.Parent is ListItem p2) newPara = ((List)p2.Parent);
            TextBox.Document.Blocks.InsertAfter(newPara, list);
            FocusTextBox();
        });
        InitializeFromPersistent();
    }

    public override void InitializeFromPersistent()
    {
    }

    void FocusTextBox()
    {
        TextBox?.Focus();
    }

    /// <summary>
    /// Set Text property from the current flow document contents
    /// </summary>
    public void UpdateText()
    {
        if (TextBox == null) return;
        IsUpdatingText = true;
        string newText = FlowDocumentToText(TextBox.Document);
        if (newText != Text)
            Text = newText;
        IsUpdatingText = false;
    }

    /// <summary>
    /// Called from view directly on Enter key (key preview)
    /// </summary>
    public void EnterPressed()
    {
        //Enter should clear format of new para if previous para was heading
        if (TextBox == null) return;
        var para2 = TextBox.CaretPosition.Paragraph;
        if (para2 == null) return;
        var range2 = new TextRange(para2.ContentStart, para2.ContentEnd);
        bool wasOnHeading = ((FontWeight)range2.GetPropertyValue(TextElement.FontWeightProperty)) == BOLD;
        if (!wasOnHeading) return;

        //much experimentation: need to do a lot to prevent it from holding the previous style at the selection point
        VisualUtils.DelayThen(10, () =>
        {
            var para = TextBox.CaretPosition.Paragraph;
            if (para == null) return;
#pragma warning disable IDE0017 // Simplify object initialization
            var range = new TextRange(para.ContentStart, para.ContentEnd);
#pragma warning restore IDE0017 // Simplify object initialization
            range.Text = "X";
            TextBox.Selection.Select(range.Start.GetPositionAtOffset(0), range.Start.GetPositionAtOffset(1));
            RemoveFormat.Execute(null); //only works if para is not empty
            TextBox.CaretPosition = TextBox.Document.ContentStart;
            VisualUtils.DelayThen(1, () =>
            {
                TextBox.CaretPosition = para.ContentEnd;
                range.Text = "";
            });
        });
    }

    public void Initialize(RichTextBox rtb)
    {
        TextBox = rtb;
        TextBox.Document = TextToFlowDocument(Text, !IsEditMode);
    }

    string _text;
    /// <summary>
    /// The storage formatted text
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            NotifyChanged();
            if (TextBox != null && !IsUpdatingText)
                TextBox.Document = TextToFlowDocument(value, !IsEditMode);
        }
    }

    protected override void EditModeChanged()
    {
        if (TextBox != null)
            TextBox.Document = TextToFlowDocument(Text, !IsEditMode);
    }

    //commands (implementation is in ctor)
    public ICommand RemoveFormat { get; private set; } 
    public ICommand HeadingFormat { get; private set; } 
    public ICommand BulletFormat { get; private set; }

    /// <summary>
    /// null or TextRange of current selection
    /// </summary>
    TextRange GetCurrentParagraphRange(bool addPlaceholderWhenEmpty)
    {
        if (TextBox == null) return null;
        var para = TextBox.CaretPosition.Paragraph;
        if (para == null) return null;
        var range = new TextRange(para.ContentStart, para.ContentEnd);
        if (range.IsEmpty && addPlaceholderWhenEmpty)
        {
            range.Text = "X";
            TextBox.Selection.Select(range.Start.GetPositionAtOffset(0), range.Start.GetPositionAtOffset(1));
            range = TextBox.Selection;
        }
        return range;
    }

    /// <summary>
    /// Convert text in storage format to FlowDocument for binding to
    /// </summary>
    public static FlowDocument TextToFlowDocument(string text, bool parseHyperlinks)
    {
        void addAsString(Paragraph b, string s)
        {
            if (parseHyperlinks)
                foreach (var i in StringToRunsWithHyperlinks(s))
                    b.Inlines.Add(i);
            else
                b.Inlines.Add(s);
        }

        var lines = ToLines(text);
        var doc = new FlowDocument();
        List activeList = null;
        foreach (var line in lines)
        {
            if (line.StartsWith(BULLETPREFIX))
            {
                if (activeList == null)
                {
                    activeList = new List();
                    doc.Blocks.Add(activeList);
                }
                var para = new Paragraph();
                activeList.ListItems.Add(new ListItem(para));
                addAsString(para, line[2..]);
            }
            else
            {
                //nonbullet
                activeList = null;
                var para = new Paragraph();
                doc.Blocks.Add(para);
                if (line.StartsWith(HEADINGPREFIX))
                {
                    para.FontSize = 14;
                    para.FontWeight = BOLD;
                    addAsString(para, line[2..]);
                }
                else
                    addAsString(para, line);
            }
        }
        return doc;
    }

    /// <summary>
    /// Convert FlowDocument to the specialized storage format
    /// </summary>
    public static string FlowDocumentToText(FlowDocument doc)
    {
        var plainParagraphs = new List<string>();
        foreach (var block in doc.Blocks)
        {
            if (block is List list)
            {
                var range = new TextRange(list.ContentStart, list.ContentEnd);
                string bulletedParagraphs = RangeToText(range);
                foreach (string s in ToLines(bulletedParagraphs))
                    plainParagraphs.Add(BULLETPREFIX + s.Trim());
            }
            else if (block is Paragraph para)
            {
                var range = new TextRange(para.ContentStart, para.ContentEnd);
                string plain = RangeToText(range);
                try
                {
                    if (((FontWeight)range.GetPropertyValue(TextElement.FontWeightProperty)) == BOLD)
                        plainParagraphs.Add(HEADINGPREFIX + plain);
                    else
                        plainParagraphs.Add(plain);
                }
                catch
                {
                    plainParagraphs.Add(plain);
                }
            }
        }
        return string.Join("\r\n", plainParagraphs);
    }

    static string RangeToText(TextRange range)
    {
        string s = range.Text.Trim();
        s = BULLET_REGEX.Replace(s, "");
        return s;
    }

    static List<string> ToLines(string s)
    {
        if (s == null) return new List<string>();
        s = s.Replace("\r", "");
        var lines = new List<string>(s.Split('\n'));

        //don't allow multiple contiguous blank lines
        //for (int i = lines.Count - 1; i > 0; --i)
        //    if (string.IsNullOrWhiteSpace(lines[i]) && string.IsNullOrWhiteSpace(lines[i - 1])) 
        //        lines.RemoveAt(i);

        return lines;
    }

    static IEnumerable<Inline> StringToRunsWithHyperlinks(string s)
    {
        for (int iter = 0; iter < 500; ++iter) //infinite loop control
        {
            var m = URL_REGEX.Match(s);
            if (m == null || !m.Success) //no more urls
            {
                if (s.Length > 0) yield return new Run(s);
                break;
            }
            string s0 = s[..m.Index],
                url = s.Substring(m.Index, m.Length),
                s1 = s[(m.Index + m.Length)..];
            if (s0.Length > 0) yield return new Run(s0);
            var hlink = new Hyperlink(new Run(url)) { NavigateUri = new Uri(url) };
            hlink.RequestNavigate += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true, Verb = "open" });
                }
                catch { }
            };
            yield return hlink;
            s = s1;
        }
    }
}
