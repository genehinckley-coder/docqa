"""One-off: write the NotRag prototype's extraction output as DocQA's golden files (plan item 2).

The C# extractor must reproduce these byte for byte. Run it with the prototype's own virtual
environment, so the prototype's pinned lxml does the parsing:

    D:\\code\\NotRag\\.venv\\Scripts\\python.exe tools\\generate_golden.py

It imports the prototype's ``extract_corpus()`` instead of running ``extract.py --show``. On
Windows, ``print`` translates ``\\n`` to ``\\r\\n``, and shell redirection can re-encode the
output; either would show up as a parser difference that is not one. Each document is written
as UTF-8 with no BOM and ``\\n`` line endings.

The prototype is frozen and is only read, never modified. It reads this repository's
``admincode/`` pages, which are byte-identical to the prototype's.
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
CORPUS_DIR = REPO_ROOT / "admincode"
GOLDEN_DIR = REPO_ROOT / "tests" / "golden"
DEFAULT_PROTOTYPE = Path(r"D:\code\NotRag")

#: Golden files are named by Part number: "ADMINISTRATIVE CODE 41 175.htm" -> "175.txt".
_PART = re.compile(r"ADMINISTRATIVE CODE 41 (\d+)\.html?")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument(
        "--prototype",
        type=Path,
        default=DEFAULT_PROTOTYPE,
        help=f"the NotRag prototype's folder (default: {DEFAULT_PROTOTYPE})",
    )
    args = parser.parse_args(argv)

    sys.path.insert(0, str(args.prototype))
    import lxml  # noqa: E402  (the prototype's environment, after the path change)
    from lxml import etree  # noqa: E402
    from extract import extract_corpus  # noqa: E402

    documents = extract_corpus(CORPUS_DIR)
    GOLDEN_DIR.mkdir(parents=True, exist_ok=True)
    for document in documents:
        match = _PART.fullmatch(document.filename)
        if match is None:
            print(f"Unexpected corpus file name: {document.filename}", file=sys.stderr)
            return 1
        path = GOLDEN_DIR / f"{match.group(1)}.txt"
        with open(path, "w", encoding="utf-8", newline="\n") as file:
            file.write(document.text)
        print(f"{path.relative_to(REPO_ROOT)}: {len(document.text):,} characters")

    libxml2 = ".".join(str(part) for part in etree.LIBXML_VERSION)
    print(f"lxml {lxml.__version__}, libxml2 {libxml2}, Python {sys.version.split()[0]}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
