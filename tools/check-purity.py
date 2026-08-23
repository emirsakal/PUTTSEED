"""Enforces CLAUDE.md hard rules 1 and 2: core/src is float-free.

    python tools/check-purity.py [--self-test]

Exit 0 when core/src contains no float, double, System.Random, DateTime or
UnityEngine in CODE. Exit 1 with file:line for anything it finds.

The emphasis on CODE is the whole reason this file exists. The check used to
be a raw grep, written out twice — once in ci.yml, once in
scripts\\check-purity.bat, with a comment in the first calling itself a mirror
of the second. It failed on its own documentation: the doc comment on
DailyCalendar that explains DateTime is banned contains the word DateTime, and
a comment in CourseBake explaining that a Fix64 must not become a float
contains the word float. So the two lines describing the rule broke the rule,
CI went red, and stayed red for days while the core it was guarding was in
fact perfectly clean.

A grep cannot tell code from prose. This blanks comments and string literals
first — replacing them with spaces so line numbers still point at the right
place — and only then looks. Both callers now run this one implementation,
because a rule with two implementations has two chances to be wrong.
"""

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CORE = os.path.join(ROOT, "core", "src")

# The tokens that would make the simulation non-deterministic, plus the engine
# reference that would break the layering.
FORBIDDEN = ["float", "double", "System.Random", "DateTime", "UnityEngine"]


def blank_noncode(text):
    """Replaces comments and string/char literals with spaces, keeping newlines.

    Length and line breaks are preserved so a match's line number in the
    result is its line number in the file.
    """
    out = list(text)
    i, n = 0, len(text)
    state = None  # None | line | block | str | verbatim | char

    def blank(at):
        if out[at] != "\n":
            out[at] = " "

    while i < n:
        c = text[i]
        nxt = text[i + 1] if i + 1 < n else ""

        if state is None:
            if c == "/" and nxt == "/":
                state = "line"
                blank(i), blank(i + 1)
                i += 2
            elif c == "/" and nxt == "*":
                state = "block"
                blank(i), blank(i + 1)
                i += 2
            elif c == "@" and nxt == '"':
                state = "verbatim"
                blank(i), blank(i + 1)
                i += 2
            elif c == '"':
                state = "str"
                blank(i)
                i += 1
            elif c == "'":
                state = "char"
                blank(i)
                i += 1
            else:
                i += 1
            continue

        if state == "line":
            if c == "\n":
                state = None
            else:
                blank(i)
            i += 1
            continue

        if state == "block":
            if c == "*" and nxt == "/":
                blank(i), blank(i + 1)
                state = None
                i += 2
            else:
                blank(i)
                i += 1
            continue

        if state == "verbatim":
            # "" is an escaped quote inside a verbatim string, not the end.
            if c == '"' and nxt == '"':
                blank(i), blank(i + 1)
                i += 2
            elif c == '"':
                blank(i)
                state = None
                i += 1
            else:
                blank(i)
                i += 1
            continue

        # str or char
        quote = '"' if state == "str" else "'"
        if c == "\\" and i + 1 < n:
            blank(i), blank(i + 1)
            i += 2
        elif c == quote:
            blank(i)
            state = None
            i += 1
        else:
            blank(i)
            i += 1

    return "".join(out)


def violations():
    """Every forbidden token in code under core/src, as (path, line, token, text)."""
    patterns = [(token, re.compile(r"\b" + re.escape(token) + r"\b")) for token in FORBIDDEN]
    found = []
    for folder, _, names in os.walk(CORE):
        for name in sorted(names):
            if not name.endswith(".cs"):
                continue
            path = os.path.join(folder, name)
            with open(path, encoding="utf-8-sig") as handle:
                raw = handle.read()
            code = blank_noncode(raw)
            raw_lines = raw.splitlines()
            for number, line in enumerate(code.splitlines(), start=1):
                for token, pattern in patterns:
                    if pattern.search(line):
                        rel = os.path.relpath(path, ROOT).replace("\\", "/")
                        found.append((rel, number, token, raw_lines[number - 1].strip()))
    return found


def self_test():
    """Proves the blanker still hides prose AND still reveals code.

    Half of this guards against the bug that produced the file; the other half
    guards against the obvious over-correction, a check so forgiving it passes
    everything. A check nobody has watched fail is not known to work.
    """
    hidden = [
        "// a float here is prose",
        "/// <summary>DateTime is banned</summary>",
        "/* double\n   UnityEngine */",
        'var message = "float";',
        'var verbatim = @"C:\\double\\path";',
        "var quote = \"say \\\"float\\\" here\";",
    ]
    for sample in hidden:
        blanked = blank_noncode(sample)
        for token in FORBIDDEN:
            if re.search(r"\b" + re.escape(token) + r"\b", blanked):
                print("SELF-TEST FAILED: '%s' still visible in %r" % (token, sample))
                return 1

    visible = [
        "float x = 1f;",
        "private double Ratio;",
        "var rng = new System.Random(4);",
        "var now = DateTime.Now;",
        "using UnityEngine;",
        'var s = "text"; float after = 2f;',
    ]
    for sample in visible:
        blanked = blank_noncode(sample)
        if not any(re.search(r"\b" + re.escape(t) + r"\b", blanked) for t in FORBIDDEN):
            print("SELF-TEST FAILED: nothing found in %r, which is code" % sample)
            return 1

    # Line numbers have to survive, or a report points at the wrong line.
    if len(blank_noncode("a\n// b\nc\n").splitlines()) != 3:
        print("SELF-TEST FAILED: blanking changed the line count")
        return 1

    print("Self-test passed: prose is hidden, code is not, line numbers hold.")
    return 0


def main():
    if "--self-test" in sys.argv:
        return self_test()

    if not os.path.isdir(CORE):
        print("Purity check: %s not found." % CORE)
        return 1

    found = violations()
    if not found:
        print("Purity check passed: core/src code is free of "
              + ", ".join(FORBIDDEN) + ".")
        return 0

    for path, number, token, text in found:
        # ASCII only: this prints into a Windows console at cp1252 and into a
        # CI log, and a mangled byte in a failure message is a failure message
        # somebody has to squint at.
        print("PURITY VIOLATION %s:%d - '%s' in: %s" % (path, number, token, text))
    print("\n%d violation(s). core/ is fixed-point only: see CLAUDE.md hard rule 1."
          % len(found))
    return 1


if __name__ == "__main__":
    sys.exit(main())
