# Sidekick UI findings, 2026-07-30

From a screenshot of the running app plus source inspection. Two of these land in every launch
screenshot, which matters because "zero screenshots exist" is a listed blocker in the Warden ship plan
and Sidekick is the headline Pro feature inside Warden.

What already works: the severity cards, the score ring, and the table chrome read as one system, and
the palette matches Warden.

---

## 1. The wordmark asset is horizontally mirrored

**File:** `src/RevenantWorkspaceSidekick.Ui/Assets/RWS_Wordmark.png` (1.5 MB, modified 2026-07-29 19:23)
**Referenced at:** `src/RevenantWorkspaceSidekick.Ui/MainWindow.xaml:97-100`, `Height="62"`

Every letter in the image is flipped left to right. "REVENANT" renders as a reversed R followed by
backwards letterforms. "WORKSPACE" the same. The "REVENANT SYSTEMS" line and the split "SIDE" / "KICK"
blocks on the right are all mirrored too. At 62 pixels tall in the header it stops reading as words and
reads as texture, which in the running app looks like a double exposure.

**A single horizontal flip will not fix it.** Flipping the canvas also swaps block order: "SIDE / KICK /
REVENANT SYSTEMS" would land on the left and "REVENANT WORKSPACE" on the right. The blocks have to be
flipped individually and re-laid out.

The XAML is correct and references exactly one image, so this is entirely an asset problem. Fastest path
is the Krita source if it still exists. Dave does all real art, so this one is his.

---

## 2. Export buttons are clipped

**File:** `src/RevenantWorkspaceSidekick.Ui/MainWindow.xaml:362-371` (buttons), `:242-247` (right rail)

`Export JSON` and `Export HTML` sit inside the severity `Border`. The right rail is a three row grid,
`Auto` / `Auto` / `*`, with **no `ScrollViewer` anywhere in it**. At the shipped window height the
combined content overflows: the buttons are cut mid-glyph and the SELECTED FINDING panel at `:375` is
pushed off screen entirely.

Window is `Height="820" MinHeight="620"` (`:6-7`), so anyone who resizes downward hits this harder.

**Fix:** wrap the right rail contents in a `ScrollViewer`, or move the export row out of the severity
panel into its own row. Code change, small.

---

## 3. The score reads 0/100 before any scan has run

Ring shows a dash, score shows a hard zero, and all four severity cards show 0 with no scan performed.
A tool whose entire job is grading code should not greet the user with what looks like its own failing
grade.

**Fix:** distinguish null from zero in the view model. Empty state should read "not scanned" rather than
a numeric zero.

---

## 4. Minor: no empty state on the findings grid

The FINDINGS `DataGrid` is a large empty void with column headers and nothing else. It should carry a
line telling the user to pick a folder and press SCAN.

---

## Suggested order

Items 2 and 3 are code and small, and both are visible in any screenshot. Item 4 is a nice-to-have in
the same pass. Item 1 is art and belongs to Dave, but it is the most damaging of the four, because it
is at the top of every frame.
