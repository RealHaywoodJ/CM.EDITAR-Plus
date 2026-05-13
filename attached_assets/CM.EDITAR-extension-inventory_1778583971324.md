# CM.EDITAR+ Proposed Extension (.ext) Inventory

Date: 2026-05-12

**Purpose: inventory every extension currently compiled to go into CM.EDITAR+** 

Plus, do to the rise in AI we feel there is a huge need for AI power users; that we now add a proposed AI/Automation expansion set for modern LLM, ComfyUI, image-generation, agent, dataset, and model workflows.

## Summary

- Current wired catalog entries: 216 rows from `ExtensionCatalog.cs`.
- Current wired categories: Archives, Automation/Data, CAD/3D, Cloud Docs, Legacy, Media, Office/Docs, Omega Database, Power User, System, Text/Data.
- Duplicates intentionally exist in the current catalog where an extension appears in both a focused category and Omega Database, such as `.dwg`, `.dxf`, `.iso`, and `.zip`.
- AI/Automation currently wired: `.ahk`, `.arrow`, `.chat`, `.ckpt`, `.gguf`, `.ipynb`, `.jinja`, `.jsonl`, `.ndjson`, `.onnx`, `.prompt`, `.pt`, `.pth`, `.safetensors`, `.workflow`.
- AI/Automation proposed expansion: included below as candidates, not currently wired unless marked `Wired`.

## Current Wired Extensions By Category

| Category | Count | Extensions |
|---|---:|---|

| Archives | 10 | `.7z`, `.bz2`, `.cab`, `.gz`, `.iso`, `.pea`, `.rar`, `.tar`, `.xz`,`.winzip`, `.zip` |

| Automation/Data | 15 | `.ahk`, `.arrow`, `.chat`, `.ckpt`, `.gguf`, `.ipynb`, `.jinja`, `.jsonl`, `.ndjson`, `.onnx`, `.prompt`, `.pt`, `.pth`, `.safetensors`, `.workflow` |

| CAD/3D | 14 | `.dwg`, `.dxf`, `.f3d`, `.iam`, `.ifc`, `.iges`, `.igs`, `.ipt`, `.rvt`, `.skp`, `.sldasm`, `.sldprt`, `.step`, `.stp` |

| Cloud Docs | 8 | `.gdoc`, `.gdraw`, `.gform`, `.gsheet`, `.gslides`, `.keynote`, `.numbers`, `.pages` |

| Legacy | 11 | `.bat`, `.bin`, `.com`, `.dbf`, `.ebcdic`, `.fox`, `.hqx`, `.jcl`, `.pif`, `.sit`, `.sys` |

| Media | 11 | `.ai`, `.eps`, `.jpg`, `.mid`, `.mod`, `.mp3`, `.mp4`, `.png`, `.svg`, `.wav`, `.webp` |

| Office/Docs | 21 | `.accdb`, `.doc`, `.docx`, `.dot`, `.dotx`, `.mdb`, `.mpp`, `.odp`, `.ods`, `.odt`, `.one`, `.pdf`, `.ppsx`, `.ppt`, `.pptx`, `.pub`, `.vsd`, `.vsdx`, `.xls`, `.xlsm`, `.xlsx` |

| Omega Database | 82 | `.astro`, `.avsc`, `.bak`, `.bashrc`, `.bicep`, `.blend`, `.cjs`, `.cmake`, `.cmd`, `.conf`, `.cron`, `.crt`, `.dart`, `.db`, `.dcm`, `.dmg`, `.dwg`, `.dxf`, `.edi`, `.ex`, `.fasta`, `.fbx`, `.go`, `.gpg`, `.gradle`, `.graphql`, `.h`, `.heic`, `.heif`, `.hl7`, `.hpp`, `.iso`, `.java`, `.key`, `.kt`, `.less`, `.liquid`, `.lnk`, `.lock`, `.lua`, `.makefile`, `.mdx`, `.mjs`, `.mov`, `.obj`, `.ofx`, `.old`, `.ovpn`, `.parquet`, `.pcap`, `.pem`, `.php`, `.pl`, `.plist`, `.proto`, `.psd1`, `.psm1`, `.qif`, `.r`, `.rb`, `.rdp`, `.rs`, `.scss`, `.service`, `.sqlite`, `.stl`, `.svelte`, `.swift`, `.tf`, `.tfvars`, `.url`, `.vbs`, `.vhd`, `.vhdx`, `.vue`, `.wasm`, `.webloc`, `.wsf`, `.x12`, `.xbrl`, `.zip`, `.zshrc` |

