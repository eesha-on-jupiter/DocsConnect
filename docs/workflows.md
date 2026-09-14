# Workflows

Component chains for the things people usually build. Components only — see each component's page
for its inputs. `─▶` is a wire; `▶ RJ` means "into Request Aggregator's Request JSON input"; a
`Merge` is the Grasshopper Merge component (slot order = reading order).

Every workflow ends the same way: content blocks go into **Request Aggregator**, its `Requests JSON`
goes into **Batch Update**, and Batch Update is clicked.

---

## 1. First document

```
Auth (check the token)

Create Document ──DID──────────────────────────────────────┐
                                                           ▼
Heading Text ─┐                                       Batch Update
Insert Text  ─┼─▶ Request Aggregator ──RQ────────────▶  (click)
Bullet List  ─┘
```

Same thing with a Merge, which is easier to reorder:

```
Heading Text ─▶ D1 ┐
Insert Text  ─▶ D2 ├─ Merge ─▶ Request Aggregator ─▶ Batch Update
Bullet List  ─▶ D3 ┘
```

---

## 2. One styled block (chaining)

Chain traits onto the *same* text instead of inserting it several times.

```
Heading Text ─RJ▶ Text Color ─RJ▶ Aligned Text ─RJ▶ Space Below ─▶ Request Aggregator ─▶ Batch Update
```

Works on composites too:

```
Report Skeleton ─RJ▶ Font Family ─▶ Request Aggregator
```

---

## 3. Report: title, body, nested list

```
Heading Text (H1)            ─▶ D1 ┐
Insert Text (paragraph)      ─▶ D2 ├─ Merge ─▶ Request Aggregator ─▶ Batch Update
Bold Text ("Details:")       ─▶ D3 │
Bullet List (Indent Levels)  ─▶ D4 ┘
```

Use **Insert Text** for plain paragraphs — **Bullet Item Text** bullets every line it is given.

---

## 4. Rhino viewports into the document

```
Panel (view names) ─▶ Capture Rhino View ─FP▶ Get Image URL ─IU▶ Insert Image ─▶ Request Aggregator ─▶ Batch Update
                        (click)               (click, needs drive.file)
```

Images land one per line. Put text around them with a Merge:

```
Heading Text  ─▶ D1 ┐
Insert Image  ─▶ D2 ├─ Merge ─▶ Request Aggregator
Insert Text   ─▶ D3 ┘
```

An image that is already online skips the first two components: **Get Image URL** (URL input) → **Insert Image**.

---

## 5. Table from Grasshopper data

Rows as branches:

```
Entwine (one list per row) ─▶ Data Tree To Table ─▶ Request Aggregator ─▶ Batch Update
```

Columns as branches — the usual shape of Grasshopper data (one list per attribute):

```
Labels ─┐
Values ─┼─ Entwine ─▶ Data Tree To Table (Transpose) ─▶ Request Aggregator
Units  ─┘
```

Fixed shape regardless of data:

```
flat list of cells ─▶ Fixed Size Table (Rows, Columns) ─▶ Request Aggregator
```

---

## 6. Coloured / formatted table

Values coloured by a gradient, labels beside them:

```
Values ─▶ Text Color ─┐
                      ├─ Entwine ─▶ Data Tree To Table (Transpose) ─▶ Request Aggregator
Labels ───────────────┘
```

Any Text/Paragraph component's output works as cells the same way (Bold Text, Font Size, a chain).

Formatting as matching trees — bold header, filled status column:

```
Data tree  ─▶ D  ┐
Bold tree  ─▶ B  ├─ Data Tree To Table ─▶ Request Aggregator
Fill tree  ─▶ FL ┘
```

---

## 7. Headers and footers

The segment is created live first (its ID is server-generated); content follows through its **own**
Request Aggregator, one per segment.

```
Create Header (click) ─HID─┐
                           ▼ Segment ID
Insert Text ─────────────────▶ Request Aggregator (header) ─┐
Aligned Text ────────────────▶                              │
                                                            ├─▶ Batch Update
Create Footer (click) ─FID─┐                                │
                           ▼ Segment ID                     │
Insert Text ─────────────────▶ Request Aggregator (footer) ─┤
                                                            │
Heading Text ─┐                                             │
Insert Text  ─┴─────────────▶ Request Aggregator (body) ────┘
```

A footnote is the same shape with **Create Footnote**.

---

## 8. Append below existing content

```
Get Document ─End Index─▶ Request Aggregator (Start Index)
                            ▲
Insert Text ────────────────┘                ─▶ Batch Update
```

---

## 9. Rebuild the document on every run

```
Clear Document (click)   then   blocks ─▶ Request Aggregator ─▶ Batch Update (click)
```

---

## 10. Find a document by name

```
Panel (title) ─▶ Get Document (Name) ─DID▶ Batch Update / Clear Document / Create Header …
                 (needs drive.readonly)
```

---

## 11. Swap an image already in the document

```
Get Document ─(image object ID)─▶ Replace Image ─▶ Request Aggregator (Fixed Requests) ─▶ Batch Update
                                       ▲
Get Image URL ─IU──────────────────────┘
```

---

## 12. Page setup

```
Update Document Style ─▶ Request Aggregator (Fixed Requests) ─▶ Batch Update
```

Replace All Text goes the same way. **Fixed Requests** is only for requests that carry no position or
their own real indices — a content block wired there is refused.

---

## 13. Several sections with page breaks

```
Heading Text        ─▶ D1 ┐
Insert Text         ─▶ D2 │
Insert Page Break   ─▶ D3 ├─ Merge ─▶ Request Aggregator ─▶ Batch Update
Heading Text        ─▶ D4 │
Data Tree To Table  ─▶ D5 ┘
```
