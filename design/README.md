# Noir Modern

The design system of the AutoElite mobile application and website: one look on every screen.

Graphite and champagne gold, with a geometric display face for headings and figures and a neutral grotesque for
everything else: premium without being old-fashioned. The interface is dark.

## Tokens

[`tokens.json`](tokens.json) holds every color, font, size, spacing and radius, in the format of the
[W3C Design Tokens Community Group](https://www.designtokens.org/). It is the source of truth: the clients copy its
values (the mobile application in `src/AutoElite.Mobile/Resources/Styles/Colors.xaml`), and the architecture tests check
that the copies match, so a change starts here.

| Color | Value | Use |
|---|---|---|
| background | `#0E0F11` | The page behind everything |
| surface | `#1A1C20` | Cards, fields and panels |
| surfaceRaised | `#23252A` | A surface above a surface |
| border | `#2A2C31` | Hairlines, dividers, progress tracks |
| text | `#EDEAE3` | Body text and headings |
| textSecondary | `#B7B2A8` | Supporting text |
| textMuted | `#979288` | Labels and captions |
| accent | `#C9A96E` | Primary actions, highlights, the active tab |
| accentHover | `#E2C891` | The accent pressed or under the pointer |
| onAccent | `#0E0F11` | Text and icons on the accent |
| success / warning / danger | `#86C29A` / `#E0B661` / `#E5867A` | States |

Every text color has a contrast of at least 4.5:1 on the background and on every surface (WCAG AA).

## Type

- **Unbounded** (500, 600) for headings and large figures: display 44, headline 28, title 20.
- **Inter** (400, 500, 600) for body text, labels and numbers: body large 17, body 15, label 13, caption 12. Labels
  in capitals are tracked wide.

Both are under the SIL Open Font License 1.1 ([Inter](fonts/Inter-OFL.txt), [Unbounded](fonts/Unbounded-OFL.txt)).
The files in [`fonts`](fonts) are static instances of the variable fonts of Google Fonts at those weights (Inter at
an optical size of 14), so that every platform shows the weight it is asked for.

## Shapes

Spacing in steps of 4 (4, 8, 12, 16, 24, 32), 22 at the sides of a phone screen. Radii of 14 for buttons and fields,
18 for cards and 22 for the highlighted card. Buttons and fields are 48 high, the smallest touch target.