| Power User | 20 | `.cpp`, `.cs`, `.csproj`, `.css`, `.dockerfile`, `.html`, `.js`, `.json`, `.jsx`, `.md`, `.ps1`, `.py`, `.sh`, `.sln`, `.sql`, `.toml`, `.ts`, `.tsx`, `.yaml`, `.yml` |

| System | 10 | `.clang-format`, `.editorconfig`, `.env`, `.eslintrc`, `.gitattributes`, `.gitignore`, `.htaccess`, `.npmrc`, `.prettierrc`, `.` |

| Text/Data | 14 | `.cfg`, `.csv`, `.ini`, `.log`, `.nfo`, `.properties`, `.readme`, `.reg`, `.rtf`, `.tsv`, `.txt`, `.wpd`, `.wps`, `.xml` |

## Current Wired Extensions With Labels

| Category | Extension | Label |
|---|---|---|

---

| Archives | `.7z` | 7-Zip Archive |
| Archives | `.bz2` | BZip2 Archive |
| Archives | `.cab` | Windows Cabinet |
| Archives | `.gz` | GZip Archive |
| Archives | `.iso` | ISO Image |
| Archives | `.pea` | PeaZip PEA Archive |
| Archives | `.rar` | WinRAR Archive |
| Archives | `.tar` | TAR Archive |
| Archives | `.xz` | XZ Archive |
| Archives | `.winzip` | Windows ZIP Archive |
| Archives | `.zip` | ZIP Archive |

---

| Automation/Data | `.ahk` | AutoHotkey Script |
| Automation/Data | `.arrow` | Arrow Dataset |
| Automation/Data | `.chat` | Chat Transcript |
| Automation/Data | `.ckpt` | Checkpoint Model |
| Automation/Data | `.gguf` | GGUF Model |
| Automation/Data | `.ipynb` | Jupyter Notebook |
| Automation/Data | `.jinja` | Jinja Template |
| Automation/Data | `.jsonl` | JSON Lines File |
| Automation/Data | `.ndjson` | NDJSON File |
| Automation/Data | `.onnx` | ONNX Model |
| Automation/Data | `.prompt` | Prompt File |
| Automation/Data | `.pt` | PyTorch Model |
| Automation/Data | `.pth` | PyTorch Weights |
| Automation/Data | `.safetensors` | SafeTensors Model |
| Automation/Data | `.workflow` | Workflow File |

---

| CAD/3D | `.dwg` | AutoCAD Drawing |
| CAD/3D | `.dxf` | DXF Drawing |
| CAD/3D | `.f3d` | Fusion 360 Design |
| CAD/3D | `.g` | CAD/CNC/3D Printing |
| CAD/3D | `.gcode` | G-code CNC/3D Printing |
| CAD/3D | `.iam` | Inventor Assembly |
| CAD/3D | `.ifc` | IFC BIM Model |
| CAD/3D | `.iges` | IGES 3D Model |
| CAD/3D | `.igs` | IGES 3D Model |
| CAD/3D | `.ipt` | Inventor Part |
| CAD/3D | `.max` | 3ds Max Scene File |
| CAD/3D | `.rvt` | Revit Project |
| CAD/3D | `.skp` | SketchUp Model |
| CAD/3D | `.sldasm` | SolidWorks Assembly |
| CAD/3D | `.sldprt` | SolidWorks Part |
| CAD/3D | `.step` | STEP 3D Model |
| CAD/3D | `.stp` | STEP 3D Model |

---

--
{
| Android Package | `.apk` | Android Package File |
| Google Docs | `.gdoc` | Google Docs |
| Google Docs | `.gdraw` | Google Drawing |
| Google Docs | `.gform` | Google Form |
| Google Docs | `.gsheet` | Google Sheets |
| Google Docs | `.gslides` | Google Slides |
| Amazon Kindle | `.kcb` | Kindle Proj. File |
| Apple Keynote | `.key` | Apple Keynote Presentation |
| Apple Keynote | `.key-tef` | iCloud Optimized Keynote File |
| Apple Keynote | `.keynote` | Apple Keynote Presentation |
| Apple Numbers | `.numbers` | Apple Numbers Spreadsheet |
| Apple Docs/Pages | `.pages` | Apple Pages Document |
| Legacy Google/Apple Cal | `.ics` | Legacy Calendar File |
| Apple Calendar | `.ical` | Apple iCalendar File |
}
--

