/**
 * Curated emoji + colour palettes for the folder modal pickers. Kept as
 * a small handpicked set rather than wiring in an emoji-picker library:
 * folders are a low-volume artefact (a user typically creates 5–20 of
 * them), so the value of a thousands-strong picker is negligible vs. the
 * bundle cost of `emoji-mart` / `emoji-picker-element`.
 *
 * The text input in the modal is still there as an escape hatch — any
 * Unicode emoji the user pastes is saved as-is. The grid is just the
 * "fast path" for the common case.
 */

/**
 * Emoji palette. Order is curated by use-case so users find what they
 * want by scanning rather than searching:
 *  - organisation glyphs first
 *  - work / study
 *  - lifestyle / home
 *  - play / hobbies
 *  - body / health
 *  - travel
 *  - tech / projects
 *  - symbols / status
 */
export const FOLDER_EMOJI_PRESETS: readonly string[] = [
  // Organisation
  '📁', '📂', '🗂️', '📋', '📌', '🔖', '📔', '📕', '📗', '📘', '📙', '📚',
  // Work / study
  '💼', '💻', '🖥️', '📝', '📊', '📈', '🧠', '🎓', '🔬', '🧪',
  // Lifestyle / home
  '🏠', '🛏️', '🍽️', '🛒', '💰', '🧺', '🧹', '👕',
  // Play / hobbies
  '🎮', '🎯', '🎨', '🎵', '🎬', '📷', '🎲', '🏆', '🃏', '🎸',
  // Body / health
  '💪', '🏃', '🧘', '🥗', '🍎', '💊', '❤️', '🩺',
  // Travel
  '✈️', '🚗', '🏖️', '🗺️', '📍', '🧳',
  // Tech / projects
  '🚀', '⚡', '🔥', '💡', '🔧', '⚙️', '🛠️', '🔨', '📡',
  // Symbols / status
  '⭐', '🌟', '✨', '🎉', '✅', '❗', '🔔', '🌈', '⏰', '🌙',
];

/**
 * Colour swatches mapped to the Tailwind-ish palette already used
 * elsewhere in the app. The accent applies to the folder's left rail
 * and complete-button background — keep the contrast high enough on
 * the dark surface (which is why pure black/white isn't included).
 */
export interface FolderColorSwatch {
  readonly label: string;
  readonly value: string;
}

export const FOLDER_COLOR_PRESETS: readonly FolderColorSwatch[] = [
  { label: 'Blue (default)', value: '#60a5fa' },
  { label: 'Sky',            value: '#0ea5e9' },
  { label: 'Indigo',         value: '#6366f1' },
  { label: 'Purple',         value: '#a855f7' },
  { label: 'Pink',           value: '#ec4899' },
  { label: 'Rose',           value: '#fb7185' },
  { label: 'Red',            value: '#ef4444' },
  { label: 'Orange',         value: '#f97316' },
  { label: 'Amber',          value: '#fbbf24' },
  { label: 'Yellow',         value: '#eab308' },
  { label: 'Lime',           value: '#84cc16' },
  { label: 'Green',          value: '#10b981' },
  { label: 'Teal',           value: '#14b8a6' },
  { label: 'Cyan',           value: '#22d3ee' },
  { label: 'Slate',          value: '#94a3b8' },
];

/** Case-insensitive equality used to highlight the active swatch. */
export function colorsEqual(a: string | null | undefined, b: string | null | undefined): boolean {
  if (!a && !b) return true;
  if (!a || !b) return false;
  return a.trim().toLowerCase() === b.trim().toLowerCase();
}
