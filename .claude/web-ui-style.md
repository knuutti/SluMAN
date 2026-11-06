# Web UI style: tool pages that look like desktop software

The style SluMAN's skin editor (`SluMAN/SkinEditor.html`) was moved to in v1.1.7, written down so other web tools can follow it. The aim: a page that looks like the program it belongs to, a plain desktop application, and puts its features first. Dark only.

Screenshots of the editor before and after are in [knuutti/SluMAN#78](https://github.com/knuutti/SluMAN/pull/78).

## Principles

1. **It's a tool, not a landing page.** Open straight into the work area. No hero card, no marketing copy, no onboarding steps on the main screen.
2. **Desktop conventions over web ones.** Menu bar, toolbar, panels with captions, group boxes, status bar, modal dialogs with OK / Cancel. People already know where things are in these.
3. **Functionality is the decoration.** Colour only for meaning: selection, focus, errors, warnings. Everything else is neutral grey.
4. **Dense but readable.** Small text and compact controls, so more of the work fits on screen, as in a desktop app.
5. **Help is there when asked for.** Instructions go in a Help dialog (F1) and tooltips, not paragraphs that are always on screen.
6. **Change the look, not the behaviour.** When converting a page, keep its features, IDs and logic; restyle and move controls around them.

## What to remove

| Flashy pattern | Replace with |
|---|---|
| Rounded pills, chips, 6–12px corner radii | Square controls, 2px radius |
| Filled, coloured primary button (big blue Save) | A normal button; Ctrl+S |
| UPPERCASE letter-spaced section headings | Group box legends, or bold plain-case rows |
| Dimmed overlay with a dashed, rounded "get started" card | A plain modal dialog with a title bar and OK / Cancel |
| Numbered how-to steps and keyboard badges on screen | Help dialog with a two-column table |
| Status text squeezed into the header | Status bar at the bottom |
| Glows, coloured shadows, inset accent rings | 1px borders; accent border on the active panel |
| Gradients, large padding, airy spacing | Flat fills, 3–8px padding |
| Truncated labels ("L ...", "Squ...") from fixed-width chips | Tables with columns wide enough for full names |
| Light / dark switch, or following the system | Dark only, `color-scheme: dark` |

## Colours

Neutral greys in a few steps (window, panel, control, input), one blue for selection and focus, and muted status colours. Put them in CSS variables, and have canvas drawing code read the same variables (`getComputedStyle(...).getPropertyValue("--accent")`) so drawn overlays match the controls.

```css
:root {
  color-scheme: dark;                 /* native scrollbars, selects, number spinners go dark too */
  --window: #252526;                  /* page background, gaps between panels */
  --panel: #2d2d2d;                   /* panels, dialogs */
  --menu: #2b2b2b;                    /* menu bar, captions, title bars, status bar */
  --control: #3a3a3a; --control-hover: #454545;
  --input: #1e1e1e;                   /* text boxes, list boxes, pressed buttons */
  --canvas: #1b1b1b;                  /* drawing areas */
  --fg: #e0e0e0; --muted: #a0a0a0;
  --line: #454545; --line-soft: #383838;
  --accent: #3d8ee6; --accent-fg: #ffffff;  /* focus, selection outlines, active panel */
  --highlight: #094771;               /* selected row / hovered menu item */
  --danger: #f2765f; --warn: #e5b442; --info: #8fb4e3; --ok: #7cc47f;
}
```

- The accent is not a fill for buttons. It marks focus, the selection and the active panel.
- Selected rows and hovered menu items use `--highlight` (dark blue) with white text, as desktop list boxes do.
- Behind images that may be transparent, use a checkerboard of two close greys (`#222` / `#262626`, 12px), not a contrasting one.

## Type

- `font: 12px/1.4 "Segoe UI", Tahoma, Arial, sans-serif` everywhere, canvas labels included. Segoe UI is what Windows apps use; SluMAN's WinForms windows use 8.25pt Microsoft Sans Serif, about the same size.
- Bold only for panel captions, dialog headings and the selected item's name. No other sizes.
- `user-select: none` on the page, `user-select: text` on inputs, so dragging on the canvas doesn't select labels.
- Title Case for menu items, captions and dialog titles ("Open Skin", "Trim Crop"); sentence case for labels and messages. An ellipsis (…) on items that open a dialog or picker ("Open PNG…").

## Controls

```css
button, input[type="text"], input[type="number"], select {
  height: 22px; border: 1px solid var(--line); border-radius: 2px;
}
button { padding: 0 10px; background: var(--control); cursor: default; }
button:hover:not(:disabled) { background: var(--control-hover); }
button:active:not(:disabled), button[aria-pressed="true"] { background: var(--input); border-color: var(--accent); }
button:disabled { color: var(--muted); opacity: 0.55; }
input, select { background: var(--input); padding: 0 4px; }
input:focus, select:focus { outline: none; border-color: var(--accent); }
```

- 22px controls (20px in captions), `cursor: default` on buttons as in a desktop app, not a pointer hand.
- Toggle buttons (Live, a mode) use `aria-pressed` and look pressed in: dark fill, accent border.
- Disabled is greyed out, never hidden, so the layout doesn't jump.
- Checkboxes and radios are native, with `accent-color`.

## Layout

```
┌ Menu bar ─ File  Edit  View  Help ─────────────────────────────────┐
├ Toolbar ── [Open…] [Save] │ [Undo] [Redo] │ Name: [______] [x] … ──┤
├──────────┬───────────────────────┬───────────────────────┬─────────┤
│ Caption  │ Caption   62% [−][+]  │ Caption   62% [−][+]  │ Caption │
│ list /   │ canvas                │ canvas                │ group   │
│ table    │                       │                       │ boxes   │
├──────────┴───────────────────────┴───────────────────────┴─────────┤
│ Status message                         │ live state │ x, y │ zoom   │
└────────────────────────────────────────────────────────────────────┘
```

- **Page:** a CSS grid `auto auto 1fr auto` (menu bar, toolbar, work area, status bar) at `100vh`, so the work area fills the window and only panels scroll.
- **Panels:** flex columns with a 26px caption bar (bold title on the left, the panel's own buttons and zoom on the right) and a scrolling body. 3px gaps between panels on the window colour. The panel last clicked gets an accent border, and keyboard actions (arrow keys, zoom from the View menu) go to it.
- **Side panels** are fixed width (250px); the work panels share the rest.
- **Below about 1100px** the panels stack in one column. Fine for a desktop tool; it only has to stay usable.

## Menu bar and toolbar

- **Menus:** File, Edit, View, Help. Every action is in a menu, shortcut shown on the right in muted text, with separators between groups. The toolbar has only the most used ones.
- **One code path per action.** A menu item clicks the existing control (`data-run="saveButton"` → `$(id).click()`), and when the menu opens each item copies its control's `disabled` and `aria-pressed` state. Nothing has to be wired twice.
- Click a title to open its menu, hover across to switch while one is open, click elsewhere or press Escape to close.
- Global shortcuts (Ctrl+O, Ctrl+S, F1, Escape) are handled in a capture-phase `keydown` listener, so they also work while typing in a field. Other keys (arrows, Ctrl+Z) are ignored while focus is in a text box.
- Buttons whose only use is from the menu can stay in the page, hidden, so their IDs and handlers stay put.

## Panels and properties

- **Lists as tables:** a sticky header row, group names as bold muted rows, and state as a column (✓ / –), not coloured chips. Clicking a cell selects; selected is the highlight colour with an accent border.
- **Properties in `fieldset` / `legend` group boxes**, like WinForms GroupBoxes: one box per subject (selected item, settings, checks). Inside, a label/field grid (`auto 1fr auto 1fr`).
- **Validation as a list box:** a bordered, dark list with an icon per row (✖ error, ⚠ warning, ℹ info, in the status colours). Clicking a row selects what it's about.
- **Full names**, and enough context in names shown outside their group ("D-pad right pressed", not "Right pressed").

## Status bar

- 22px, at the bottom, on the menu colour. The message takes the remaining space on the left; fixed-width fields with left borders follow (connection or live state, pointer position, zoom).
- Messages replace each other; they don't stack or pop up as toasts. Errors in `--danger`, success in `--ok`, the rest muted. "Ready" when there's nothing to say.
- Long messages are cut with an ellipsis, so keep them short.

## Dialogs

- A modal box (`role="dialog"`) with a 28px title bar, title on the left and a ✕ on the right that turns red on hover. Footer buttons right-aligned, 75px minimum, the main one first (Open, then Cancel); a Help button may sit on the left.
- The overlay only darkens (`rgba(0,0,0,0.45)`), no blur.
- Escape closes the top dialog. A start dialog may be closed too, leaving an empty work area that still says what to do (for example a line of text drawn on the empty canvas).
- Lists of things to open are list boxes (`<select size="10">` with `<optgroup>`s), opened with a double-click, Enter or the Open button.
- Simple yes/no questions use the browser's own `confirm()`. It's plain and blocking, like a desktop message box.

## Help

- Help › How to Use and F1 open a dialog: short numbered steps for the main task, then a two-column table of mouse and keyboard controls, then notes.
- Each control's tooltip (`title`) says what it does and gives its shortcut.
- The main screen has no explanatory paragraphs. At most a one-line hint where something is empty or blocked.

## Converting a page: checklist

1. List every element ID and class the scripts use (`getElementById`, `querySelector`, `classList`, `className`) and keep them.
2. Replace the CSS with the variables and control styles above, dark only.
3. Rebuild the markup into menu bar, toolbar, captioned panels, group boxes and status bar. Put each action in a menu.
4. Move help text into a Help dialog, and turn status text and toasts into the status bar.
5. Replace landing or empty-state cards with a plain dialog.
6. Turn chips and badges into table columns or plain text, with full names.
7. Only touch the scripts where markup they build changed (lists, menus, status); leave the logic alone.
8. Check it at about 1600 and 1280 wide, with every menu open and every dialog shown, and watch the console for errors.