---

| Legacy | `.bat` | Batch File |
| Legacy | `.bin` | Binary File |
| Legacy | `.com` | COM Executable |
| Legacy | `.dbf` | dBase Database |
| Legacy | `.ebcdic` | EBCDIC Data |
| Legacy | `.fox` | FoxPro File |
| Legacy | `.hqx` | BinHex File |
| Legacy | `.ima` | Legacy Floppy File |
| Legacy | `.img` | Generic Disc Image - Raw Binary |
| Legacy | `.imz` | Zipped Disk Image File |
| Legacy | `.iso` | Disc Image File |
| Legacy | `.isz` | Zipped ISO Image File |
| Legacy | `.jcl` | JCL Script |
| Legacy | `.pif` | Program Information File |
| Legacy | `.sit` | StuffIt Archive |
| Legacy | `.sys` | System File |

---

| Media | `.ai` | Adobe Illustrator File |
| Media | `.als` | Ableton Live Set File |
| Media | `.braw` | Blackmagic RAW Video File |
| Media | `.dae` | Digital Asset Exchange File |
| Media | `.eml` | E-Mail Message File |
| Media | `.eps` | EPS File |
| Media | `.gif` | GIF Media Clip |
| Media | `.icns` | MacOS Icon Format |
| Media | `.ico` | ICO Icon Format |
| Media | `.iff` | IFF Container Format - Elec. Arts |
| Media | `.jpg` | JPEG Image |
| Media | `.hdr` | HDR Image |
| Media | `.mid` | MIDI File |
| Media | `.mod` | Module Audio |
| Media | `.mp3` | MP3 Audio |
| Media | `.mp4` | MP4 Video |
| Media | `.png` | PNG Image |
| Media | `.svg` | SVG Image |
| Media | `.tiff` | Tagged Img File Format |
| Media | `.wav` | WAV Audio |
| Media | `.webm` | WebM Video |
| Media | `.webp` | WebP Image |

---

| Office/Docs | `.accdb` | Microsoft Access Database |
| Office/Docs | `.doc` | Microsoft Word 97-2003 Document |
| Office/Docs | `.docx` | Microsoft Word Document |
| Office/Docs | `.dot` | Word 97-2003 Template |
| Office/Docs | `.dotx` | Word Template |
| Office/Docs | `.kpr` | KDE Slideshow File |
| Office/Docs | `.mdb` | Microsoft Access 97-2003 Database |
| Office/Docs | `.mpp` | Microsoft Project Document |
| Office/Docs | `.odp` | OpenDocument Presentation |
| Office/Docs | `.ods` | OpenDocument Spreadsheet |
| Office/Docs | `.odt` | OpenDocument Text |
| Office/Docs | `.one` | OneNote Section |
| Office/Docs | `.pdf` | PDF Document |
| Office/Docs | `.ppsx` | PowerPoint Show |
| Office/Docs | `.ppt` | Microsoft PowerPoint 97-2003 Presentation |
| Office/Docs | `.pptx` | Microsoft PowerPoint Presentation |
| Office/Docs | `.pub` | Microsoft Publisher Document |
| Office/Docs | `.vsd` | Microsoft Visio Drawing |
| Office/Docs | `.vsdx` | Microsoft Visio Drawing |
| Office/Docs | `.xls` | Microsoft Excel 97-2003 Worksheet |
| Office/Docs | `.xltx` | Microsoft Excel Spreadsheet Template |
| Office/Docs | `.xlsm` | Excel Macro-Enabled Workbook |
| Office/Docs | `.xlsx` | Microsoft Excel Worksheet |

---

