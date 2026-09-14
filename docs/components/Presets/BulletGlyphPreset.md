# Bullet Glyph Preset

A value-list preset of Google Docs bullet/numbering preset values, for use with the Bullet Preset input on bullet-list components.

## Values

- BULLET_DISC_CIRCLE_SQUARE
- BULLET_DIAMONDX_ARROW3D_SQUARE
- BULLET_CHECKBOX
- BULLET_ARROW_DIAMOND_DISC
- BULLET_STAR_CIRCLE_SQUARE
- NUMBERED_DECIMAL_ALPHA_ROMAN
- NUMBERED_DECIMAL_NESTED
- NUMBERED_UPPERALPHA_ALPHA_ROMAN

## Feeds into

- [Bullet Item Text](../Paragraph/CreateParagraphBulletsJson.md)'s Bullet Preset input
- [Bullet List](../Paragraph/BulletList.md)'s Bullet Preset input

## Notes

These values are literal Google Docs API `createParagraphBullets` bullet preset enum values. Using a string outside this list will cause the API to reject the request.
