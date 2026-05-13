import { useEffect, useRef, useState } from "react";
import {
  Search,
  ShieldCheck,
  ShieldAlert,
  HelpCircle,
  ChevronRight,
  FileText,
  FileCode,
  FileArchive,
  FileSpreadsheet,
  FileJson,
  Terminal,
  File as FileIcon,
  FileType,
  Globe,
  Palette,
  ScrollText,
  Layers,
  PlayCircle,
  RotateCcw,
  Undo2,
  Trash2,
  Upload,
  Download,
  Plus,
  X,
  Check,
  AlertTriangle,
  Info,
  Wrench,
  PencilLine,
  Eye,
  Sparkles,
  Link2,
  Save,
  FolderOpen,
  BookOpen,
  Heart,
  Github,
  Coffee,
  ExternalLink,
  Settings,
  Keyboard,
  LifeBuoy,
  Power,
  ListChecks,
  Eraser,
  RefreshCw,
  Camera,
  History,
  Globe2,
  Sun,
  Moon,
  Monitor,
} from "lucide-react";

const APPLY_WARNING_DISMISSED_KEY = "cm-editar:apply-warning-dismissed";

type RiskLevel = "rec" | "warn" | "high";
type EntryState = "enabled" | "disabled" | "missing" | "pending";

interface ShellNewEntry {
  id: string;
  ext: string;
  label: string;
  group: string;
  state: EntryState;
  risk: RiskLevel;
  description: string;
  template?: string;
  icon: typeof FileText;
  pack?: string;
  queued?: "enable" | "disable" | "edit" | null;
}

// =============================================================================
// Catalog wiring — entries come from `catalog.generated.json`, which is built
// from `src/CM.EDITAR.Templates/ExtensionCatalog.cs` (the single source of
// truth) by `pnpm generate:catalog`. Never hand-edit the JSON.
// =============================================================================
import catalogJson from "./catalog.generated.json";

interface CatalogJsonEntry {
  id: string;
  ext: string;
  label: string;
  category: string;
  state: string;
  risk: string;
  pack: string;
  template: string | null;
  description: string;
}

interface CatalogJson {
  generatedAt: string;
  source: string;
  totalEntries: number;
  uniqueExtensions: number;
  categories: string[];
  countsByCategory: Record<string, number>;
  entries: CatalogJsonEntry[];
}

const CATALOG = catalogJson as unknown as CatalogJson;

// One icon per wired category — picked so the dense sidebar reads at a glance.
const CATEGORY_ICONS: Record<string, typeof FileText> = {
  "Archives": FileArchive,
  "Automation/Data": Sparkles,
  "CAD/3D": Layers,
  "Cloud Docs": Globe,
  "Legacy": ScrollText,
  "Media": Palette,
  "Office/Docs": FileSpreadsheet,
  "Omega Database": FileJson,
  "Power User": Terminal,
  "System": Wrench,
  "Text/Data": FileText,
};

// A few illustrative pending-queue overlays so the lifecycle column still has
// something to demo. Mockup-only — the real app derives this from the registry.
const SEEDED_QUEUED: Record<string, ShellNewEntry["queued"]> = {
  ".md": "edit",
  ".csv": "enable",
  ".py": "edit",
  ".(blank)": "enable",
};

const ENTRIES: ShellNewEntry[] = CATALOG.entries.map((e) => ({
  id: e.id,
  ext: e.ext,
  label: e.label,
  group: e.category,
  state: e.state as EntryState,
  risk: e.risk as RiskLevel,
  description: e.description,
  template: e.template ?? undefined,
  icon: CATEGORY_ICONS[e.category] ?? FileText,
  pack: e.pack,
  queued: SEEDED_QUEUED[e.ext] ?? null,
}));

// Sidebar = "All" + every wired category (with live counts) + cross-cutting
// state filters. The category list is derived from the JSON so adding a new
// category to ExtensionCatalog.cs surfaces here automatically.
const CATEGORIES = [
  { id: "all", label: "All Extensions", count: ENTRIES.length, icon: Layers },
  ...CATALOG.categories.map((cat) => ({
    id: `cat:${cat}`,
    label: cat,
    count: ENTRIES.filter((e) => e.group === cat).length,
    icon: CATEGORY_ICONS[cat] ?? FileText,
  })),
  { id: "missing", label: "Missing / Broken", count: ENTRIES.filter((e) => e.state === "missing").length, icon: AlertTriangle },
  { id: "disabled", label: "Disabled", count: ENTRIES.filter((e) => e.state === "disabled").length, icon: X },
];

// A–Z view groups rows under one sticky header per first-character of the
// extension (after the leading dot). Letters use their own header (A, B, …),
// digits use their own (0, 1, …), and any non-alphanumeric character collapses
// under a single "#" Symbols header. We also expose a coarse bucket so the
// header strip can show a section label (Symbols / Numerical / Alphabetical).
type AzBucket = "Symbols" | "Numerical" | "Alphabetical";
interface AzKey {
  bucket: AzBucket;
  header: string;
}
function azKey(ext: string): AzKey {
  const c = ext.replace(/^\./, "").charAt(0).toLowerCase();
  if (c >= "0" && c <= "9") return { bucket: "Numerical", header: c };
  if (c >= "a" && c <= "z") return { bucket: "Alphabetical", header: c.toUpperCase() };
  return { bucket: "Symbols", header: "#" };
}

function StateBadge({ state }: { state: EntryState }) {
  const styles: Record<EntryState, string> = {
    enabled: "bg-emerald-500/10 text-emerald-300 border-emerald-500/30",
    disabled: "bg-zinc-500/10 text-zinc-400 border-zinc-500/30",
    missing: "bg-amber-500/10 text-amber-300 border-amber-500/30",
    pending: "bg-sky-500/10 text-sky-300 border-sky-500/30",
  };
  const labels: Record<EntryState, string> = {
    enabled: "ENABLED",
    disabled: "DISABLED",
    missing: "MISSING",
    pending: "PENDING",
  };
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded text-[10px] font-mono font-semibold tracking-wider border ${styles[state]}`}>
      {labels[state]}
    </span>
  );
}

function RiskBadge({ risk }: { risk: RiskLevel }) {
  const styles: Record<RiskLevel, string> = {
    rec: "bg-emerald-500/10 text-emerald-300 border-emerald-500/30",
    warn: "bg-amber-500/10 text-amber-300 border-amber-500/30",
    high: "bg-rose-500/10 text-rose-300 border-rose-500/30",
  };
  const labels: Record<RiskLevel, string> = {
    rec: "REC",
    warn: "WARN",
    high: "HIGH",
  };
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded text-[10px] font-mono font-semibold tracking-wider border ${styles[risk]}`}>
      {labels[risk]}
    </span>
  );
}

function QueueBadge({ queued }: { queued: ShellNewEntry["queued"] }) {
  if (!queued) return <span className="text-zinc-600">—</span>;
  const map = {
    enable: { label: "+ Enable", cls: "text-emerald-300" },
    disable: { label: "− Disable", cls: "text-rose-300" },
    edit: { label: "✎ Edit", cls: "text-sky-300" },
  };
  const m = map[queued];
  return <span className={`text-[11px] font-mono font-semibold ${m.cls}`}>{m.label}</span>;
}

function Card({ title, icon: Icon, children, accent }: { title: string; icon: typeof FileText; children: React.ReactNode; accent?: boolean }) {
  return (
    <div className={`rounded-md border ${accent ? "border-[#C87533]/40 bg-gradient-to-b from-[#C87533]/[0.04] to-transparent" : "border-zinc-800/80 bg-zinc-900/40"} backdrop-blur-sm`}>
      <div className="flex items-center gap-2 px-3 py-2 border-b border-zinc-800/80">
        <Icon className="w-3.5 h-3.5 text-[#C87533]" />
        <h3 className="text-[11px] font-semibold uppercase tracking-[0.12em] text-zinc-300">{title}</h3>
      </div>
      <div className="p-3">{children}</div>
    </div>
  );
}

function FooterButton({
  label,
  icon: Icon,
  variant = "default",
  onClick,
}: {
  label: string;
  icon: typeof FileText;
  variant?: "default" | "primary" | "go" | "warn" | "danger";
  onClick?: () => void;
}) {
  const styles = {
    default: "bg-zinc-800/60 hover:bg-zinc-700/70 border-zinc-700/70 text-zinc-200",
    primary: "bg-[#C87533] hover:bg-[#d28344] border-[#C87533] text-zinc-950 shadow-[0_0_18px_-4px_rgba(200,117,51,0.55)]",
    // Bright contrastic "GO" green — high-saturation traffic-light cue for the
    // primary destructive-but-safe action (Apply Changes).
    go: "bg-[#22c55e] hover:bg-[#16a34a] border-[#16a34a] text-zinc-950 font-bold shadow-[0_0_22px_-2px_rgba(34,197,94,0.75)] ring-1 ring-emerald-300/50",
    warn: "bg-amber-600/20 hover:bg-amber-600/30 border-amber-600/40 text-amber-200",
    danger: "bg-rose-600/15 hover:bg-rose-600/25 border-rose-600/40 text-rose-200",
  };
  return (
    <button
      onClick={onClick}
      className={`inline-flex items-center gap-2 px-3 py-1.5 rounded border text-xs font-semibold transition ${styles[variant]}`}
    >
      <Icon className="w-3.5 h-3.5" />
      {label}
    </button>
  );
}