| Omega Database | `.astro` | Astro Component |
| Omega Database | `.avsc` | Avro Schema |
| Omega Database | `.bak` | Backup File |
| Omega Database | `.bashrc` | Bash RC File |
| Omega Database | `.bicep` | Bicep Template |
| Omega Database | `.blend` | Blender Project |
| Omega Database | `.cjs` | CommonJS JavaScript |
| Omega Database | `.cmake` | CMake Script |
| Omega Database | `.cmd` | Command Script |
| Omega Database | `.conf` | Config File |
| Omega Database | `.cron` | Cron Schedule |
| Omega Database | `.crt` | Certificate File |
| Omega Database | `.crypt14` | WhatsApp Encrypted DB File |
| Omega Database | `.dart` | Dart Source |
| Omega Database | `.db` | Database File |
| Omega Database | `.dcm` | DICOM File |
| Omega Database | `.dmg` | Apple Disk Image |
| Omega Database | `.dwg` | AutoCAD Drawing |
| Omega Database | `.dxf` | DXF Drawing |
| Omega Database | `.edi` | EDI File |
| Omega Database | `.ex` | Elixir Source |
| Omega Database | `.fasta` | FASTA Sequence |
| Omega Database | `.fbx` | FBX 3D Model |
| Omega Database | `.geo` | Gmsh Geo File |
| Omega Database | `.go` | Go Source |
| Omega Database | `.gpg` | GPG Key |
| Omega Database | `.gradle` | Gradle Build Script |
| Omega Database | `.graphql` | GraphQL Schema |
| Omega Database | `.glsl` | OpenGL Shader Source |
| Omega Database | `.gltf` | glTF Trans Source |
| Omega Database | `.glb` | glTF Binary SF Source |
| Omega Database | `.gts` | GNU t.s. Format |
| Omega Database | `.gr2` | Granny3D/Epic Games Model File |
| Omega Database | `.gmax` | GMax Scene File |
| Omega Database | `.h` | C/C++ Header |
| Omega Database | `.heic` | HEIC Image |
| Omega Database | `.heif` | HEIF Image |
| Omega Database | `.hl7` | HL7 File |
| Omega Database | `.hpp` | C++ Header |
| Omega Database | `.ib` | InterBase DB File |
| Omega Database | `.ibd` | InnoDB Table Data File |
| Omega Database | `.iso` | ISO Image |
| Omega Database | `.java` | Java Source |
| Omega Database | `.key` | Private Key File |
| Omega Database | `.kt` | Kotlin Source |
| Omega Database | `.less` | LESS Stylesheet |
| Omega Database | `.liquid` | Liquid Template |
| Omega Database | `.lnk` | Shortcut File |
| Omega Database | `.lock` | Lock File |
| Omega Database | `.lua` | Lua Script |
| Omega Database | `.makefile` | Makefile |
| Omega Database | `.mdx` | MDX Document |
| Omega Database | `.mjs` | ES Module JavaScript |
| Omega Database | `.mov` | QuickTime Movie |
| Omega Database | `.obj` | Wavefront OBJ |
| Omega Database | `.ofx` | OFX Banking Data |
| Omega Database | `.old` | Old File |
| Omega Database | `.ovpn` | OpenVPN Config |
| Omega Database | `.parquet` | Parquet File |
| Omega Database | `.pcap` | Packet Capture |
| Omega Database | `.pem` | PEM Certificate |
| Omega Database | `.php` | PHP Script |
| Omega Database | `.pl` | Perl Script |
| Omega Database | `.plist` | Apple Property List |
| Omega Database | `.proto` | Protocol Buffers |
| Omega Database | `.psd1` | PowerShell Data File |
| Omega Database | `.psm1` | PowerShell Module |
| Omega Database | `.qif` | Quicken Interchange |
| Omega Database | `.r` | R Script |
| Omega Database | `.rb` | Ruby Script |
| Omega Database | `.rdp` | Remote Desktop File |
| Omega Database | `.rs` | Rust Source |
| Omega Database | `.sb3` | Scratch 3.0 Project File |
| Omega Database | `.scss` | SCSS Stylesheet |
| Omega Database | `.service` | Systemd Service |
| Omega Database | `.sqlite` | SQLite Database |
| Omega Database | `.stl` | STL Model |
| Omega Database | `.svelte` | Svelte Component |
| Omega Database | `.swift` | Swift Source |
| Omega Database | `.tf` | Terraform File |
| Omega Database | `.tfvars` | Terraform Variables |
| Omega Database | `.url` | Internet Shortcut |
| Omega Database | `.vbs` | VBScript File |
| Omega Database | `.vhd` | Virtual Hard Disk |
| Omega Database | `.vhdx` | Hyper-V Virtual Disk |
| Omega Database | `.vue` | Vue Component |
| Omega Database | `.wasm` | WebAssembly Binary |
| Omega Database | `.webloc` | Web Location |
| Omega Database | `.wsf` | Windows Script File |
| Omega Database | `.x12` | ANSI X12 File |
| Omega Database | `.xbrl` | XBRL Filing |
| Omega Database | `.zip` | ZIP Archive |
| Omega Database | `.zshrc` | Zsh RC File |

