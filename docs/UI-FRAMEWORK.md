# GK2+ UI Framework

GK2+ UI is built through a small shared framework under
`src/GK2Plus/Framework/UI`. Feature code should register data/actions with
`GK2UIService` and avoid creating arbitrary Unity UI hierarchies itself.

## Goals

- Reuse Graveyard Keeper 2 fonts, sprites, text styles, item-cell visuals, and
  button language instead of shipping a visually separate UI skin.
- Keep sizes and placement tunable from one location.
- Pool/reuse runtime controls rather than destroying/recreating them during
  gameplay.
- Make new screens predictable: theme -> metrics -> factory -> view/controller.

## Core pieces

### `GK2UiTheme`

Resolves native GK2 UI assets once and caches them:

- Inventory/header text template
- building/list text template
- button text template
- window frame/background/divider/button sprites
- native item-cell slot sprite and icon material
- native item-count normal/red text styles
- reusable native item-requirement cells

The theme cache can be enriched later when additional native widgets have
loaded. It is reset when the UI service shuts down.

### `GK2UiMetrics`

Central source of logical UI sizes and spacing.

Use this file first when changing:

- mod-menu dimensions
- tab positions and spacing
- content viewport size
- feature-row spacing
- tracker panel width
- tracker title/body padding
- ingredient-cell size, icon size, and count font size

Avoid adding new magic dimensions directly to controllers when a reusable
metric makes sense.

### `GK2UiFactory`

Creates shared Unity UI primitives:

- RectTransform roots
- images/panels
- TMP labels copied from native templates
- legacy/shared action buttons
- exact native red SpriteSwap buttons
- native inspiration-style progress bars
- native tooltip containers, text, and separators
- flat category/tab buttons

### `GK2UiWindowBuilder`

Creates modal GK2+ window chrome:

- fullscreen overlay/dimmer
- top sorting canvas
- native frame/background
- content safe area

The F2 mod menu now uses this builder. Its frame interior uses the exact
`comm-frame_bg_1` + 13-unit inset recipe rather than the old title-screen
background approximation.

### `GK2UiListRowBuilder`

Creates a consistent settings/list row with:

- native list typography
- title + muted secondary description
- standard parent/child indentation
- right-aligned native action control
- optional expand/collapse behavior on parent rows
- optional transparent child rows when a parent group owns the shared panel
- centralized row/button sizing

Feature toggles and options in the F2 menu use this builder. Parent features with
sub-options render as a single expandable group rather than a stack of unrelated
boxes.

### `GK2UiSectionPanelBuilder`

Creates compact titled panels for HUD widgets and other small in-game surfaces.
The Tracker's Quests / Crafts / Items sections use this builder.

### `GK2UiItemRequirementBuilder`

Creates a reusable native-style item requirement cell with the game's item
sprite material/tint and enough/not-enough count styling. Quest Journal and
future recipe/build views can share this control.

### `GK2UiPool<T>`

Small reusable view pool. Runtime HUDs and future dynamic lists should use it
rather than destroying/recreating rows every refresh.

The tracker HUD uses pooled ingredient cells.

## Native GK2 v1.008 catalog

The reusable native primitives below were captured directly from live GK2
v1.008 UI with UnityExplorer. UnityExplorer is discovery tooling only; GK2+
does **not** depend on UnityExplorer or UniverseLib at runtime.

### Window frame

Native window stone is a two-layer composition:

- border: `comm-frame_1-border`, Sliced
- interior: `comm-frame_bg_1`, Simple
- interior inset: 13 logical UI units on all four sides

Do not use `titlescreen-menu-bg` as a generic window interior. It is not the
stone panel used by normal GK2 windows.

### Red action button

`GK2UiFactory.CreateNativeRedButton` uses the native SpriteSwap set:

- normal: `comm-btn-simple_red-active`
- hover/selected: `comm-btn-simple_red-over`
- pressed: `comm-btn-simple_red-press`
- disabled: `comm-btn-simple_red-inactive`

The native control is 162x26 in the pause menu, uses a Tiled background,
10-unit left/right text padding, 2-unit top padding, and
`small_font_bold` at 16.

### Item cell

