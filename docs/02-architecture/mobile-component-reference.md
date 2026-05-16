# Mobile component reference

The Angular app has a generated reference site built with [**Compodoc**](https://compodoc.app/). It documents:

- Every standalone component, its inputs/outputs, and the template it renders.
- Every service, its public surface, and its dependencies.
- The router graph (which routes load which components).
- A documentation **coverage report** that flags undocumented public APIs.

This complements [Swagger](api-reference.md): Swagger is the server-side contract, Compodoc is the client-side contract.

## Generating the site

The output is **not committed** (it lives under `docs/generated/` which is gitignored — regenerate it whenever you want a fresh view):

```bash
cd src/LuminaPath.MobileApp
npm run docs:compodoc           # build static site into docs/generated/mobile/
```

Then open `docs/generated/mobile/index.html` in a browser.

## Live-reloading while writing TSDoc comments

```bash
cd src/LuminaPath.MobileApp
npm run docs:compodoc:serve     # serves at http://localhost:4567 with HMR
```

Compodoc watches your source files; saving a `.ts` with a new comment refreshes the browser.

## How to write good entries

Compodoc reads **standard TSDoc** above classes, methods, properties and components. The minimum useful contract:

````typescript
/**
 * One sentence: what does this thing exist to do?
 *
 * More detail if needed — preconditions, edge cases, side effects.
 *
 * @example
 * ```typescript
 * const url = mediaImageUrl(game.image);
 * ```
 */
export function mediaImageUrl(file: MediaFile | null): string { ... }
````

For Angular components, document the `@Input()` and `@Output()` you expose — those are the public contract.

## Coverage budget

The generated `coverage.html` shows a percentage. We aren't enforcing a CI threshold yet, but the rule of thumb is:

- **Public services**: every public method documented.
- **Components**: every `@Input` / `@Output` documented; private template helpers can be skipped.
- **Models / DTOs**: not worth documenting — the type signature is the doc.

## When to (re)generate

- Before opening a PR that adds a new component or service, run `npm run docs:compodoc` and skim the coverage page.
- Don't commit the output. CI can regenerate it for previews if/when we set up GitHub Pages.
