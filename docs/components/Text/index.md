# Text

Every component here except Insert Text and Replace All Text accepts an optional Request JSON input: wire another Text- or Paragraph-tab component's Request JSON output in and this component adds its trait to that block's existing text instead of inserting a new copy, so traits can be stacked one component at a time. See [Chaining styles](../../chaining.md).

| Component | Description |
|---|---|
| [Insert Text](InsertTextJson.md) | Inserts a plain string of text. |
| [Replace All Text](ReplaceAllTextJson.md) | Finds and replaces every occurrence of a string across the whole document. |
| [Styled Text](UpdateTextStyleJson.md) | Inserts text and applies any combination of style traits (bold, italic, underline, strikethrough, font size, font family, color, highlight, link, baseline) in one step. |
| [Bold Text](BoldTextJson.md) | Inserts text styled bold. |
| [Italic Text](ItalicTextJson.md) | Inserts text styled italic. |
| [Underline Text](UnderlineTextJson.md) | Inserts text styled underline. |
| [Strikethrough Text](StrikethroughTextJson.md) | Inserts text styled strikethrough. |
| [Font Size Text](FontSizeTextJson.md) | Inserts text at a specified font size. |
| [Font Family Text](FontFamilyTextJson.md) | Inserts text in a specified font family. |
| [Text Color Text](TextColorTextJson.md) | Inserts text in a specified foreground color. |
| [Highlight Text](HighlightTextJson.md) | Inserts text with a specified highlight (background) color. |
| [Link Text](LinkTextJson.md) | Inserts text as a hyperlink to a specified URL. |
| [Baseline Text](BaselineTextJson.md) | Inserts text with a specified baseline offset (for superscript/subscript). |