The reusable item requirement cell now follows the native `UIItemCell`
geometry:

- logical cell: 42x42
- background: `widget_items_cell-inventory_item_cell`, Simple, 44x44
- item art: 48x48 with the native `ItemCellMaterial`
- selection: `selection`, Sliced
- inactive shade: `comm-item-inactive_shade`

The builder scales these native proportions when a feature requests a larger
or smaller logical cell.

### Inspiration progress bar

`GK2UiFactory.CreateNativeProgressBar` reproduces the vanilla inspiration
bar:

- frame: `inspiration-plate-green-value_frame`, Sliced
- fill: `inspiration-plate-green-bar_fill`, Sliced
- non-interactable Unity `Slider`, left-to-right, range 0..1
- fill is driven by the Slider's fill RectTransform, not Image.fillAmount
- label: `tiny_font`, 16, centered, using the native progress material

### Tooltip

`GK2UiFactory.CreateNativeTooltip` and its text/separator helpers reproduce
the native tooltip container:

- frame: `hint-frame`, Sliced, 20-unit sprite border
- padding: 14 on all sides
- spacing: 2
- minimum width: 60
- preferred width: 200
- separator: `hint-text_separator`, 96x6
- optional tail asset: `hint-frame_tail`, 10x10
- regular text: `small_font`, 16
- additional/meta text: `tiny_font`, 16
- native tooltip sorting order: 700

Tail corner/orientation behavior remains data in the theme until a feature
actually needs a directional tooltip pointer; no guessed rotation logic is
used.

### Confirmation dialog

`GK2UiDialogBuilder` reproduces the reusable `UIDialogWindow` shell:

- overlay shadow: black at 40% alpha
- sorting order: 415
- minimum window: 200x100
- GenericWindowLayout padding: left/right 24, top 47, bottom 23
- content spacing: 6
- body: `small_font`, 16, native `regular_text_3` material
- actions use the native red-button builder

### Main-window header/tab assets

The theme also caches the exact CharacterWindow header/tab primitives for
future builders:

- `main_window-header_1`
- `main_window-header_1-dec_side_3`
- `comm-header_frame_splitter`
- `main_window-header_1-button`
- `main_window-header_1-button-over`
- `main_window-header_1-dec_separator`

These are deliberately cataloged without inventing geometry that was not
captured.

## Controller rules

The persistent F2 menu supports both keyboard and controller access:

- keyboard shortcut is configurable; default is F2;
- hold L3 + R3 for 0.45 seconds to toggle the menu when controller access is enabled;
- B mirrors Esc for closing the top-most GK2+ picker/menu surface;
- opening from controller focuses the active native tab so Unity automatic navigation has a deterministic starting point.

Do not bind Steam Deck rear paddles directly. Players may map those through Steam Input, while GK2+ keeps its built-in shortcut portable across normal controllers and Steam Deck.

1. Controllers own state and lifecycle, not visual constants.
2. Pull visual assets from `GK2UiTheme`.
3. Pull dimensions from `GK2UiMetrics`.
4. Create controls through `GK2UiFactory` / builders.
5. Pool frequently updated rows.
6. Only rebuild hierarchy when structure changes. Update labels/counts/images
   in place for normal gameplay refreshes.
7. Feature modules should register through `GK2UIService` instead of reaching
   into menu controllers directly.

## Current migrations

### F2 mod menu

The F2 menu is the reference implementation for GK2+ native-style feature
configuration.

Its shell uses:

- `comm-frame_1-border` + `comm-frame_bg_1` for the window;
- the native CharacterWindow header/tab asset family;
- the exact native CharacterWindow tab-label template (`small_font_bold`, 16);
- exact native red SpriteSwap buttons;
- native section headers;
- native list/cell backgrounds;
- one scrollable body viewport shared by every top-level tab.

The current top-level tabs are:

~~~text
General
Inventory
Crafting
Farming
Movement
Tracker
Zombies
Cheats
More
~~~

#### Feature-page visual contract

New feature UI added to the F2 menu should follow this hierarchy unless a
feature has a documented reason not to:

~~~text
[ PAGE TITLE ]

short one-line page description

[ SECTION HEADER ]