---

| Power User | `.app` | MacOS Application Bundle File |
| Power User | `.c` | C/C++  Source Code File |
| Power User | `.cpp` | C++ Source File |
| Power User | `.cs` | C# Source File |
| Power User | `.csproj` | C# Project File |
| Power User | `.css` | CSS Stylesheet |
| Power User | `.dockerfile` | Dockerfile |
| Power User | `.exe` | Win Executable File |
| Power User | `.html` | HTML Document |
| Power User | `.gs` | Google Apps Script File |
| Power User | `.js` | JavaScript File |
| Power User | `.json` | JSON File |
| Power User | `.jsx` | React JSX File |
| Power User | `.ksh` | Unix Korn Shell Script |
| Power User | `.md` | Markdown Document |
| Power User | `.msi` | Microsoft Install File |
| Power User | `.pdb` | Program Database File |
| Power User | `.ps1` | PowerShell Script |
| Power User | `.py` | Python Script |
| Power User | `.sh` | Shell Script |
| Power User | `.sln` | Visual Studio Solution |
| Power User | `.sql` | SQL Script |
| Power User | `.toml` | TOML Config |
| Power User | `.ts` | TypeScript File |
| Power User | `.tsx` | TypeScript React File |
| Power User | `.yaml` | YAML File |
| Power User | `.yml` | YAML File |

---

| System | `.` | New Extensionless File |
| System | `.csr` | Cert. Signing Request File |
| System | `.clang-format` | Clang Format Config |
| System | `.editorconfig` | EditorConfig File |
| System | `.env` | Environment File |
| System | `.eslintrc` | ESLint Config |
| System | `.gitattributes` | Git Attributes Config File |
| System | `.gitignore` | Git Ignore Patterns File |
| System | `.gitignore` | Git Submodules Config File |
| System | `.htaccess` | Apache HTAccess File |
| System | `.npmrc` | NPM RC File |
| System | `.prettierrc` | Prettier Config |

---

| Text/Data | `.cfg` | Config File |
| Text/Data | `.csv` | CSV File |
| Text/Data | `.ini` | Plain-Text Init File |
| Text/Data | `.inf` | INF Win. Setup File |
| Text/Data | `.ins` | INS Legacy Network File |
| Text/Data | `.log` | Log File |
| Text/Data | `.nfo` | NFO File |
| Text/Data | `.properties` | Properties File |
| Text/Data | `.readme` | README File |
| Text/Data | `.reg` | Registry Script |
| Text/Data | `.rtf` | Rich Text Document |
| Text/Data | `.tsv` | TSV File |
| Text/Data | `.txt` | Text Document |
| Text/Data | `.wpd` | WordPerfect Document |
| Text/Data | `.wps` | Works Document |
| Text/Data | `.xml` | XML File |

---
--
## AI & Automation Inventory

Status meanings:

- `Wired`: already present in CM.EDITAR's current catalog.
- `Candidate`: recommended to add later, but not currently wired.

