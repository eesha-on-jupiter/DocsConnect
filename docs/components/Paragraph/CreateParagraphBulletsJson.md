# Bullet Item Text

Inserts one or more bulleted lines of text, producing `insertText` requests followed by `createParagraphBullets` requests.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Text | string (tree) | Yes | — | Text to insert as bulleted line(s). Each item places its own line; a multi-branch tree is flattened into one ordered list. |
| Bullet Preset | string | Yes | "BULLET_DISC_CIRCLE_SQUARE" | Bullet glyph preset applied to every line. |
| Segment ID | string | No | "" | Optional, blank = document body, or wire in a Header ID / Footer ID / Footnote ID from the Document tab to target that segment instead. |
| Request JSON | string (list) | No | — | Optional. Wire another Text- or Paragraph-tab component's Request JSON output in to add this trait to that block's existing text instead of inserting new text. Text is ignored while this is connected, and Segment ID is inherited from the incoming block. See [Chaining styles](../../chaining.md). |

## Outputs

| Name | Type | Description |
|---|---|---|
| Request JSON | string (list) | The `insertText` and `createParagraphBullets` requests for this line. |

## Output

This component never calls the API directly — it emits Request JSON (a Google Docs `Request` object, or a list of them) that goes into [Request Aggregator](../Combine/RequestAggregator.md)'s `Fixed Requests` input (it already carries its own indices, so it must not be re-placed) before [Batch Update](../Document/BatchUpdateDocument.md) can push it live.

## Notes

This component replaces the older range-based Create Paragraph Bullets component: it is now self-contained, taking Text directly rather than an index into an already-inserted range. For a multi-item list assembled in one block, use Bullet List instead.

Feed a list of Text to get several bulleted lines at once, each landing as its own line. Bullet Preset stays a single value and applies to every line — there is no per-line preset.

Text is read as a data tree and flattened across all branches into one ordered list, rather than plain list access — this keeps the component's running insertion cursor intact even when the upstream source is a multi-branch tree (list access would re-solve the component once per branch and silently reset that state).

The Bullet Preset input is typically fed by the Bullet Glyph preset.

## Related

- [Bullet List](BulletList.md) — use instead for a multi-item list rather than a single bulleted line.
- [Bullet Glyph preset](../Presets/BulletGlyphPreset.md) — feeds the Bullet Preset input.
