using System.Text;
using System.Text.RegularExpressions;

namespace ModelPublisher.Core.Shared;

public static partial class MarkdownHelper
{
    public static string ToPlainText(string markdown)
    {
        var text = markdown;

        // Headers
        text = HeadingRegex().Replace(text, "$1");
        // Unordered list markers → plain-text bullet (must run before bold/italic to avoid * being consumed)
        text = UnorderedListRegex().Replace(text, "• ");
        // Ordered list markers → preserve number
        text = OrderedListRegex().Replace(text, "$1. ");
        // Bold / italic
        text = BoldItalicRegex().Replace(text, "$1");
        // Inline code
        text = InlineCodeRegex().Replace(text, "$1");
        // Code blocks
        text = CodeBlockRegex().Replace(text, "$1");
        // Links — keep label, drop URL
        text = LinkRegex().Replace(text, "$1");
        // Images — drop entirely
        text = ImageRegex().Replace(text, "");
        // Blockquotes
        text = BlockquoteRegex().Replace(text, "");
        // Horizontal rules
        text = HorizontalRuleRegex().Replace(text, "");
        // Collapse 3+ newlines to 2
        text = ExcessNewlinesRegex().Replace(text, "\n\n");

        return text.Trim();
    }

    /// <summary>
    /// Converts markdown to HTML suitable for injection into a TipTap (ProseMirror) rich text editor.
    /// Produces block-level elements: h1-h6, p, ul/li, ol/li, and inline strong/em/code.
    /// </summary>
    public static string ToTipTapHtml(string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder();
        var inUl = false;
        var inOl = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            var ulMatch = UlLineRegex().Match(line);
            var olMatch = OlLineRegex().Match(line);
            var hMatch  = HLineRegex().Match(line);

            if (ulMatch.Success)
            {
                if (inOl) { sb.Append("</ol>"); inOl = false; }
                if (!inUl) { sb.Append("<ul>"); inUl = true; }
                sb.Append($"<li>{InlineHtml(ulMatch.Groups[1].Value)}</li>");
            }
            else if (olMatch.Success)
            {
                if (inUl) { sb.Append("</ul>"); inUl = false; }
                if (!inOl) { sb.Append("<ol>"); inOl = true; }
                sb.Append($"<li>{InlineHtml(olMatch.Groups[1].Value)}</li>");
            }
            else
            {
                if (inUl) { sb.Append("</ul>"); inUl = false; }
                if (inOl) { sb.Append("</ol>"); inOl = false; }

                if (hMatch.Success)
                {
                    var level = hMatch.Groups[1].Value.Length;
                    sb.Append($"<h{level}>{InlineHtml(hMatch.Groups[2].Value)}</h{level}>");
                }
                else if (string.IsNullOrWhiteSpace(line))
                {
                    // blank lines are implicit paragraph breaks — skip
                }
                else
                {
                    sb.Append($"<p>{InlineHtml(line.Trim())}</p>");
                }
            }
        }

        if (inUl) sb.Append("</ul>");
        if (inOl) sb.Append("</ol>");

        return sb.ToString();
    }

    private static string InlineHtml(string text)
    {
        // Bold must run before italic so ** isn't consumed by the single-* rule
        text = InlineBoldAsterisksRegex().Replace(text, "<strong>$1</strong>");
        text = InlineBoldUnderscoresRegex().Replace(text, "<strong>$1</strong>");
        text = InlineItalicAsterisksRegex().Replace(text, "<em>$1</em>");
        text = InlineItalicUnderscoresRegex().Replace(text, "<em>$1</em>");
        text = InlineCodeRegex().Replace(text, "<code>$1</code>");
        // Links — keep label, drop URL
        text = LinkRegex().Replace(text, "$1");
        // Images — drop entirely
        text = ImageRegex().Replace(text, "");
        return text;
    }

    // ── Block patterns ────────────────────────────────────────────────────────

    [GeneratedRegex(@"^(#{1,6})\s+(.+)", RegexOptions.Multiline)]
    private static partial Regex HLineRegex();

    [GeneratedRegex(@"^\s*[-*+]\s+(.+)", RegexOptions.Multiline)]
    private static partial Regex UlLineRegex();

    [GeneratedRegex(@"^\s*(\d+)\.\s+(.+)", RegexOptions.Multiline)]
    private static partial Regex OlLineRegex();

    // ── ToPlainText patterns ──────────────────────────────────────────────────

    [GeneratedRegex(@"^#{1,6}\s+(.*)", RegexOptions.Multiline)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"\*{1,2}([^*]+)\*{1,2}")]
    private static partial Regex BoldItalicRegex();

    [GeneratedRegex(@"`([^`]+)`")]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"```[\w]*\n(.*?)```", RegexOptions.Singleline)]
    private static partial Regex CodeBlockRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\([^\)]+\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"!\[([^\]]*)\]\([^\)]+\)")]
    private static partial Regex ImageRegex();

    [GeneratedRegex(@"^\s*[-*+]\s+", RegexOptions.Multiline)]
    private static partial Regex UnorderedListRegex();

    [GeneratedRegex(@"^\s*(\d+)\.\s+", RegexOptions.Multiline)]
    private static partial Regex OrderedListRegex();

    [GeneratedRegex(@"^\s*>\s+", RegexOptions.Multiline)]
    private static partial Regex BlockquoteRegex();

    [GeneratedRegex(@"^\s*[-*_]{3,}\s*$", RegexOptions.Multiline)]
    private static partial Regex HorizontalRuleRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExcessNewlinesRegex();

    // ── Inline HTML patterns ──────────────────────────────────────────────────

    [GeneratedRegex(@"\*\*(.+?)\*\*")]
    private static partial Regex InlineBoldAsterisksRegex();

    [GeneratedRegex(@"__(.+?)__")]
    private static partial Regex InlineBoldUnderscoresRegex();

    [GeneratedRegex(@"\*(.+?)\*")]
    private static partial Regex InlineItalicAsterisksRegex();

    [GeneratedRegex(@"_(.+?)_")]
    private static partial Regex InlineItalicUnderscoresRegex();
}