| Status | Extension / File Pattern | Suggested Label | Use Case / Tool Family |
|---|---|---|---|
| Wired | `.ahk` | AutoHotkey Script | Windows automation scripts |
| Wired | `.arrow` | Arrow Dataset | Apache Arrow / data science artifacts |
| Wired | `.chat` | Chat Transcript | Generic AI/chat transcript |
| Wired | `.ckpt` | Checkpoint Model | Stable Diffusion / ML checkpoint |
| Wired | `.gguf` | GGUF Model | llama.cpp, Ollama-style local model artifact |
| Wired | `.ipynb` | Jupyter Notebook | Notebook and AI/data-science workflow |
| Wired | `.jinja` | Jinja Template | Prompt/code generation templates |
| Wired | `.jsonl` | JSON Lines Dataset | LLM fine-tuning/evals/log datasets |
| Wired | `.ndjson` | NDJSON Dataset | Streaming/newline-delimited data |
| Wired | `.onnx` | ONNX Model | Cross-framework neural-network model |
| Wired | `.prompt` | Prompt File | Reusable prompt text |
| Wired | `.pt` | PyTorch Model | PyTorch model artifact |
| Wired | `.pth` | PyTorch Weights | PyTorch weights/checkpoints |
| Wired | `.safetensors` | SafeTensors Model | Hugging Face / Diffusers / ComfyUI model weights |
| Wired | `.workflow` | Workflow File | Generic workflow definition |
| Candidate | `.bin` | Binary Model Weights | Transformers/PyTorch model binaries, also generic binary |
| Candidate | `.bpe` | BPE Tokenizer Model | Tokenizer merge/vocabulary workflows |
| Candidate | `.chat.json` | AI Chat Export JSON | Structured ChatGPT/Claude/Gemini export convention |
| Candidate | `.chat.md` | AI Chat Markdown Export | Human-readable AI conversation archive |
| Candidate | `.chatml` | ChatML Prompt File | Chat message/prompt format convention |
| Candidate | `.clip` | CLIP Model Artifact | Image/text encoder artifacts |
| Candidate | `.cmfy` | ComfyUI Workflow Bundle | Proposed compact ComfyUI bundle alias |
| Candidate | `.comfy.json` | ComfyUI Workflow JSON | ComfyUI graph/workflow JSON convention |
| Candidate | `.conversations.json` | ChatGPT Export Conversations | OpenAI/ChatGPT data-export conversation JSON filename |
| Candidate | `.csv` | CSV AI Dataset | Fine-tuning/eval/tabular data, already wired as Text/Data |
| Candidate | `.diffusers` | Diffusers Pipeline Marker | Hugging Face Diffusers project/pipeline convention |
| Candidate | `.dduf` | Diffusers DDUF Artifact | Diffusers model packaging / single-file model artifact |
| Candidate | `.emb` | Embedding File | Generic embedding/vector artifact |
| Candidate | `.embedding` | Embedding Artifact | Vector/embedding output |
| Candidate | `.eval` | Evaluation Spec | LLM evaluation/task spec |
| Candidate | `.eval.json` | Evaluation JSON | LLM eval configuration/results |
| Candidate | `.eval.yaml` | Evaluation YAML | LLM eval configuration |
| Candidate | `.faiss` | FAISS Vector Index | Local vector search index |
| Candidate | `.ggml` | GGML Model | Legacy llama.cpp/ggml model artifact |
| Candidate | `.gltf` | GLTF 3D Asset | AI-generated 3D / asset workflows |
| Candidate | `.glb` | GLB 3D Asset | Binary GLTF asset, AI-generated 3D workflows |
| Candidate | `.hf` | Hugging Face Metadata | Local Hugging Face artifact marker/convention |
| Candidate | `.h5` | Keras/HDF5 Model | TensorFlow/Keras model artifact |
| Candidate | `.hdf5` | HDF5 Model/Data | ML model/data artifact |
| Candidate | `.index` | Model/Vector Index | TensorFlow/checkpoint/vector index |
| Candidate | `.json` | AI Config JSON | Tool/model/workflow config, already wired as Power User |
| Candidate | `.keras` | Keras Model | Native Keras model format |
| Candidate | `.llamafile` | Llamafile Executable Model | llama.cpp packaged model/app artifact |
| Candidate | `.lora` | LoRA Adapter | LoRA adapter marker/convention |
| Candidate | `.loras` | LoRA Collection | Local LoRA pack marker/convention |
| Candidate | `.mcp.json` | MCP Server Config | Model Context Protocol server/tool config convention |
| Candidate | `.mcp.yaml` | MCP Server Config YAML | Model Context Protocol server/tool config convention |
| Candidate | `.metadata.json` | AI Metadata JSON | Model/workflow/card metadata |
| Candidate | `.midjourney` | Midjourney Prompt Set | Proposed prompt/style reference container |
| Candidate | `.mj.txt` | Midjourney Prompt Text | Midjourney prompt library convention |
| Candidate | `.model` | Generic Model Artifact | Generic ML model file |
| Candidate | `.modelcard` | Model Card | Model documentation/metadata |
| Candidate | `.nemo` | NVIDIA NeMo Model | NVIDIA NeMo model package |
| Candidate | `.npz` | NumPy Compressed Array | ML tensors/datasets |
| Candidate | `.npy` | NumPy Array | ML tensors/datasets |
| Candidate | `.ort` | ONNX Runtime Model | ONNX Runtime optimized model |
| Candidate | `.pb` | TensorFlow Graph | TensorFlow protobuf graph/model |
| Candidate | `.pbtxt` | TensorFlow Text Graph | TensorFlow graph/config text protobuf |
| Candidate | `.pkl` | Pickle Model/Data | Python serialized object, risky |
| Candidate | `.pickle` | Pickle Model/Data | Python serialized object, risky |
| Candidate | `.prompt.md` | Prompt Markdown | Prompt library with documentation |
| Candidate | `.prompt.json` | Prompt JSON | Structured prompt template |
| Candidate | `.prompt.yaml` | Prompt YAML | Structured prompt template |
| Candidate | `.prompty` | Prompty File | Prompt flow/prompt asset convention |
| Candidate | `.q4_0.gguf` | Quantized GGUF Variant | Quant naming appears in filenames before `.gguf` |
| Candidate | `.q5_k_m.gguf` | Quantized GGUF Variant | Quant naming appears in filenames before `.gguf` |
| Candidate | `.sft` | Supervised Fine-Tuning Data | SFT dataset/training artifact |
| Candidate | `.spm` | SentencePiece Model | Tokenizer model |
| Candidate | `.t5x` | T5X Checkpoint | Google/T5X training artifact convention |
| Candidate | `.tflite` | TensorFlow Lite Model | Edge/mobile ML model |
| Candidate | `.tokenizer` | Tokenizer Artifact | Generic tokenizer asset |
| Candidate | `.tokenizer.json` | Tokenizer JSON | Hugging Face tokenizer file |
| Candidate | `.tokens` | Token Data | Token list/cache convention |
| Candidate | `.trt` | TensorRT Engine | NVIDIA TensorRT engine |
| Candidate | `.tsv` | TSV AI Dataset | Fine-tuning/eval/tabular data, already wired as Text/Data |
| Candidate | `.vae` | VAE Model | Stable Diffusion VAE artifact convention |
| Candidate | `.vec` | Vector Embeddings | Word/vector embedding file |
| Candidate | `.vectors` | Vector Store Data | Vector store artifact convention |
| Candidate | `.webp` | Generated WebP Image | Midjourney/ComfyUI/image-generation output, already wired as Media |
| Candidate | `.workflow.json` | Workflow JSON | ComfyUI/automation workflow convention |
| Candidate | `.yaml` | AI Config YAML | Model/pipeline/workflow config, already wired as Power User |
| Candidate | `.yml` | AI Config YAML | Model/pipeline/workflow config, already wired as Power User |
| Candidate | `adapter_config.json` | LoRA Adapter Config | Hugging Face PEFT/LoRA config filename |
| Candidate | `adapter_model.bin` | LoRA Adapter Weights | Hugging Face PEFT/LoRA weights filename |
| Candidate | `adapter_model.safetensors` | LoRA Adapter SafeTensors | Hugging Face PEFT/LoRA weights filename |
| Candidate | `config.json` | Model Config | Hugging Face/model config filename |
| Candidate | `generation_config.json` | Generation Config | Hugging Face generation settings |
| Candidate | `merges.txt` | Tokenizer Merges | BPE tokenizer merges |
| Candidate | `model.safetensors` | SafeTensors Model | Common Hugging Face model filename |
| Candidate | `pytorch_model.bin` | PyTorch Model | Common Hugging Face PyTorch model filename |
| Candidate | `special_tokens_map.json` | Tokenizer Special Tokens | Hugging Face tokenizer metadata |
| Candidate | `tokenizer.model` | SentencePiece Tokenizer | Tokenizer model filename |
| Candidate | `tokenizer_config.json` | Tokenizer Config | Hugging Face tokenizer config |
| Candidate | `vocab.json` | Tokenizer Vocabulary | Tokenizer vocabulary |
| Candidate | `workflow.json` | ComfyUI Workflow | Common ComfyUI workflow filename |

