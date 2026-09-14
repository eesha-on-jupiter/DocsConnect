# Alignment Preset

A value-list preset of Google Docs paragraph alignment values, for use with the Alignment input on paragraph-styling components.

## Values

- START
- CENTER
- END
- JUSTIFIED

## Feeds into

- [Styled Paragraph](../Paragraph/UpdateParagraphStyleJson.md)'s Alignment input
- [Aligned Text](../Paragraph/AlignedTextJson.md)'s Alignment input

## Notes

These values are literal Google Docs API `alignment` enum values. Using a string outside this list will cause the API to reject the request.
