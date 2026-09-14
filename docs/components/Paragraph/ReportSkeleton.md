# Report Skeleton

Builds a title, a set of styled section headings, and their body paragraphs, producing one self-indexed sequence of `insertText` and `updateParagraphStyle` requests covering the whole document skeleton.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Title | string | No | "" | The document title text, inserted in TITLE style. Left blank, the title is skipped entirely (no empty paragraph). |
| Headings | string (list) | No | — (empty list) | Section heading text, one per section. |
| Heading Levels | int (list) | No | 1 | Named-style heading level for each heading (e.g. 1 for `HEADING_1`), clamped to 1-6. If shorter than Headings, the missing entries default to 1 rather than repeating the last supplied value. |
| Bodies | string (list) | No | — (empty list) | Body paragraph text for each section. If a section has no corresponding (non-blank) entry here, its body paragraph is skipped entirely rather than reusing an earlier body. |

## Outputs

| Name | Type | Description |
|---|---|---|
| Request JSON | string (list) | The full sequence of requests for the title, headings, and body paragraphs, already assembled into one requests-array string and self-indexed from 1 — a single item, not a list. |

## Output

This component never calls the API directly — it emits Request JSON (a Google Docs `Request` object, or a list of them) that must be wired into [Request Aggregator](../Combine/RequestAggregator.md)'s `Request JSON` input (on its own wire, in the order it should appear, to get a real document position) before [Batch Update](../Document/BatchUpdateDocument.md) can push it live.

## Notes

This component was moved here from the removed Report Tools tab. It is a composite: it builds the title, each per-section styled heading, and each body paragraph as one self-indexed sequence, using standard Google Docs `NamedStyleType` values for the heading levels.

Headings, Heading Levels, and Bodies are index-matched by position, but not with the "shorter list repeats its last value" convention used elsewhere in this tab: a missing Heading Level defaults to 1 (not the previous section's level), and a missing or blank Body simply omits that section's body paragraph (not the previous section's body).

## Related

- [Heading Text](HeadingTextJson.md) — the single-heading equivalent, if only one heading is needed rather than a full skeleton.
- [Report Header Block](ReportHeaderBlock.md) — a separate composite for a project title block, often placed before a Report Skeleton.