[ expandable parent feature                           ON/OFF ]
  short muted description
    child option                                      value
    child option                                      value

[ another feature                                    ON/OFF ]
~~~

Rules:

1. **Use the existing page shell.** Do not create a second visual language,
   custom window skin, or feature-specific top-level menu.
2. **Keep page copy short.** Use one concise introductory line, then let native
   section headers and grouped controls carry the hierarchy.
3. **Use section headers for meaningful groups, not for every control.** Good
   examples are `Currency`, `Player`, `Item Spawning`, `Project Links`,
   or a feature family. Do not wrap every individual button in its own section.
4. **Parent features own their children.** If a feature has sub-options, the
   parent row keeps the ON/OFF control and the parent label area expands or
   collapses its children.
5. **Expanded parent + children share one dark native panel.** Child options do
   not draw separate embedded background boxes. Collapsing the parent shrinks
   the shared panel and removes the children's reserved vertical space.
6. **Collapse state is UI-only.** Expanding/collapsing does not change the
   feature/config state.
7. **Turning a parent OFF preserves child values.** Child settings remain part
   of the group and may be inspected when expanded, but controls that depend on
   the parent should be non-interactive while it is disabled.
8. **Descriptions belong in the row.** Parent rows should use a short muted
   secondary description instead of floating explanatory text elsewhere.
9. **Use native red buttons for explicit actions/values.** Keep labels concise:
   `ON`, `OFF`, `Spawn`, `Select`, `2x`, etc. Do not put sentences in
   buttons.
10. **Use native 26-unit red-button typography.** Do not scale the native
    `small_font_bold` label down with short button heights; resizing the text
    causes the font to look smeared.
11. **Group repeated actions.** Action-heavy pages such as Cheats should group
    related actions into a section/grid rather than presenting a vertical stack
    of unrelated buttons.
12. **Scroll only when content needs it.** Body height is content-driven and the
    scrollbar should stay hidden for pages that fit.
13. **Use framework primitives.** Theme assets come from `GK2UiTheme`,
    dimensions from `GK2UiMetrics`, and controls from the shared factories /
    builders. New feature modules should register with `GK2UIService` instead
    of drawing directly inside `ModMenuController`.
14. **Prefer native templates over reconstructed TMP styling.** When GK2 uses a
    styled TMP label, clone the proven native template so TextStyle/material
    behavior is preserved.

#### Current page organization

The current menu establishes these grouping patterns:

~~~text
General
  Features

Inventory
  Inventory Features

Crafting
  Crafting Features

Farming
  Farming Features

Movement
  Movement

Tracker
  Tracker Settings

Zombies
  Zombie Systems

Cheats
  Currency
  Player
  Item Spawning

More
  Project Links
  About
~~~

These labels can evolve with the feature set, but the grouping behavior above
is the default for future additions.

### RPG Quest Journal

The native Quests tab can now host a two-pane RPG journal built with the same
theme, metrics, pooling, scrolling, and item-requirement primitives. The
journal controller is presentation-only and falls back to the vanilla
`QuestTreePageWidget` if replacement initialization fails.

### Unified Tracker HUD

The HUD now uses:

- shared theme and metrics
- pooled material views
- cached native item slot/count/icon materials
- native blue-channel replacement for item icons
- content-driven panel sizing

The renderer skips hierarchy/layout work when the tracker snapshot has not
changed.

## Adding future UI

For a new feature screen or menu entry:

1. Register the feature/action with `GK2UIService`.
2. Put a feature toggle at the parent level when practical.
3. Declare child options with the parent feature ID so the menu can render them
   inside the same expandable group.
4. Add any reusable dimensions to `GK2UiMetrics`.
5. Use `GK2UiTheme.Current` / `Resolve` for native styling.
6. Build static controls with `GK2UiFactory` / the shared row/section
   builders.
7. Follow the **F2 feature-page visual contract** above.
8. Use `GK2UiPool<T>` for repeated/dynamic rows.
9. Prefer a dedicated controller/view class instead of extending
   `ModMenuController` with feature-specific rendering logic.

The long-term direction is to keep `ModMenuController` as navigation/window
orchestration while feature pages become independent reusable views.
