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
}