function TemplateManagerModal({ onClose }: { onClose: () => void }) {
  const templates = [
    { id: "frontmatter.md", name: "frontmatter.md", ext: ".md", maps: 1 },
    { id: "main.py", name: "main.py", ext: ".py", maps: 1 },
    { id: "object.json", name: "object.json", ext: ".json", maps: 1 },
    { id: "headers.csv", name: "headers.csv", ext: ".csv", maps: 1 },
    { id: "html5.html", name: "html5.html", ext: ".html", maps: 1 },
    { id: "blank.docx", name: "blank.docx", ext: ".docx", maps: 1 },
  ];
  const [active, setActive] = useState("frontmatter.md");
  const [body, setBody] = useState(
    `---\ntitle: {{filename}}\ndate: {{date}}\nauthor: {{user}}\ntags: []\n---\n\n# {{filename}}\n\nCreated {{date}} by {{user}} on {{host}}.\n\n`,
  );

  return (
    <div className="absolute inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-6">
      <div className="w-full max-w-[1280px] h-[640px] rounded-lg border border-zinc-700/80 bg-[#15171a] shadow-2xl shadow-black/60 flex flex-col overflow-hidden">
        {/* Modal header */}
        <div className="flex items-center justify-between px-4 py-2.5 border-b border-zinc-800 bg-zinc-900/60">
          <div className="flex items-center gap-2">
            <Layers className="w-4 h-4 text-[#C87533]" />
            <h2 className="text-sm font-semibold text-zinc-100 tracking-wide">Template Manager</h2>
            <span className="text-[10px] font-mono text-zinc-500 px-2 py-0.5 rounded border border-zinc-800">
              %APPDATA%\CM.EDITAR+\templates
            </span>
          </div>
          <div className="flex items-center gap-1.5">
            <button className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded border border-zinc-700 bg-zinc-800/60 hover:bg-zinc-700/60 text-[11px] text-zinc-200 font-semibold">
              <Download className="w-3 h-3" /> Import Pack
            </button>
            <button className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded border border-zinc-700 bg-zinc-800/60 hover:bg-zinc-700/60 text-[11px] text-zinc-200 font-semibold">
              <Upload className="w-3 h-3" /> Export Pack
            </button>
            <button onClick={onClose} className="ml-2 p-1.5 rounded hover:bg-zinc-800 text-zinc-400 hover:text-zinc-100">
              <X className="w-4 h-4" />
            </button>
          </div>
        </div>

        {/* Modal body */}
        <div className="flex-1 grid grid-cols-[240px_1fr_360px] divide-x divide-zinc-800 min-h-0">
          {/* Templates list */}
          <div className="flex flex-col min-h-0 bg-zinc-950/30">
            <div className="px-3 py-2 flex items-center justify-between border-b border-zinc-800">
              <span className="text-[10px] font-semibold uppercase tracking-widest text-zinc-500">Templates</span>
              <button className="p-1 rounded hover:bg-zinc-800 text-[#C87533]">
                <Plus className="w-3.5 h-3.5" />
              </button>
            </div>
            <div className="flex-1 overflow-y-auto py-1">
              {templates.map(t => (
                <button
                  key={t.id}
                  onClick={() => setActive(t.id)}
                  className={`w-full text-left px-3 py-2 flex items-start gap-2 border-l-2 ${
                    active === t.id
                      ? "border-[#C87533] bg-[#C87533]/[0.06]"
                      : "border-transparent hover:bg-zinc-800/40"
                  }`}
                >
                  <FileText className="w-3.5 h-3.5 mt-0.5 text-zinc-400" />
                  <div className="flex-1 min-w-0">
                    <div className="text-[12px] text-zinc-200 font-mono truncate">{t.name}</div>
                    <div className="text-[10px] text-zinc-500 mt-0.5">
                      maps to <span className="text-zinc-400 font-mono">{t.ext}</span> · {t.maps} ext
                    </div>
                  </div>
                </button>
              ))}
            </div>
          </div>

          {/* Editor */}
          <div className="flex flex-col min-h-0">
            <div className="px-4 py-2 flex items-center justify-between border-b border-zinc-800">
              <div className="flex items-center gap-2">
                <PencilLine className="w-3.5 h-3.5 text-zinc-400" />
                <span className="text-[12px] font-mono text-zinc-200">{active}</span>
                <span className="text-[10px] text-zinc-500">· UTF-8 · LF</span>
              </div>
              <div className="flex items-center gap-1">
                {["{{filename}}", "{{date}}", "{{user}}", "{{host}}", "{{guid}}"].map(p => (
                  <button
                    key={p}
                    onClick={() => setBody(b => b + p)}
                    className="px-2 py-0.5 rounded border border-zinc-700 bg-zinc-800/60 hover:bg-[#C87533]/20 hover:border-[#C87533]/50 text-[10px] font-mono text-zinc-300"
                  >
                    {p}
                  </button>
                ))}
              </div>
            </div>
            <textarea
              value={body}
              onChange={e => setBody(e.target.value)}
              spellCheck={false}
              className="flex-1 bg-[#0e1013] text-zinc-200 p-4 font-mono text-[12.5px] leading-relaxed resize-none focus:outline-none border-0"
            />
            <div className="px-4 py-1.5 border-t border-zinc-800 flex items-center justify-between text-[10px] text-zinc-500 font-mono bg-zinc-950/40">
              <span>Ln 7, Col 1 · 132 chars</span>
              <span className="text-emerald-400">● Saved · 3 placeholders</span>
            </div>
          </div>

          {/* Preview + mappings */}
          <div className="flex flex-col min-h-0 bg-zinc-950/30">
            <div className="px-3 py-2 border-b border-zinc-800 flex items-center gap-2">
              <Eye className="w-3.5 h-3.5 text-[#C87533]" />
              <span className="text-[10px] font-semibold uppercase tracking-widest text-zinc-300">Live Preview</span>
            </div>
            <div className="px-3 py-3 border-b border-zinc-800">
              <div className="rounded border border-zinc-800 bg-[#0e1013] p-3 text-[11px] font-mono text-zinc-300 leading-relaxed whitespace-pre-wrap">
                {body
                  .replace(/\{\{filename\}\}/g, "New Markdown Document")
                  .replace(/\{\{date\}\}/g, "2026-05-12")
                  .replace(/\{\{user\}\}/g, "carlos")
                  .replace(/\{\{host\}\}/g, "WIN-CMEDITAR")
                  .replace(/\{\{guid\}\}/g, "8A6F-4B22")}
              </div>
            </div>
            <div className="px-3 py-2 flex items-center justify-between">
              <span className="text-[10px] font-semibold uppercase tracking-widest text-zinc-300">Map to Extensions</span>
              <button className="inline-flex items-center gap-1 text-[10px] text-[#C87533] font-semibold hover:underline">
                <Link2 className="w-3 h-3" /> Add mapping
              </button>
            </div>
            <div className="flex-1 overflow-y-auto px-3 pb-3 space-y-1.5">
              {[
                { ext: ".md", label: "Markdown Document", on: true },
                { ext: ".markdown", label: "Markdown (alt)", on: false },
                { ext: ".mdx", label: "MDX Document", on: false },
              ].map(m => (
                <div key={m.ext} className="flex items-center justify-between rounded border border-zinc-800 bg-zinc-900/40 px-2.5 py-1.5">
                  <div className="flex items-center gap-2">
                    <span className={`w-1.5 h-1.5 rounded-full ${m.on ? "bg-emerald-400" : "bg-zinc-600"}`} />
                    <span className="font-mono text-[11px] text-zinc-200">{m.ext}</span>
                    <span className="text-[10px] text-zinc-500">{m.label}</span>
                  </div>
                  <button className={`text-[10px] font-semibold ${m.on ? "text-emerald-300" : "text-zinc-500"}`}>
                    {m.on ? "MAPPED" : "MAP"}
                  </button>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Modal footer */}
        <div className="px-4 py-2.5 border-t border-zinc-800 bg-zinc-900/60 flex items-center justify-between">
          <span className="text-[11px] text-zinc-400 font-mono inline-flex items-center gap-1.5">
            <Info className="w-3 h-3" />
            Templates are stored per-user in HKCU. Changes apply on next "Apply".
          </span>
          <div className="flex items-center gap-2">
            <button onClick={onClose} className="px-3 py-1.5 rounded border border-zinc-700 bg-zinc-800/60 hover:bg-zinc-700/60 text-xs font-semibold text-zinc-200">Cancel</button>
            <button onClick={onClose} className="px-3 py-1.5 rounded bg-[#C87533] hover:bg-[#d28344] text-xs font-semibold text-zinc-950 inline-flex items-center gap-1.5">
              <Check className="w-3.5 h-3.5" /> Save Template
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

function ApplyConfirmModal({
  pendingCount,
  onCancel,
  onConfirm,
}: {
  pendingCount: number;
  onCancel: () => void;
  onConfirm: (dontShowAgain: boolean) => void;
}) {
  const [backupChecked, setBackupChecked] = useState(false);
  const [dontShowAgain, setDontShowAgain] = useState(false);
  const [savedPath, setSavedPath] = useState<string | null>(null);

  const defaultBackup =
    "%LocalAppData%\\CM.EDITAR+\\snapshots\\auto-2026-05-12_17-42.reg";

  // Allow apply only when the user has acknowledged the default backup OR
  // explicitly saved a manual one.
  const canProceed = backupChecked || savedPath !== null;

  return (
    <div className="absolute inset-0 z-[60] flex items-center justify-center bg-black/75 backdrop-blur-sm p-6">
      <div className="w-full max-w-[560px] rounded-lg border border-amber-500/40 bg-[#15171a] shadow-2xl shadow-black/70 overflow-hidden">
        {/* Header */}
        <div className="flex items-center gap-2 px-4 py-3 border-b border-zinc-800 bg-amber-500/[0.07]">
          <ShieldAlert className="w-5 h-5 text-amber-300" />
          <h2 className="text-sm font-bold text-zinc-100 tracking-wide">
            Apply Changes — Confirm Backup
          </h2>
        </div>

        {/* Body */}
        <div className="px-5 py-4 space-y-4 text-[12.5px] text-zinc-300 leading-relaxed">
          <p>
            You're about to apply{" "}
            <span className="font-mono font-bold text-emerald-300">
              {pendingCount}
            </span>{" "}
            pending change{pendingCount === 1 ? "" : "s"} to the Windows{" "}
            <span className="font-mono text-zinc-100">HKCU</span> registry.
            Before continuing, please make sure you have a registry backup so
            you can roll back if anything goes wrong.
          </p>

          {/* Default backup option */}
          <label
            className={`flex items-start gap-2.5 rounded border px-3 py-2.5 cursor-pointer transition ${
              backupChecked
                ? "border-emerald-500/50 bg-emerald-500/[0.07]"
                : "border-zinc-700/70 bg-zinc-900/40 hover:border-zinc-600"
            }`}
          >
            <input
              type="checkbox"
              checked={backupChecked}
              onChange={(e) => setBackupChecked(e.target.checked)}
              className="mt-0.5 accent-emerald-500"
            />
            <div className="flex-1 min-w-0">
              <div className="text-[12px] font-semibold text-zinc-100">
                I've checked the default <span className="font-mono">.reg</span>{" "}
                backup at:
              </div>
              <div className="mt-1 text-[10.5px] font-mono text-zinc-400 truncate">
                {defaultBackup}
              </div>
            </div>
          </label>

          {/* OR divider */}
          <div className="flex items-center gap-2 text-[10px] font-semibold uppercase tracking-widest text-zinc-500">
            <span className="flex-1 h-px bg-zinc-800" />
            or
            <span className="flex-1 h-px bg-zinc-800" />
          </div>

          {/* Save now option */}
          <div className="rounded border border-zinc-700/70 bg-zinc-900/40 px-3 py-2.5">
            <div className="flex items-center justify-between gap-3">
              <div className="flex items-start gap-2 min-w-0">
                <Save className="w-4 h-4 text-[#C87533] mt-0.5" />
                <div className="min-w-0">
                  <div className="text-[12px] font-semibold text-zinc-100">
                    Save a backup now to a path I choose
                  </div>
                  <div className="mt-0.5 text-[10.5px] text-zinc-500">
                    Exports the current ShellNew keys to a{" "}
                    <span className="font-mono">.reg</span> file you can keep
                    anywhere.
                  </div>
                </div>
              </div>
              <button
                onClick={() =>
                  setSavedPath("D:\\Backups\\cm-editar_2026-05-12.reg")
                }
                className="shrink-0 inline-flex items-center gap-1.5 px-2.5 py-1.5 rounded border border-[#C87533]/60 bg-[#C87533]/15 hover:bg-[#C87533]/25 text-[11px] font-semibold text-[#d6a378]"
              >
                <FolderOpen className="w-3.5 h-3.5" />
                {savedPath ? "Change…" : "Choose location…"}
              </button>
            </div>
            {savedPath && (
              <div className="mt-2 inline-flex items-center gap-1.5 text-[10.5px] font-mono text-emerald-300">
                <Check className="w-3 h-3" />
                Saved to {savedPath}
              </div>
            )}
          </div>

          {/* Don't show again — power-user opt-out */}
          <label className="flex items-center gap-2 pt-1 text-[11.5px] text-zinc-400 cursor-pointer select-none">
            <input
              type="checkbox"
              checked={dontShowAgain}
              onChange={(e) => setDontShowAgain(e.target.checked)}
              className="accent-zinc-500"
            />
            Don't show this warning again{" "}
            <span className="text-zinc-600">(power-user mode)</span>
          </label>
        </div>

        {/* Footer */}
        <div className="px-4 py-3 border-t border-zinc-800 bg-zinc-900/60 flex items-center justify-between">
          <span className="text-[10.5px] text-zinc-500 font-mono inline-flex items-center gap-1.5">
            <Info className="w-3 h-3" />
            HKCU-only · No elevation required
          </span>
          <div className="flex items-center gap-2">
            <button
              onClick={onCancel}
              className="px-3 py-1.5 rounded border border-zinc-700 bg-zinc-800/60 hover:bg-zinc-700/60 text-xs font-semibold text-zinc-200"
            >
              Cancel
            </button>
            <button
              disabled={!canProceed}
              onClick={() => onConfirm(dontShowAgain)}
              className={`inline-flex items-center gap-1.5 px-4 py-1.5 rounded border text-xs font-bold transition ${
                canProceed
                  ? "bg-[#22c55e] hover:bg-[#16a34a] border-[#16a34a] text-zinc-950 shadow-[0_0_18px_-4px_rgba(34,197,94,0.75)]"
                  : "bg-zinc-800/40 border-zinc-700/40 text-zinc-600 cursor-not-allowed"
              }`}
            >
              <PlayCircle className="w-3.5 h-3.5" />
              Proceed with Apply
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

// ============================================================================
// Menubar primitives
// ============================================================================

interface MenuItemSpec {
  label?: string;
  icon?: typeof FileText;
  shortcut?: string;
  onClick?: () => void;
  separator?: boolean;
  toggle?: boolean;
  checked?: boolean;
  submenu?: MenuItemSpec[];
  danger?: boolean;
  disabled?: boolean;
}

interface MenuSpec {
  label: string;
  items: MenuItemSpec[];
}

function MenuRow({ item, onClose }: { item: MenuItemSpec; onClose: () => void }) {
  const [openSub, setOpenSub] = useState(false);

  if (item.separator) {
    return <div className="my-1 h-px bg-zinc-800" />;
  }

  const Icon = item.icon;
  const hasSub = item.submenu && item.submenu.length > 0;

  return (
    <div
      className="relative"
      onMouseEnter={() => hasSub && setOpenSub(true)}
      onMouseLeave={() => hasSub && setOpenSub(false)}
    >
      <button
        disabled={item.disabled}
        onClick={() => {
          if (hasSub) return;
          item.onClick?.();
          onClose();
        }}
        className={`w-full flex items-center gap-2 px-2.5 py-1.5 text-left text-[12px] rounded-sm ${
          item.disabled
            ? "text-zinc-600 cursor-not-allowed"
            : item.danger
            ? "text-rose-300 hover:bg-rose-500/10"
            : "text-zinc-200 hover:bg-[#C87533]/15 hover:text-zinc-50"
        }`}
      >
        {Icon ? (
          <Icon className="w-3.5 h-3.5 text-zinc-500 shrink-0" />
        ) : (
          <span className="w-3.5 h-3.5 inline-flex items-center justify-center">
            {item.toggle ? (
              <span
                className={`w-2.5 h-2.5 rounded-sm border ${
                  item.checked ? "bg-[#C87533] border-[#C87533]" : "border-zinc-600"
                }`}
              />
            ) : null}
          </span>
        )}
        <span className="flex-1 truncate">{item.label}</span>
        {item.shortcut && (
          <span className="text-[10px] font-mono text-zinc-500 ml-4">{item.shortcut}</span>
        )}
        {hasSub && <ChevronRight className="w-3 h-3 text-zinc-500" />}
      </button>
      {hasSub && openSub && (
        <div className="absolute top-0 left-full ml-1 min-w-[220px] rounded-md border border-zinc-800 bg-[#15171a] shadow-xl shadow-black/60 py-1 z-[80]">
          {item.submenu!.map((sub, i) => (
            <MenuRow key={i} item={sub} onClose={onClose} />
          ))}
        </div>
      )}
    </div>
  );
}

function MenuBar({ menus }: { menus: MenuSpec[] }) {
  const [openIdx, setOpenIdx] = useState<number | null>(null);
  const ref = useRef<HTMLDivElement | null>(null);

  // Click-outside closes any open menu so the bar feels native.
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (!ref.current) return;
      if (!ref.current.contains(e.target as Node)) setOpenIdx(null);
    };
    window.addEventListener("mousedown", handler);
    return () => window.removeEventListener("mousedown", handler);
  }, []);

  return (
    <div
      ref={ref}
      className="relative z-20 flex items-center gap-0.5 px-2 h-8 border-b border-zinc-800/80 bg-[#0b0d10]/95 backdrop-blur"
    >
      {menus.map((menu, idx) => {
        const open = openIdx === idx;
        return (
          <div key={menu.label} className="relative">
            <button
              onClick={() => setOpenIdx(open ? null : idx)}
              onMouseEnter={() => openIdx !== null && setOpenIdx(idx)}
              className={`px-2.5 h-7 rounded text-[12px] font-medium tracking-wide transition ${
                open
                  ? "bg-[#C87533]/15 text-zinc-50"
                  : "text-zinc-300 hover:bg-zinc-800/70 hover:text-zinc-100"
              }`}
            >
              {menu.label}
            </button>
            {open && (
              <div className="absolute top-full left-0 mt-0.5 min-w-[260px] rounded-md border border-zinc-800 bg-[#15171a] shadow-xl shadow-black/70 py-1 z-[80]">
                {menu.items.map((item, i) => (
                  <MenuRow key={i} item={item} onClose={() => setOpenIdx(null)} />
                ))}
              </div>
            )}
          </div>
        );
      })}
      <span className="ml-2 text-[10px] font-mono text-zinc-600 tracking-wide">
        File · Edit · Options · Help · Support
      </span>
    </div>
  );
}

// ============================================================================
// Help, Support & About modals
// ============================================================================

interface HelpSection {
  id: string;
  title: string;
  icon: typeof FileText;
  body: React.ReactNode;
}

const HELP_SECTIONS: HelpSection[] = [
  {
    id: "getting-started",
    title: "Getting Started",
    icon: Sparkles,
    body: (
      <>
        <p>
          CM.EDITAR+ is a safe, HKCU-only manager for the Windows Explorer
          <span className="font-mono"> "New" </span> submenu. Nothing here
          requires elevation — every change writes to your own user hive and
          is fully reversible from a snapshot.
        </p>
        <p className="mt-2">
          Pick an extension from the left to inspect it, toggle it on or off
          in the right panel, then hit the bright green <b>Apply Changes</b>{" "}
          button at the bottom. CM.EDITAR+ takes an automatic backup before
          every apply, and prompts you to confirm.
        </p>
      </>
    ),
  },
  {
    id: "submenu",
    title: "Submenu Concepts",
    icon: Layers,
    body: (
      <>
        <p>
          Windows reads the <span className="font-mono">"New"</span> submenu
          from <span className="font-mono">HKCU\Software\Classes\.ext\ShellNew</span>{" "}
          and its system-wide counterpart. CM.EDITAR+ <i>only</i> writes to
          the user hive, so a global reset never touches the system defaults.
        </p>
        <p className="mt-2">
          Each row tracks <b>state</b> (enabled/disabled/missing/pending) and{" "}
          <b>risk</b> (rec/warn/high). High-risk extensions like{" "}
          <span className="font-mono">.bat</span> and{" "}
          <span className="font-mono">.ps1</span> ship disabled by default.
        </p>
      </>
    ),
  },
  {
    id: "backup",
    title: "Backup & Rollback",
    icon: ShieldCheck,
    body: (
      <>
        <p>
          Every Apply auto-exports the current ShellNew keys to{" "}
          <span className="font-mono">%LocalAppData%\CM.EDITAR+\snapshots\auto-*.reg</span>.
          You can also save a manual <span className="font-mono">.reg</span>{" "}
          to any path you choose from the Apply confirmation pop-up.
        </p>
        <p className="mt-2">
          Use <b>Undo Last</b> to revert just the most recent apply, or{" "}
          <b>Undo All</b> to roll back to the snapshot you took at install
          time. Power users can dismiss the apply warning under Options →
          Reset Apply Warning whenever they want it back.
        </p>
      </>
    ),
  },
  {
    id: "templates",
    title: "Templates",
    icon: FileText,
    body: (
      <>
        <p>
          A template is the file body that gets dropped on disk when the user
          clicks <span className="font-mono">New ▸ Markdown Document</span>.
          Templates live under{" "}
          <span className="font-mono">%APPDATA%\CM.EDITAR+\templates</span>{" "}
          and support placeholders like{" "}
          <span className="font-mono">{"{{filename}}"}</span>,{" "}
          <span className="font-mono">{"{{date}}"}</span>,{" "}
          <span className="font-mono">{"{{user}}"}</span>,{" "}
          <span className="font-mono">{"{{host}}"}</span>, and{" "}
          <span className="font-mono">{"{{guid}}"}</span>.
        </p>
        <p className="mt-2">
          The Template Manager (Inspector → Edit Template) lets you author,
          preview, and re-map templates to extensions. Packs can be exported
          and shared.
        </p>
      </>
    ),
  },
  {
    id: "newplus",
    title: 'New+ Submenu',
    icon: Plus,
    body: (
      <>
        <p>
          The optional <b>New+</b> submenu is an extra cascading menu that
          shows under your existing New menu. Use it when you don't want to
          touch Microsoft's defaults but still want fast access to{" "}
          <span className="font-mono">.yaml</span>,{" "}
          <span className="font-mono">.toml</span>, etc.
        </p>
        <p className="mt-2">
          Toggle it from Options → Toggle "New+" Submenu. Items added under
          the Custom (New+) category land here, not in the main New list.
        </p>
      </>
    ),
  },
  {
    id: "filecreator",
    title: "FileCreator CLI",
    icon: Terminal,
    body: (
      <>
        <p>
          CM.EDITAR+ ships a tiny background service that exposes a named
          pipe (<span className="font-mono">\\.\pipe\cm-editar-filecreator</span>)
          authenticated with DPAPI. The <span className="font-mono">cme-new</span>{" "}
          CLI talks to it to create files using your templates from a script
          or scheduled task.
        </p>
        <p className="mt-2">
          Example:{" "}
          <span className="font-mono">cme-new --ext .md --in C:\Notes</span>
        </p>
      </>
    ),
  },
  {
    id: "shortcuts",
    title: "Keyboard Shortcuts",
    icon: Keyboard,
    body: (
      <table className="w-full text-[12px]">
        <tbody className="divide-y divide-zinc-800">
          {[
            ["Ctrl + K", "Find extension"],
            ["Ctrl + E", "Edit selected template"],
            ["Ctrl + Enter", "Apply Changes"],
            ["Ctrl + Z", "Undo last apply"],
            ["Ctrl + Shift + Z", "Undo all (revert to snapshot)"],
            ["Ctrl + S", "Save snapshot now"],
            ["Ctrl + N", "Add new entry to New+ submenu"],
            ["F1", "Open this help panel"],
          ].map(([k, d]) => (
            <tr key={k}>
              <td className="py-1.5 pr-3 font-mono text-zinc-300 whitespace-nowrap">
                {k}
              </td>
              <td className="py-1.5 text-zinc-400">{d}</td>
            </tr>
          ))}
        </tbody>
      </table>
    ),
  },
  {
    id: "faq",
    title: "FAQ",
    icon: HelpCircle,
    body: (
      <div className="space-y-3">
        <div>
          <div className="font-semibold text-zinc-100">
            Will this break Windows if I uninstall?
          </div>
          <p className="text-zinc-400">
            No. CM.EDITAR+ only writes to HKCU. Uninstalling restores your
            original snapshot automatically and removes our user-hive entries.
          </p>
        </div>
        <div>
          <div className="font-semibold text-zinc-100">
            Why are .bat / .ps1 disabled by default?
          </div>
          <p className="text-zinc-400">
            They execute on double-click and are common malware vectors in
            phishing scenarios. You can re-enable them any time.
          </p>
        </div>
        <div>
          <div className="font-semibold text-zinc-100">
            Does this need admin rights?
          </div>
          <p className="text-zinc-400">
            Never. The header badge always reads "Elevation: Not required".
          </p>
        </div>
      </div>
    ),
  },
  {
    id: "about",
    title: "About",
    icon: Info,
    body: (
      <div className="space-y-2">
        <div className="text-[14px] font-bold text-zinc-100">
          CM<span className="text-[#C87533]">.EDITAR</span>
          <span className="text-[#d6a378]">+</span>{" "}
          <span className="text-zinc-500 font-normal">v1.3.0</span>
        </div>
        <div className="text-[11.5px] font-mono text-zinc-500">
          build 1.3.0+f4a2e · .NET 8 · Avalonia 11.2.1
        </div>
        <p className="text-zinc-400">
          A safer, HKCU-only manager for the Windows Explorer "New" submenu.
          Built by{" "}
          <a
            href="https://SHAmun.fyi"
            target="_blank"
            rel="noreferrer"
            className="text-[#C87533] hover:underline"
          >
            SHAmun.fyi
          </a>
          .
        </p>
        <p className="text-[10.5px] text-zinc-600">
          © 2026 Ethan Munson Sr. Released under the MIT License.
        </p>
      </div>
    ),
  },
];

function HelpModal({
  initialSection,
  onClose,
}: {
  initialSection?: string;
  onClose: () => void;
}) {
  const [active, setActive] = useState(initialSection ?? "getting-started");
  const section = HELP_SECTIONS.find((s) => s.id === active) ?? HELP_SECTIONS[0];

  return (
    <div className="absolute inset-0 z-[70] flex items-center justify-center bg-black/75 backdrop-blur-sm p-6">
      <div className="w-full max-w-[920px] h-[600px] rounded-lg border border-zinc-700/80 bg-[#15171a] shadow-2xl shadow-black/70 flex flex-col overflow-hidden">
        <div className="flex items-center justify-between px-4 py-2.5 border-b border-zinc-800 bg-zinc-900/60">
          <div className="flex items-center gap-2">
            <BookOpen className="w-4 h-4 text-[#C87533]" />
            <h2 className="text-sm font-semibold text-zinc-100 tracking-wide">
              CM.EDITAR+ Help
            </h2>
            <span className="text-[10px] font-mono text-zinc-500 px-2 py-0.5 rounded border border-zinc-800">
              v1.3.0
            </span>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 rounded hover:bg-zinc-800 text-zinc-400 hover:text-zinc-100"
          >
            <X className="w-4 h-4" />
          </button>
        </div>
        <div className="flex-1 grid grid-cols-[220px_1fr] divide-x divide-zinc-800 min-h-0">
          <nav className="overflow-y-auto py-2 bg-zinc-950/30">
            {HELP_SECTIONS.map((s) => {
              const Icon = s.icon;
              const sel = s.id === active;
              return (
                <button
                  key={s.id}
                  onClick={() => setActive(s.id)}
                  className={`w-full flex items-center gap-2 text-left px-3 py-1.5 text-[12px] border-l-2 ${
                    sel
                      ? "border-[#C87533] bg-[#C87533]/[0.07] text-zinc-100"
                      : "border-transparent text-zinc-400 hover:bg-zinc-800/40 hover:text-zinc-200"
                  }`}
                >
                  <Icon
                    className={`w-3.5 h-3.5 ${
                      sel ? "text-[#C87533]" : "text-zinc-500"
                    }`}
                  />
                  {s.title}
                </button>
              );
            })}
          </nav>
          <div className="overflow-y-auto px-6 py-5">
            <div className="flex items-center gap-2 mb-3">
              <section.icon className="w-4 h-4 text-[#C87533]" />
              <h3 className="text-[14px] font-semibold text-zinc-100">
                {section.title}
              </h3>
            </div>
            <div className="text-[12.5px] text-zinc-300 leading-relaxed space-y-2 max-w-[640px]">
              {section.body}
            </div>
          </div>
        </div>
        <div className="px-4 py-2 border-t border-zinc-800 bg-zinc-900/60 flex items-center justify-between text-[10.5px] text-zinc-500 font-mono">
          <span>Need more? Visit SHAmun.fyi or open an issue on GitHub.</span>
          <span>Press F1 anywhere to reopen this panel.</span>
        </div>
      </div>
    </div>
  );
}

function SupportModal({
  onClose,
  onAbout,
}: {
  onClose: () => void;
  onAbout: () => void;
}) {
  const links = [
    {
      title: "SHAmun.fyi",
      desc: "My developer site & portfolio",
      href: "https://SHAmun.fyi",
      icon: Globe2,
      cls: "border-[#C87533]/40 bg-[#C87533]/[0.06] hover:bg-[#C87533]/15 text-[#d6a378]",
    },
    {
      title: "@RealHaywoodJ",
      desc: "GitHub — source, issues, PRs",
      href: "https://github.com/RealHaywoodJ",
      icon: Github,
      cls: "border-zinc-700 bg-zinc-800/60 hover:bg-zinc-700/60 text-zinc-100",
    },
    {
      title: "Buy Me a Coffee",
      desc: "buymeacoffee.com/SirSHAmun5on12",
      href: "https://buymeacoffee.com/SirSHAmun5on12",
      icon: Coffee,
      cls: "border-yellow-500/40 bg-yellow-500/[0.07] hover:bg-yellow-500/15 text-yellow-200",
    },
  ];

  return (
    <div className="absolute inset-0 z-[70] flex items-center justify-center bg-black/75 backdrop-blur-sm p-6">
      <div className="w-full max-w-[760px] rounded-lg border border-zinc-700/80 bg-[#15171a] shadow-2xl shadow-black/70 overflow-hidden">
        <div className="flex items-center justify-between px-4 py-2.5 border-b border-zinc-800 bg-zinc-900/60">
          <div className="flex items-center gap-2">
            <Heart className="w-4 h-4 text-rose-400" />
            <h2 className="text-sm font-semibold text-zinc-100 tracking-wide">
              Support CM.EDITAR+
            </h2>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 rounded hover:bg-zinc-800 text-zinc-400 hover:text-zinc-100"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        <div className="px-5 py-4 space-y-4">
          {/* Dev bio strip */}
          <div className="rounded border border-zinc-800 bg-zinc-900/40 px-4 py-3 flex items-center gap-3">
            <div className="w-10 h-10 rounded-full bg-gradient-to-br from-[#C87533] to-[#d6a378] flex items-center justify-center text-[14px] font-bold text-zinc-950">
              EM
            </div>
            <div className="flex-1 min-w-0">
              <div className="text-[13px] font-semibold text-zinc-100">
                Built by Ethan Munson Sr.
              </div>
              <div className="text-[11px] text-zinc-500">
                Independent developer · CM.EDITAR+ is free and open-source.
              </div>
            </div>
          </div>

          {/* Link cards */}
          <div className="grid grid-cols-3 gap-2">
            {links.map((l) => {
              const Icon = l.icon;
              return (
                <a
                  key={l.title}
                  href={l.href}
                  target="_blank"
                  rel="noreferrer"
                  className={`rounded border px-3 py-2.5 transition flex flex-col gap-1.5 ${l.cls}`}
                >
                  <div className="flex items-center justify-between">
                    <Icon className="w-4 h-4" />
                    <ExternalLink className="w-3 h-3 opacity-60" />
                  </div>
                  <div className="text-[12px] font-bold leading-tight">
                    {l.title}
                  </div>
                  <div className="text-[10px] opacity-80 leading-snug truncate">
                    {l.desc}
                  </div>
                </a>
              );
            })}
          </div>

          {/* QR codes */}
          <div className="grid grid-cols-2 gap-3">
            <div className="rounded border border-zinc-800 bg-zinc-950/40 p-3 flex flex-col items-center">
              <img
                src="/__mockup/images/dev-qr.png"
                alt="Dev site QR — SHAmun.fyi"
                className="w-32 h-32 rounded bg-white p-1"
              />
              <div className="mt-2 text-[11px] font-semibold text-zinc-200">
                SHAmun.fyi
              </div>
              <div className="text-[10px] text-zinc-500">Scan for dev site</div>
            </div>
            <div className="rounded border border-zinc-800 bg-zinc-950/40 p-3 flex flex-col items-center">
              <img
                src="/__mockup/images/bmc-qr.png"
                alt="Buy Me a Coffee QR"
                className="w-32 h-32 rounded bg-white p-1"
              />
              <div className="mt-2 text-[11px] font-semibold text-yellow-200">
                Buy Me a Coffee
              </div>
              <div className="text-[10px] text-zinc-500">
                Scan to support development
              </div>
            </div>
          </div>

          <div className="rounded border border-rose-500/30 bg-rose-500/[0.05] px-3 py-2 text-[11.5px] text-rose-200 inline-flex items-center gap-2 w-full">
            <Heart className="w-3.5 h-3.5" />
            Thanks for supporting CM.EDITAR+ — every coffee fuels the next
            release.
          </div>
        </div>

        <div className="px-4 py-2.5 border-t border-zinc-800 bg-zinc-900/60 flex items-center justify-between">
          <button
            onClick={onAbout}
            className="text-[11px] text-zinc-400 hover:text-zinc-100 inline-flex items-center gap-1.5"
          >
            <Info className="w-3 h-3" /> About CM.EDITAR+
          </button>
          <button
            onClick={onClose}
            className="px-3 py-1.5 rounded border border-zinc-700 bg-zinc-800/60 hover:bg-zinc-700/60 text-xs font-semibold text-zinc-200"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
}

function AboutDialog({ onClose }: { onClose: () => void }) {
  return (
    <div className="absolute inset-0 z-[80] flex items-center justify-center bg-black/75 backdrop-blur-sm p-6">
      <div className="w-full max-w-[420px] rounded-lg border border-zinc-700/80 bg-[#15171a] shadow-2xl shadow-black/70 overflow-hidden">
        <div className="px-5 py-4 flex flex-col items-center text-center">
          <div className="w-14 h-14 rounded-md border border-zinc-700 bg-gradient-to-br from-zinc-800 to-zinc-950 overflow-hidden mb-3">
            <img
              src="/__mockup/images/cm-editar-logo.png"
              alt="CM.EDITAR+"
              className="w-full h-full object-cover"
            />
          </div>
          <div className="text-[18px] font-bold text-zinc-100">
            CM<span className="text-[#C87533]">.EDITAR</span>
            <span className="text-[#d6a378]">+</span>
          </div>
          <div className="text-[11px] font-mono text-zinc-500 mt-0.5">
            v1.3.0 · build 1.3.0+f4a2e
          </div>
          <p className="text-[12px] text-zinc-400 mt-3 leading-relaxed">
            A safer, HKCU-only manager for the Windows Explorer "New" submenu.
            Snapshots, rollback, templates, FileCreator CLI, optional New+
            submenu.
          </p>
          <div className="text-[11px] text-zinc-500 mt-3 font-mono">
            .NET 8 · Avalonia 11.2.1
          </div>
          <a
            href="https://SHAmun.fyi"
            target="_blank"
            rel="noreferrer"
            className="text-[11.5px] text-[#C87533] hover:underline mt-2 inline-flex items-center gap-1"
          >
            SHAmun.fyi <ExternalLink className="w-3 h-3" />
          </a>
          <div className="text-[10.5px] text-zinc-600 mt-3">
            © 2026 Ethan Munson Sr. · MIT License
          </div>
        </div>
        <div className="px-4 py-2.5 border-t border-zinc-800 bg-zinc-900/60 flex items-center justify-end">
          <button
            onClick={onClose}
            className="px-3 py-1.5 rounded bg-[#C87533] hover:bg-[#d28344] text-xs font-semibold text-zinc-950"
          >
            OK
          </button>
        </div>
      </div>
    </div>
  );
}

export function Manager() {
  const [activeCategory, setActiveCategory] = useState("all");
  const [selectedId, setSelectedId] = useState<string>(
    ENTRIES.find((e) => e.ext === ".md")?.id ?? ENTRIES[0]?.id ?? "",
  );
  const [viewMode, setViewMode] = useState<"category" | "az">("category");
  const [showTemplateManager, setShowTemplateManager] = useState(false);
  const [search, setSearch] = useState("");
  const [showApplyConfirm, setShowApplyConfirm] = useState(false);
  const [applyWarningDismissed, setApplyWarningDismissed] = useState(false);
  const [applyToast, setApplyToast] = useState<string | null>(null);
  // Track the active toast timer so rapid Apply clicks don't stack timeouts
  // that would prematurely clear a fresh toast.
  const [toastTimer, setToastTimer] = useState<number | null>(null);
  const [showHelp, setShowHelp] = useState(false);
  const [helpSection, setHelpSection] = useState<string | undefined>(undefined);
  const [showSupport, setShowSupport] = useState(false);
  const [showAbout, setShowAbout] = useState(false);
  // Options-menu toggles. Persisted only in component state for the mockup.
  const [newPlusEnabled, setNewPlusEnabled] = useState(true);
  const [showRiskColumn, setShowRiskColumn] = useState(true);
  const [autoBackup, setAutoBackup] = useState(true);
  const [theme, setTheme] = useState<"dark" | "light" | "system">("dark");

  const showToast = (msg: string) => {
    if (toastTimer !== null) window.clearTimeout(toastTimer);
    setApplyToast(msg);
    const id = window.setTimeout(() => {
      setApplyToast(null);
      setToastTimer(null);
    }, 3500);
    setToastTimer(id);
  };

  // Load the power-user "don't show again" preference once on mount so the
  // mockup behaves realistically across reloads.
  useEffect(() => {
    try {
      if (localStorage.getItem(APPLY_WARNING_DISMISSED_KEY) === "1") {
        setApplyWarningDismissed(true);
      }
    } catch {
      /* localStorage unavailable — keep default */
    }
  }, []);

  const handleApplyClick = () => {
    if (applyWarningDismissed) {
      showToast("Changes applied successfully (power-user mode).");
      return;
    }
    setShowApplyConfirm(true);
  };

  const handleApplyConfirm = (dontShowAgain: boolean) => {
    if (dontShowAgain) {
      setApplyWarningDismissed(true);
      try {
        localStorage.setItem(APPLY_WARNING_DISMISSED_KEY, "1");
      } catch {
        /* ignore */
      }
    }
    setShowApplyConfirm(false);
    showToast("Changes applied successfully.");
  };

  // Clears the localStorage opt-out so the green Apply confirm modal
  // pops up again — matches the "Reset Apply Warning" Options-menu item.
  const handleResetApplyWarning = () => {
    try {
      localStorage.removeItem(APPLY_WARNING_DISMISSED_KEY);
    } catch {
      /* ignore */
    }
    setApplyWarningDismissed(false);
    showToast("Apply confirmation re-enabled.");
  };

  const openHelp = (sectionId?: string) => {
    setHelpSection(sectionId);
    setShowHelp(true);
  };

  // ---- Shared action handlers --------------------------------------------
  // One source of truth for actions that exist in both the footer toolbar
  // and the menubar dropdowns. Both call sites pass these references so
  // there is no behaviour drift.
  const handleUndoLast = () => showToast("Undid last apply.");
  const handleUndoAll = () =>
    showToast("Reverted all changes to last snapshot.");
  const handleFlushShellCache = () =>
    showToast("Shell icon cache flushed.");
  const handleExportManifest = () => showToast("Manifest exported.");
  const handleImportManifest = () => showToast("Manifest imported.");
  const handlePreflight = () =>
    showToast("Preflight checks passed.");

  // ---- Menubar specification ----------------------------------------------
  const menus: MenuSpec[] = [
    {
      label: "File",
      items: [
        {
          label: "New Snapshot",
          icon: Camera,
          shortcut: "Ctrl+S",
          onClick: () => showToast("New snapshot saved."),
        },
        {
          label: "Open Snapshot…",
          icon: FolderOpen,
          onClick: () => showToast("Open snapshot — picker would appear here."),
        },
        {
          label: "Save Snapshot As…",
          icon: Save,
          shortcut: "Ctrl+Shift+S",
          onClick: () => showToast("Save snapshot — picker would appear here."),
        },
        { separator: true },
        {
          label: "Export Manifest…",
          icon: Upload,
          onClick: handleExportManifest,
        },
        {
          label: "Import Manifest…",
          icon: Download,
          onClick: handleImportManifest,
        },
        { separator: true },
        {
          label: "Recent Snapshots",
          icon: History,
          submenu: [
            {
              label: "auto-2026-05-12_17-42.reg",
              icon: FileText,
              onClick: () => showToast("Loaded snapshot 17:42."),
            },
            {
              label: "auto-2026-05-12_15-08.reg",
              icon: FileText,
              onClick: () => showToast("Loaded snapshot 15:08."),
            },
            {
              label: "manual-pre-install.reg",
              icon: FileText,
              onClick: () => showToast("Loaded pre-install snapshot."),
            },
            { separator: true },
            { label: "Clear list", icon: Eraser, danger: true },
          ],
        },
        { separator: true },
        {
          label: "Exit",
          icon: Power,
          shortcut: "Alt+F4",
          danger: true,
          onClick: () => showToast("Exit requested (mockup)."),
        },
      ],
    },
    {
      label: "Edit",
      items: [
        {
          label: "Undo Last Apply",
          icon: Undo2,
          shortcut: "Ctrl+Z",
          onClick: handleUndoLast,
        },
        {
          label: "Redo",
          icon: RotateCcw,
          shortcut: "Ctrl+Y",
          onClick: () => showToast("Redid last reverted change."),
        },
        {
          label: "Undo All (Revert to Snapshot)",
          icon: History,
          shortcut: "Ctrl+Shift+Z",
          onClick: handleUndoAll,
        },
        {
          label: "Flush Shell Icon Cache",
          icon: Trash2,
          onClick: handleFlushShellCache,
        },
        { separator: true },
        {
          label: "Find Extension…",
          icon: Search,
          shortcut: "Ctrl+K",
          onClick: () => {
            const el = document.querySelector<HTMLInputElement>(
              'input[placeholder^="Search extensions"]',
            );
            el?.focus();
          },
        },
        { separator: true },
        {
          label: "Bulk Enable…",
          icon: ListChecks,
          onClick: () => showToast("Bulk enable picker (mockup)."),
        },
        {
          label: "Bulk Disable…",
          icon: X,
          onClick: () => showToast("Bulk disable picker (mockup)."),
        },
        { separator: true },
        {
          label: "Clear Pending Queue",
          icon: Eraser,
          danger: true,
          onClick: () => showToast("Pending queue cleared."),
        },
      ],
    },
    {
      label: "Options",
      items: [
        {
          label: "Preferences…",
          icon: Settings,
          onClick: () => showToast("Preferences (mockup)."),
        },
        { separator: true },
        {
          label: 'Toggle "New+" Submenu',
          toggle: true,
          checked: newPlusEnabled,
          onClick: () => {
            setNewPlusEnabled((v) => !v);
            showToast(
              newPlusEnabled ? '"New+" submenu disabled.' : '"New+" submenu enabled.',
            );
          },
        },
        {
          label: "Show Power-User Risk",
          toggle: true,
          checked: showRiskColumn,
          onClick: () => setShowRiskColumn((v) => !v),
        },
        {
          label: "Auto-Backup on Apply",
          toggle: true,
          checked: autoBackup,
          onClick: () => setAutoBackup((v) => !v),
        },
        { separator: true },
        {
          label: "Reset Apply Warning",
          icon: RefreshCw,
          onClick: handleResetApplyWarning,
        },
        { separator: true },
        {
          label: "Theme",
          icon: Palette,
          submenu: [
            {
              label: "Dark",
              icon: Moon,
              toggle: true,
              checked: theme === "dark",
              onClick: () => setTheme("dark"),
            },
            {
              label: "Light",
              icon: Sun,
              toggle: true,
              checked: theme === "light",
              onClick: () => setTheme("light"),
            },
            {
              label: "System",
              icon: Monitor,
              toggle: true,
              checked: theme === "system",
              onClick: () => setTheme("system"),
            },
          ],
        },
      ],
    },
    {
      label: "Help",
      items: [
        {
          label: "Getting Started",
          icon: Sparkles,
          shortcut: "F1",
          onClick: () => openHelp("getting-started"),
        },
        {
          label: "Submenu Concepts",
          icon: Layers,
          onClick: () => openHelp("submenu"),
        },
        {
          label: "Backup & Rollback",
          icon: ShieldCheck,
          onClick: () => openHelp("backup"),
        },
        {
          label: "Templates",
          icon: FileText,
          onClick: () => openHelp("templates"),
        },
        {
          label: 'New+ Submenu',
          icon: Plus,
          onClick: () => openHelp("newplus"),
        },
        {
          label: "FileCreator CLI",
          icon: Terminal,
          onClick: () => openHelp("filecreator"),
        },
        { separator: true },
        {
          label: "Keyboard Shortcuts",
          icon: Keyboard,
          onClick: () => openHelp("shortcuts"),
        },
        {
          label: "FAQ",
          icon: HelpCircle,
          onClick: () => openHelp("faq"),
        },
        { separator: true },
        {
          label: "About CM.EDITAR+",
          icon: Info,
          onClick: () => setShowAbout(true),
        },
      ],
    },
    {
      label: "Support",
      items: [
        {
          label: "Open Support Panel…",
          icon: LifeBuoy,
          onClick: () => setShowSupport(true),
        },
        { separator: true },
        {
          label: "SHAmun.fyi (Dev Site)",
          icon: Globe2,
          onClick: () => window.open("https://SHAmun.fyi", "_blank"),
        },
        {
          label: "GitHub @RealHaywoodJ",
          icon: Github,
          onClick: () =>
            window.open("https://github.com/RealHaywoodJ", "_blank"),
        },
        {
          label: "Buy Me a Coffee",
          icon: Coffee,
          onClick: () =>
            window.open(
              "https://buymeacoffee.com/SirSHAmun5on12",
              "_blank",
            ),
        },
        { separator: true },
        {
          label: "About CM.EDITAR+",
          icon: Info,
          onClick: () => setShowAbout(true),
        },
      ],
    },
  ];

  // Category/state filtering — A–Z mode forces the "all" bucket so the
  // user is looking at the full catalog when they switch to alphabetical view.
  const effectiveCategory = viewMode === "az" ? "all" : activeCategory;
  const filteredBase = ENTRIES.filter(e => {
    if (effectiveCategory === "all") return true;
    if (effectiveCategory === "missing") return e.state === "missing";
    if (effectiveCategory === "disabled") return e.state === "disabled";
    if (effectiveCategory.startsWith("cat:")) return e.group === effectiveCategory.slice(4);
    return true;
  }).filter(e =>
    !search ||
    e.ext.toLowerCase().includes(search.toLowerCase()) ||
    e.label.toLowerCase().includes(search.toLowerCase()),
  );

  // In A–Z mode we sort the flat list and inject sticky section dividers; in
  // Category mode the natural catalog order (already grouped by category) is
  // preserved so users can scan one category at a time.
  const filtered =
    viewMode === "az"
      ? [...filteredBase].sort((a, b) => a.ext.localeCompare(b.ext))
      : filteredBase;

  const selected = ENTRIES.find(e => e.id === selectedId) ?? ENTRIES[0];
  const queuedCount = ENTRIES.filter(e => e.queued).length;
  const visibleCount = ENTRIES.filter(e => e.state === "enabled").length;

  return (
    <div className="relative h-screen w-full bg-[#0c0e10] text-zinc-200 font-['Inter'] overflow-hidden flex flex-col select-none">
      {/* Subtle metallic backdrop */}
      <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_at_top,_rgba(200,117,51,0.08),_transparent_55%)]" />
      <div className="pointer-events-none absolute inset-0 bg-[linear-gradient(180deg,rgba(255,255,255,0.02)_0%,transparent_40%)]" />

      {/* ====== HEADER ====== */}
      <header className="relative z-10 flex items-center justify-between px-4 h-14 border-b border-zinc-800/80 bg-[#0e1013]/90 backdrop-blur">
        <div className="flex items-center gap-3">
          <div className="relative w-9 h-9 rounded-md border border-zinc-700/80 bg-gradient-to-br from-zinc-800 to-zinc-950 overflow-hidden shadow-inner">
            <img src="/__mockup/images/cm-editar-logo.png" alt="CM.EDITAR+" className="absolute inset-0 w-full h-full object-cover" />
          </div>
          <div className="flex flex-col leading-tight">
            <div className="flex items-center gap-2">
              <span className="text-[15px] font-bold tracking-wide text-zinc-100">
                CM<span className="text-[#C87533]">.EDITAR</span>
                <span className="text-[#d6a378]">+</span>
              </span>
              <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-zinc-800/80 text-zinc-400 border border-zinc-700/60">
                v1.3.0
              </span>
            </div>
            <span className="text-[10.5px] text-zinc-500 tracking-wide">
              Windows "New" submenu manager · HKCU-only
            </span>
          </div>
        </div>

        <div className="flex items-center gap-2 flex-1 max-w-xl mx-6">
          <div className="relative w-full">
            <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-zinc-500" />
            <input
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder="Search extensions, labels, packs (e.g. .md, json, office)…"
              className="w-full h-8 pl-8 pr-3 rounded border border-zinc-800 bg-zinc-900/70 focus:bg-zinc-900 focus:border-[#C87533]/60 text-[12px] placeholder:text-zinc-600 focus:outline-none"
            />
            <kbd className="absolute right-2 top-1/2 -translate-y-1/2 text-[9px] font-mono text-zinc-500 border border-zinc-700 rounded px-1 py-0.5 bg-zinc-900">Ctrl+K</kbd>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <span className="inline-flex items-center gap-1.5 px-2 py-1 rounded border border-emerald-600/40 bg-emerald-600/10 text-emerald-300 text-[11px] font-semibold">
            <ShieldCheck className="w-3.5 h-3.5" />
            Elevation: Not required
          </span>
          <span className="inline-flex items-center gap-1.5 px-2 py-1 rounded border border-zinc-700 bg-zinc-800/60 text-zinc-300 text-[11px] font-semibold font-mono">
            HKCU · Per-user
          </span>
          <span className="inline-flex items-center gap-1.5 px-2 py-1 rounded border border-zinc-700 bg-zinc-800/60 text-zinc-300 text-[11px] font-mono">
            <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 shadow-[0_0_6px_rgba(52,211,153,0.8)]" />
            Snapshot · 17:42
          </span>
          <button
            onClick={() => openHelp()}
            className="inline-flex items-center gap-1 px-2 py-1 rounded border border-zinc-700 bg-zinc-800/60 hover:bg-zinc-700/60 text-zinc-200 text-[11px] font-semibold"
          >
            <HelpCircle className="w-3.5 h-3.5" /> Help
          </button>
          <button
            onClick={() => setShowSupport(true)}
            className="inline-flex items-center gap-1 px-2 py-1 rounded border border-rose-500/40 bg-rose-500/10 hover:bg-rose-500/20 text-rose-200 text-[11px] font-semibold"
          >
            <Heart className="w-3.5 h-3.5" /> Support
          </button>
        </div>
      </header>

      {/* ====== MENUBAR ====== */}
      <MenuBar menus={menus} />

      {/* ====== BODY ====== */}
      <div className="relative z-10 flex-1 grid grid-cols-[228px_minmax(0,1fr)_340px] divide-x divide-zinc-800/80 min-h-0">
        {/* LEFT — Categories */}
        <aside className="flex flex-col min-h-0 bg-[#0d0f12]/60">
          <div className="px-3 pt-3 pb-2 flex items-center justify-between">
            <span className="text-[10px] font-semibold uppercase tracking-widest text-zinc-500">Categories</span>
            <span className="text-[10px] font-mono text-zinc-600">{ENTRIES.length} total</span>
          </div>
          <nav className="flex-1 overflow-y-auto px-2 pb-3 space-y-0.5">
            {CATEGORIES.map(c => {
              const active = activeCategory === c.id;
              const Icon = c.icon;
              return (
                <button
                  key={c.id}
                  onClick={() => setActiveCategory(c.id)}
                  className={`w-full flex items-center gap-2 px-2.5 py-1.5 rounded text-[12px] transition border ${
                    active
                      ? "bg-[#C87533]/10 border-[#C87533]/40 text-zinc-100"
                      : "border-transparent text-zinc-400 hover:bg-zinc-800/50 hover:text-zinc-200"
                  }`}
                >
                  <Icon className={`w-3.5 h-3.5 ${active ? "text-[#C87533]" : "text-zinc-500"}`} />
                  <span className="flex-1 text-left">{c.label}</span>
                  <span className={`text-[10px] font-mono ${active ? "text-[#C87533]" : "text-zinc-600"}`}>
                    {c.count}
                  </span>
                </button>
              );
            })}
          </nav>

          <div className="border-t border-zinc-800/80 p-3 space-y-2">
            <div className="text-[10px] font-semibold uppercase tracking-widest text-zinc-500">Packs</div>
            {[
              { name: "cm-editar.core", count: 4, on: true },
              { name: "cm-editar.web", count: 2, on: true },
              { name: "cm-editar.dev", count: 2, on: true },
              { name: "cm-editar.user", count: 1, on: true },
            ].map(p => (
              <div key={p.name} className="flex items-center justify-between text-[11px]">
                <div className="flex items-center gap-1.5">
                  <span className={`w-1.5 h-1.5 rounded-full ${p.on ? "bg-[#C87533]" : "bg-zinc-600"}`} />
                  <span className="font-mono text-zinc-300">{p.name}</span>
                </div>
                <span className="text-zinc-600 font-mono">{p.count}</span>
              </div>
            ))}
          </div>
        </aside>

        {/* CENTER — Table */}
        <main className="flex flex-col min-h-0">
          <div className="flex items-center justify-between px-4 py-2 border-b border-zinc-800/80 bg-[#0e1013]/60">
            <div className="flex items-center gap-2">
              <h2 className="text-[12px] font-semibold uppercase tracking-widest text-zinc-300">
                ShellNew Extensions
              </h2>
              <span className="text-[10px] font-mono text-zinc-500">
                {filtered.length} shown · {visibleCount} active in submenu
              </span>
            </div>
            <div className="flex items-center gap-1.5">
              <button className="inline-flex items-center gap-1 px-2 py-1 rounded border border-zinc-700 bg-zinc-800/60 hover:bg-zinc-700/60 text-[11px] text-zinc-300">
                <Sparkles className="w-3 h-3 text-[#C87533]" /> New+ submenu
              </button>
              {/* View toggle: catalog-order vs flat A–Z with section headers */}
              <div className="inline-flex items-center rounded border border-zinc-700 bg-zinc-800/60 overflow-hidden text-[11px]">
                <button
                  onClick={() => setViewMode("category")}
                  className={`px-2 py-1 transition ${
                    viewMode === "category"
                      ? "bg-[#C87533]/20 text-[#d6a378]"
                      : "text-zinc-300 hover:bg-zinc-700/60"
                  }`}
                >
                  Category
                </button>
                <span className="w-px h-4 bg-zinc-700" />
                <button
                  onClick={() => setViewMode("az")}
                  className={`px-2 py-1 transition ${
                    viewMode === "az"
                      ? "bg-[#C87533]/20 text-[#d6a378]"
                      : "text-zinc-300 hover:bg-zinc-700/60"
                  }`}
                >
                  A–Z
                </button>
              </div>
            </div>
          </div>

          <div className="flex-1 overflow-auto">
            <table className="w-full text-[12px] border-separate border-spacing-0">
              <thead className="sticky top-0 z-10 bg-[#0e1013] text-[10px] font-semibold uppercase tracking-widest text-zinc-500">
                <tr>
                  <th className="text-left font-semibold px-3 py-2 border-b border-zinc-800 w-16">Queue</th>
                  <th className="text-left font-semibold px-3 py-2 border-b border-zinc-800 w-24">Ext</th>
                  <th className="text-left font-semibold px-3 py-2 border-b border-zinc-800">"New" Menu Label</th>
                  <th className="text-left font-semibold px-3 py-2 border-b border-zinc-800 w-32">Group</th>
                  <th className="text-left font-semibold px-3 py-2 border-b border-zinc-800 w-24">State</th>
                  <th className="text-left font-semibold px-3 py-2 border-b border-zinc-800 w-20">Risk</th>
                  <th className="text-left font-semibold px-3 py-2 border-b border-zinc-800">Description</th>
                </tr>
              </thead>
              <tbody>
                {(() => {
                  // In A–Z mode the rows are flat-sorted; emit a sticky header
                  // whenever the per-letter section changes (#, 0, 1, … A, B, …).
                  // The bucket label (Symbols / Numerical / Alphabetical) is
                  // shown alongside so the user keeps high-level orientation.
                  const rows: React.ReactNode[] = [];
                  let lastHeader: string | null = null;
                  const azCounts = viewMode === "az"
                    ? filtered.reduce<Record<string, number>>((acc, x) => {
                        const k = azKey(x.ext).header;
                        acc[k] = (acc[k] ?? 0) + 1;
                        return acc;
                      }, {})
                    : {};
                  filtered.forEach((e) => {
                    if (viewMode === "az") {
                      const { bucket, header } = azKey(e.ext);
                      if (header !== lastHeader) {
                        lastHeader = header;
                        rows.push(
                          <tr key={`hdr:${header}`} className="sticky top-[28px] z-[5] bg-[#0e1013]">
                            <td
                              colSpan={7}
                              className="px-3 py-1.5 border-b border-zinc-800 text-[10px] font-semibold uppercase tracking-[0.16em] text-[#C87533]"
                            >
                              <span className="text-[12px] tracking-[0.2em]">{header}</span>
                              <span className="ml-2 text-zinc-500 normal-case tracking-normal">{bucket}</span>
                              <span className="ml-2 font-mono text-zinc-500 normal-case tracking-normal">
                                {azCounts[header]}
                              </span>
                            </td>
                          </tr>,
                        );
                      }
                    }
                    const isSel = e.id === selectedId;
                    const Icon = e.icon;
                    rows.push(
                      <tr
                        key={e.id}
                        onClick={() => setSelectedId(e.id)}
                      className={`cursor-pointer transition ${
                        isSel
                          ? "bg-[#C87533]/[0.07] border-l-2 border-l-[#C87533]"
                          : "hover:bg-zinc-800/30 border-l-2 border-l-transparent"
                      }`}
                    >
                      <td className="px-3 py-2 border-b border-zinc-900">
                        <QueueBadge queued={e.queued} />
                      </td>
                      <td className="px-3 py-2 border-b border-zinc-900">
                        <span className="inline-flex items-center gap-1.5">
                          <Icon className="w-3.5 h-3.5 text-zinc-500" />
                          <span className="font-mono text-[12px] text-zinc-200">{e.ext}</span>
                        </span>
                      </td>
                      <td className="px-3 py-2 border-b border-zinc-900 text-zinc-100">{e.label}</td>
                      <td className="px-3 py-2 border-b border-zinc-900 text-zinc-400">{e.group}</td>
                      <td className="px-3 py-2 border-b border-zinc-900">
                        <StateBadge state={e.state} />
                      </td>
                      <td className="px-3 py-2 border-b border-zinc-900">
                        <RiskBadge risk={e.risk} />
                      </td>
                      <td className="px-3 py-2 border-b border-zinc-900 text-zinc-500 truncate max-w-[420px]">
                        {e.description}
                      </td>
                    </tr>,
                    );
                  });
                  return rows;
                })()}
              </tbody>
            </table>
          </div>
        </main>

        {/* RIGHT — Inspector */}
        <aside className="flex flex-col min-h-0 overflow-y-auto p-3 gap-3 bg-[#0d0f12]/40">
          <Card title="Selected Entry" icon={Wrench} accent>
            <div className="flex items-start justify-between mb-3">
              <div>
                <div className="flex items-center gap-2">
                  <selected.icon className="w-4 h-4 text-[#C87533]" />
                  <span className="font-mono text-[14px] text-zinc-100">{selected.ext}</span>
                  <RiskBadge risk={selected.risk} />
                </div>
                <div className="text-[12px] text-zinc-300 mt-1">{selected.label}</div>
                <div className="text-[10.5px] text-zinc-500 mt-0.5">Pack: <span className="font-mono">{selected.pack}</span></div>
              </div>
              <label className="inline-flex items-center cursor-pointer">
                <span className="text-[10px] text-zinc-500 mr-2 font-semibold uppercase tracking-wider">
                  {selected.state === "enabled" ? "On" : "Off"}
                </span>
                <span className={`relative w-9 h-5 rounded-full transition ${selected.state === "enabled" ? "bg-[#C87533]" : "bg-zinc-700"}`}>
                  <span className={`absolute top-0.5 w-4 h-4 rounded-full bg-zinc-100 transition ${selected.state === "enabled" ? "left-4" : "left-0.5"}`} />
                </span>
              </label>
            </div>
            <div className="rounded border border-zinc-800 bg-[#0a0c0e] p-2 mb-3">
              <div className="text-[9.5px] text-zinc-600 font-semibold uppercase tracking-widest mb-1">Registry path (HKCU)</div>
              <div className="font-mono text-[10.5px] text-zinc-300 break-all leading-snug">
                HKCU\Software\Classes\{selected.ext}\ShellNew
              </div>
              {selected.template && (
                <div className="font-mono text-[10.5px] text-[#C87533] break-all leading-snug mt-1">
                  FileName = {selected.template}
                </div>
              )}
            </div>
            <div className="flex gap-1.5">
              <button
                onClick={() => setShowTemplateManager(true)}
                className="flex-1 inline-flex items-center justify-center gap-1.5 px-2 py-1.5 rounded border border-[#C87533]/40 bg-[#C87533]/10 hover:bg-[#C87533]/20 text-[#d6a378] text-[11px] font-semibold"
              >
                <PencilLine className="w-3 h-3" /> Edit Template
              </button>
              <button className="inline-flex items-center justify-center gap-1.5 px-2 py-1.5 rounded border border-zinc-700 bg-zinc-800/60 hover:bg-zinc-700/60 text-[11px] text-zinc-200">
                <Eye className="w-3 h-3" /> Preview
              </button>
            </div>
          </Card>

          <Card title="Custom Add (New+)" icon={Plus}>
            <div className="space-y-2">
              <div>
                <label className="block text-[10px] font-semibold uppercase tracking-widest text-zinc-500 mb-1">Pack</label>
                <select className="w-full h-7 rounded border border-zinc-800 bg-zinc-900/70 text-[12px] text-zinc-200 px-2 focus:outline-none focus:border-[#C87533]/60">
                  <option>cm-editar.user</option>
                  <option>cm-editar.core</option>
                  <option>cm-editar.dev</option>
                </select>
              </div>
              <div className="grid grid-cols-2 gap-2">
                <div>
                  <label className="block text-[10px] font-semibold uppercase tracking-widest text-zinc-500 mb-1">Extension</label>
                  <input defaultValue=".yaml" className="w-full h-7 rounded border border-zinc-800 bg-zinc-900/70 text-[12px] font-mono text-zinc-200 px-2 focus:outline-none focus:border-[#C87533]/60" />
                </div>
                <div>
                  <label className="block text-[10px] font-semibold uppercase tracking-widest text-zinc-500 mb-1">Display name</label>
                  <input defaultValue="YAML Document" className="w-full h-7 rounded border border-zinc-800 bg-zinc-900/70 text-[12px] text-zinc-200 px-2 focus:outline-none focus:border-[#C87533]/60" />
                </div>
              </div>
              <button className="w-full inline-flex items-center justify-center gap-1.5 mt-1 px-2 py-1.5 rounded bg-[#C87533] hover:bg-[#d28344] text-zinc-950 text-[11px] font-semibold">
                <Plus className="w-3.5 h-3.5" /> Add to New+ submenu
              </button>
            </div>
          </Card>

          <Card title="Runtime Status" icon={ShieldCheck}>
            <div className="grid grid-cols-2 gap-2 mb-2">
              <div className="rounded border border-emerald-600/30 bg-emerald-600/[0.06] px-2 py-1.5">
                <div className="text-[9px] font-semibold uppercase tracking-widest text-emerald-300/80">Elevation</div>
                <div className="text-[12px] text-emerald-300 font-semibold inline-flex items-center gap-1 mt-0.5">
                  <ShieldCheck className="w-3 h-3" /> Not required
                </div>
              </div>
              <div className="rounded border border-zinc-700 bg-zinc-800/40 px-2 py-1.5">
                <div className="text-[9px] font-semibold uppercase tracking-widest text-zinc-500">Scope</div>
                <div className="text-[12px] text-zinc-200 font-mono mt-0.5">HKCU only</div>
              </div>
            </div>
            <div className="space-y-1.5 text-[11px]">
              <div className="flex items-center justify-between">
                <span className="text-zinc-400">Visible in submenu</span>
                <span className="font-mono text-zinc-100">{visibleCount}</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-zinc-400">Pending changes</span>
                <span className="font-mono text-[#C87533]">{queuedCount}</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-zinc-400">Snapshots available (undo)</span>
                <span className="font-mono text-zinc-100">7</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-zinc-400">Last snapshot</span>
                <span className="font-mono text-zinc-300">2026-05-12 17:42</span>
              </div>
            </div>
            <div className="mt-2.5 rounded border border-amber-600/30 bg-amber-600/[0.06] px-2 py-1.5 inline-flex items-start gap-1.5 w-full">
              <ShieldAlert className="w-3.5 h-3.5 text-amber-300 mt-0.5" />
              <span className="text-[10.5px] text-amber-200 leading-snug">
                2 entries reference missing handlers. Apply will skip them and warn.
              </span>
            </div>
          </Card>
        </aside>
      </div>

      {/* ====== FOOTER ====== */}
      <footer className="relative z-10 flex items-center justify-between px-4 h-14 border-t border-zinc-800/80 bg-[#0e1013]/90 backdrop-blur">
        <div className="flex items-center gap-2">
          <FooterButton
            label="Preflight"
            icon={ShieldCheck}
            onClick={handlePreflight}
          />
          <FooterButton
            label="Apply Changes"
            icon={PlayCircle}
            variant="go"
            onClick={handleApplyClick}
          />
          <span className="mx-2 h-6 w-px bg-zinc-800" />
          <FooterButton
            label="Undo Last"
            icon={Undo2}
            onClick={handleUndoLast}
          />
          <FooterButton
            label="Undo All"
            icon={RotateCcw}
            variant="warn"
            onClick={handleUndoAll}
          />
          <FooterButton
            label="Flush Shell Cache"
            icon={Trash2}
            onClick={handleFlushShellCache}
          />
          <span className="mx-2 h-6 w-px bg-zinc-800" />
          <FooterButton
            label="Export Manifest"
            icon={Upload}
            onClick={handleExportManifest}
          />
          <FooterButton
            label="Import Manifest"
            icon={Download}
            onClick={handleImportManifest}
          />
        </div>
        <div className="flex items-center gap-3 text-[10.5px] font-mono text-zinc-500">
          <span>build 1.3.0+f4a2e</span>
          <span>·</span>
          <span>net 8.0 · avalonia 11.2</span>
          <span>·</span>
          <span className="inline-flex items-center gap-1">
            <span className="w-1.5 h-1.5 rounded-full bg-emerald-400" />
            Ready
          </span>
        </div>
      </footer>

      {showTemplateManager && <TemplateManagerModal onClose={() => setShowTemplateManager(false)} />}

      {showApplyConfirm && (
        <ApplyConfirmModal
          pendingCount={queuedCount}
          onCancel={() => setShowApplyConfirm(false)}
          onConfirm={handleApplyConfirm}
        />
      )}

      {applyToast && (
        <div className="absolute bottom-20 left-1/2 -translate-x-1/2 z-[55] inline-flex items-center gap-2 px-4 py-2 rounded-md border border-emerald-500/40 bg-[#0e1013]/95 backdrop-blur shadow-lg text-[12px] text-emerald-200">
          <Check className="w-3.5 h-3.5" />
          {applyToast}
        </div>
      )}

      {showHelp && (
        <HelpModal
          initialSection={helpSection}
          onClose={() => setShowHelp(false)}
        />
      )}

      {showSupport && (
        <SupportModal
          onClose={() => setShowSupport(false)}
          onAbout={() => {
            setShowSupport(false);
            setShowAbout(true);
          }}
        />
      )}

      {showAbout && <AboutDialog onClose={() => setShowAbout(false)} />}
    </div>
  );
}

export default Manager;