## Recommended AI/Automation Category Split

If CM.EDITAR grows the catalog, these subcategories would make the AI-heavy section easier to navigate:

| Proposed Subcategory | Extensions / Patterns |
|---|---|
| AI Chat & Prompt Archives | `.chat`, `.chat.json`, `.chat.md`, `.chatml`, `.prompt`, `.prompt.md`, `.prompt.json`, `.prompt.yaml`, `.prompty`, `.midjourney`, `.mj.txt` |
| ComfyUI / Image Workflow | `.workflow`, `.workflow.json`, `.comfy.json`, `.cmfy`, `.safetensors`, `.ckpt`, `.vae`, `.lora`, `.loras`, `.png`, `.jpg`, `.jpeg`, `.webp` |
| Local LLM Models | `.gguf`, `.ggml`, `.bin`, `.safetensors`, `.pt`, `.pth`, `.onnx`, `.ort`, `.llamafile`, `.model`, `.modelcard` |
| Tokenizers | `.tokenizer`, `.tokenizer.json`, `.spm`, `.bpe`, `.tokens`, `tokenizer.model`, `tokenizer_config.json`, `special_tokens_map.json`, `vocab.json`, `merges.txt` |
| Fine-Tuning / Eval Data | `.jsonl`, `.ndjson`, `.csv`, `.tsv`, `.parquet`, `.arrow`, `.sft`, `.eval`, `.eval.json`, `.eval.yaml` |
| Vector / RAG Data | `.faiss`, `.vec`, `.vectors`, `.emb`, `.embedding`, `.sqlite`, `.db`, `.jsonl`, `.parquet` |
| Agent / Tool Config | `.mcp.json`, `.mcp.yaml`, `.json`, `.yaml`, `.yml`, `.toml`, `.env` |
| ML Framework Models | `.keras`, `.h5`, `.hdf5`, `.pb`, `.pbtxt`, `.tflite`, `.trt`, `.nemo`, `.npy`, `.npz`, `.pkl`, `.pickle` |

