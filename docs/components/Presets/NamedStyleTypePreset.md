# Named Style Type Preset

A value-list preset of Google Docs named paragraph style values, for use with the Named Style input on paragraph-styling components.

## Values

- NORMAL_TEXT
- TITLE
- SUBTITLE
- HEADING_1
- HEADING_2
- HEADING_3
- HEADING_4
- HEADING_5
- HEADING_6

## Feeds into

- [Styled Paragraph](../Paragraph/UpdateParagraphStyleJson.md)'s Named Style input
- [Heading Text](../Paragraph/HeadingTextJson.md)'s Named Style input

## Notes

These values are literal Google Docs API `namedStyleType` enum values. Using a string outside this list will cause the API to reject the request.
