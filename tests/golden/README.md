# Golden extraction files

`<part>.txt` is the NotRag prototype's extracted text of
`admincode/ADMINISTRATIVE CODE 41 <part>.htm`. `CorpusGoldenTests` requires DocQA's C# extractor
to reproduce each one byte for byte (plan item 2).

- **Source.** The prototype's own `extract_corpus()`, called by `tools/generate_golden.py`, not
  `extract.py --show`. On Windows, `print` would turn `\n` into `\r\n`.
- **Generated.** 2026-09-14, with the prototype's pinned environment: lxml 6.1.3,
  libxml2 2.11.9, Python 3.13.14.
- **Format.** UTF-8 with no BOM, `\n` line endings, and no trailing newline. `.gitattributes`
  marks these files `-text`, so git never rewrites them.

Do not edit these files by hand. To regenerate them, from the repository root:

```
D:\code\NotRag\.venv\Scripts\python.exe tools\generate_golden.py
```