## Notes For The Next Catalog Update

- Treat model/checkpoint formats as risky or high-trust: `.ckpt`, `.pt`, `.pth`, `.pkl`, `.pickle`, `.bin`, `.llamafile`, `.trt`, `.pb`, `.h5`, `.hdf5`.
- Prefer `.safetensors` over pickle-backed formats when possible.
- For ChatGPT, Claude, and similar assistants, most exports are not proprietary extensions; they are usually `.zip`, `.json`, `.html`, `.md`, `.txt`, `.pdf`, or tool-specific bundles.
- For ComfyUI, workflows are usually JSON and may be embedded in generated images; model files commonly include `.safetensors`, `.ckpt`, `.pt`, `.pth`, `.bin`, `.onnx`, `.gguf`, and sometimes `.sft`.
- For Midjourney, prompt/style data is mostly text or web-account data, while generated/reference images are common image extensions such as `.png`, `.jpg`, `.jpeg`, `.gif`, and `.webp`.

## Source Notes

# THERE ARE MANY, MANY FILE EXTENSIONS THAT ARE NOT INCLUDED WITHIN THIS LIST THAT WE WOULD LIKE TO ALSO ADD TO OUR SOFTWARE. THIS IS ONLY A SMALL LIST OF EXTENSIONS TO START. YOU CAN FIND MORE HERE: https://fileinfo.com/  **OR** https://github.com/dyne/file-extension-list




- Current wired list came from `src/Shamun.ContextMenuManager/src/Shamun.ContextMenuManager.Core/Data/ExtensionCatalog.cs`.
- ComfyUI docs describe JSON workflows and model placement conventions, including checkpoint/LoRA/VAE folders and `.safetensors` examples: https://docs.comfy.org/development/core-concepts/models
- OpenAI Help describes ChatGPT data export as a downloadable ZIP: https://help.openai.com/en/articles/7260999-how-do-i-export-my-chatgpt-history-and-data
- Claude Help describes artifacts as text/code/content surfaces, not a single proprietary file extension: https://support.claude.com/en/articles/9487310-what-are-artifacts-and-how-do-i-use-them
- Midjourney docs list accepted image file extensions for style reference workflows: https://docs.midjourney.com/hc/en-us/articles/32180011136653-Style-Reference
- Hugging Face Safetensors docs identify `.safetensors` model files: https://huggingface.co/docs/safetensors/index
- llama.cpp docs identify GGUF as the binary model format used by llama.cpp: https://www.mintlify.com/ggml-org/llama.cpp/concepts/gguf-format
- PyTorch docs describe ONNX as an open model representation format: https://docs.pytorch.org/docs/stable/onnx.html
