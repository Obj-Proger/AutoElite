# Noir Modern

The design system of the AutoElite mobile application and website: one look on every screen.

Graphite and champagne gold, with a geometric display face for headings and figures and a neutral grotesque for
everything else: premium without being old-fashioned. The interface is dark; a light theme of warm ivory and stone with
soft gold keeps the same look by day. The application follows the theme of the device unless the user chooses one in
the profile.

## Tokens

[`tokens.json`](tokens.json) holds every color, font, size, spacing and radius, in the format of the
[W3C Design Tokens Community Group](https://www.designtokens.org/). It is the source of truth: the clients copy its
values (the mobile application in `src/AutoElite.Mobile/Resources/Styles/Colors.xaml` and `ColorsLight.xaml`), and the
architecture tests check that the copies match, so a change starts here. The group `color` is the dark theme and
`colorLight` the light one, with the same names.

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
| accentText | `#C9A96E` | Gold text and icons on a surface: labels in capitals, links, the current day |
| primary / primaryPressed | `#C9A96E` / `#E2C891` | The main button of a screen, and pressed |
| onPrimary | `#0E0F11` | Text on the main button |
| highlight / highlightBorder | `#23252A` / `#2A2C31` | The highlighted card: what matters most on a screen now |
| success / warning / danger | `#86C29A` / `#E0B661` / `#E5867A` | States |

The light theme puts warm ivory behind white cards, stone (`#ECE7DE`) under the highlighted card and soft gold
(`#E2D1AA`) on the main button; the gold of text turns dark (`#7A5C24`) and the states deeper, so that every text still
reads at 4.5:1. The gold of fills, badges and the active tab stays `#C9A96E` in both themes.

Every text color has a contrast of at least 4.5:1 on the background and on every surface of its theme (WCAG AA).

## Type

- **Unbounded** (500, 600) for headings and large figures: display 44, headline 28, title 20.
- **Inter** (400, 500, 600) for body text, labels and numbers: body large 17, body 15, label 13, caption 12. Labels
  in capitals are tracked wide.

Both are under the SIL Open Font License 1.1 ([Inter](fonts/Inter-OFL.txt), [Unbounded](fonts/Unbounded-OFL.txt)).
The files in [`fonts`](fonts) are static instances of the variable fonts of Google Fonts at those weights (Inter at
an optical size of 14), so that every platform shows the weight it is asked for.

## Icons

Outlines on a grid of 24 with round ends, 1.8 thick, drawn after [Lucide](https://lucide.dev) (ISC License,
[`icons/Lucide-LICENSE.txt`](icons/Lucide-LICENSE.txt)): the mobile application has them by name in
`Controls/AppIcons.cs` and draws them with `AppIcon`. An icon takes the color of the text it goes with, or of the tone
of its tile.

## Components

The screens are built from a few parts of `src/AutoElite.Mobile/Controls`, so that the same thing looks the same
everywhere:

- **ListRow**: one thing of a list as a card, with an icon in a tile of its tone, a title over a line of details, a
  short value at the end and an arrow when it opens something.
- **StatusPill**: a short status in the color of its tone instead of a line of text.
- **CountBadge**: a count of new things in gold, up to 99, then 99+.
- **Avatar**: a photo or initials in a circle, gold for the user themselves.
- **StatTile**: a figure with what it counts and, as part of a whole, a bar.
- **EmptyState**: an icon, why a list is empty and what to do about it.
- **NoticeBanner**: the notice at the top of a page, such as that it shows what was saved on the device.

A tone says what a thing tells: neutral, accent (soon, now, waiting), success, warning or danger. Its color tints the
background of a tile, a pill or a notice and colors its icon and text.

## Shapes

Spacing in steps of 4 (4, 8, 12, 16, 24, 32), 22 at the sides of a phone screen. Radii of 14 for buttons and fields,
18 for cards and 22 for the highlighted card. Buttons and fields are 48 high, the smallest touch target.
