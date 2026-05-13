#!/usr/bin/env node
/**
 * Generates `artifacts/mockup-sandbox/src/components/mockups/cm-editar/catalog.generated.json`
 * by parsing `src/CM.EDITAR.Templates/ExtensionCatalog.cs` (the C# source of truth).
 *
 * The C# file lists each entry on a single line with a stable shape:
 *   new(".ext", "Label", "Category", "state", "risk", "pack", null|"template", "Description"),
 *
 * We regex-match those lines and emit a JSON array consumed by the React mockup.
 * Run via: `pnpm generate:catalog`
 *
 * Correctness gates (any failure aborts with non-zero exit):
 *   1. Every `new(` constructor call in the entry array must successfully parse.
 *      A count mismatch means the regex skipped a malformed/multiline row —
 *      hard fail rather than silently shipping a smaller catalog.
 *   2. `state` must be one of {enabled, disabled, missing} and `risk` one of
 *      {rec, warn, high}.
 *   3. Every entry's `category` must appear in the C# `Categories` array.
 *   4. Categories in the JSON preserve C# declaration order — so the sidebar
 *      reflects the author's intent, not alphabetical happenstance.
 *   5. Every high-risk entry must be disabled by default (mirrors the xUnit
 *      invariant in ExtensionCatalogTests.cs).
 */
import { readFileSync, writeFileSync, mkdirSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = resolve(__dirname, "..");
const SRC = resolve(ROOT, "src/CM.EDITAR.Templates/ExtensionCatalog.cs");
const OUT = resolve(
  ROOT,
  "artifacts/mockup-sandbox/src/components/mockups/cm-editar/catalog.generated.json",
);

const text = readFileSync(SRC, "utf8");

function fail(msg) {
  console.error(`[generate-catalog] FAILED: ${msg}`);
  process.exit(1);
}

// --- 1. Extract the wired Categories array straight from the C# source so
//        the JSON preserves declaration order (gate #4).
const categoriesBlock = text.match(
  /Categories\s*=\s*new\[\]\s*\{([\s\S]*?)\};/,
);
if (!categoriesBlock) fail("could not locate `Categories = new[] { ... };` in source");
const wiredCategories = [...categoriesBlock[1].matchAll(/"([^"]+)"/g)].map(
  (m) => m[1],
);
if (wiredCategories.length === 0) fail("Categories array parsed empty");
const wiredCategorySet = new Set(wiredCategories);

// --- 2. Isolate the `All = new Entry[] { ... };` block so we count `new(` only
//        inside the entry array (avoids accidentally counting unrelated `new`s).
const allBlock = text.match(/All\s*=\s*new\s+Entry\[\]\s*\{([\s\S]*?)\n\s*\};/);
if (!allBlock) fail("could not locate `All = new Entry[] { ... };` in source");
const allBody = allBlock[1];

// Count every `new(` constructor call in the entry array — this is the ground
// truth for how many entries the parser must produce.
const ctorCount = (allBody.match(/\bnew\(/g) ?? []).length;
if (ctorCount === 0) fail("no `new(` entries found inside `All = new Entry[] { ... }`");

// --- 3. Parse each entry. Allow escaped quotes inside string fields and
//        surrounding whitespace; the template field is `null` or a string.
const STR = `"((?:[^"\\\\]|\\\\.)*)"`;
const NULLABLE = `(?:null|${STR})`;
const RE = new RegExp(
  `new\\(\\s*${STR}\\s*,\\s*${STR}\\s*,\\s*${STR}\\s*,\\s*${STR}\\s*,\\s*${STR}\\s*,\\s*${STR}\\s*,\\s*${NULLABLE}\\s*,\\s*${STR}\\s*\\)`,
  "g",
);

const ALLOWED_STATES = new Set(["enabled", "disabled", "missing"]);
const ALLOWED_RISKS = new Set(["rec", "warn", "high"]);

const entries = [];
const errors = [];
let m;
let idx = 0;
while ((m = RE.exec(allBody)) !== null) {
  // Capture groups: 1 ext, 2 label, 3 category, 4 state, 5 risk, 6 pack,
  // 7 template (undefined when null), 8 description.
  const [, ext, label, category, state, risk, pack, template, description] = m;

  if (!ALLOWED_STATES.has(state))
    errors.push(`${ext}: invalid state "${state}" (allowed: enabled|disabled|missing)`);
  if (!ALLOWED_RISKS.has(risk))
    errors.push(`${ext}: invalid risk "${risk}" (allowed: rec|warn|high)`);
  if (!wiredCategorySet.has(category))
    errors.push(`${ext}: category "${category}" is not in the wired Categories array`);

  entries.push({
    id: `e${++idx}`,
    ext,
    label,
    category,
    state,
    risk,
    pack,
    template: template ?? null,
    description,
  });
}

// --- 4. Hard fail if the parser dropped any rows (gate #1).
if (entries.length !== ctorCount) {
  fail(
    `parse-count mismatch: regex matched ${entries.length} entries but the source ` +
      `contains ${ctorCount} \`new(\` constructor calls inside the entry array. ` +
      `Some row likely drifted from the single-line shape — fix the formatting and rerun.`,
  );
}

// --- 5. Surface enum/category violations together for fast diagnosis.
if (errors.length > 0) {
  console.error("[generate-catalog] FAILED: invalid rows detected:");
  for (const e of errors) console.error(`  - ${e}`);
  process.exit(1);
}

// --- 6. Mirror the xUnit invariant — high-risk must ship disabled.
const violators = entries.filter((e) => e.risk === "high" && e.state !== "disabled");
if (violators.length > 0) {
  fail(
    "high-risk entries must be disabled by default: " +
      violators.map((v) => `${v.category}::${v.ext}`).join(", "),
  );
}

// --- 7. Build per-category counts in C#-declared order (gate #2/#4) so the
//        mockup sidebar honours the author's intent, not alphabetical sort.
const countsByCategory = {};
for (const cat of wiredCategories) countsByCategory[cat] = 0;
for (const e of entries) countsByCategory[e.category]++;

const uniqueExts = new Set(entries.map((e) => e.ext));

mkdirSync(dirname(OUT), { recursive: true });
writeFileSync(
  OUT,
  JSON.stringify(
    {
      // NOTE: intentionally NO generatedAt — the file is generated, but it
      // must be byte-stable for a given ExtensionCatalog.cs so the CI catalog
      // drift gate (`git diff --quiet -- catalog.generated.json`) is meaningful.
      source: "src/CM.EDITAR.Templates/ExtensionCatalog.cs",
      totalEntries: entries.length,
      uniqueExtensions: uniqueExts.size,
      categories: wiredCategories, // declaration-order, sourced from C#
      countsByCategory,
      entries,
    },
    null,
    2,
  ) + "\n",
);

console.log(
  `[generate-catalog] wrote ${entries.length} entries (${uniqueExts.size} unique extensions) -> ${OUT}`,
);
console.log("[generate-catalog] categories (declared order):", wiredCategories.join(", "));
console.log("[generate-catalog] counts:", countsByCategory);
