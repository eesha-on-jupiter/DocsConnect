# Bullet List

Builds a multi-item bulleted list from a list of strings, with optional per-item indent levels, producing one self-indexed sequence of `insertText` and `createParagraphBullets` requests.

## Inputs

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| Items | string (tree) | Yes | — | The text of each list item; a multi-branch tree is flattened into one ordered list. |
| Indent Levels | int (tree) | No | 0 | Indent level for each item, for nested lists (flattened the same way as Items). Only applied where >0. |
| Bullet Preset | string | No | "BULLET_DISC_CIRCLE_SQUARE" | Bullet glyph preset to apply to the whole list, including `BULLET_CHECKBOX`. |

## Outputs

| Name | Type | Description |
|---|---|---|
| Request JSON | string (list) | The full sequence of requests for the list, already assembled into one requests-array string and self-indexed from 1 — a single item, not a list. |

## Output

This component never calls the API directly — it emits Request JSON (a Google Docs `Request` object, or a list of them) that must be wired into [Request Aggregator](../Combine/RequestAggregator.md)'s `Request JSON` input (on its own wire, in the order it should appear, to get a real document position) before [Batch Update](../Document/BatchUpdateDocument.md) can push it live.

## Notes

This component was moved here from the removed Report Tools tab. It is a composite: it builds a list of items into one self-indexed sequence, and is meant for a multi-item list in one block. For a single bulleted line, use Bullet Item Text instead.

Items and Indent Levels are read as data trees and flattened across all branches into one ordered list, rather than plain list access — this avoids the component re-solving once per branch on a multi-branch upstream source.

The Bullet Preset input is typically fed by the Bullet Glyph preset.

## Related

- [Bullet Item Text](CreateParagraphBulletsJson.md) — use instead for a single bulleted line rather than a multi-item list.
- [Bullet Glyph preset](../Presets/BulletGlyphPreset.md) — feeds the Bullet Preset input.
