"""Adds a release's notes to the top of CHANGELOG.html, which the update dialog links to.

Usage: changelog.py VERSION < notes.md

The notes are the release's Markdown body. Only what release notes use is converted: ## and ###
headings, "- " list items, `code`, **bold** and [links](url). The "Full Changelog" line is left out.
Does nothing if CHANGELOG.html already has an entry for VERSION.
"""
import html
import re
import sys

CHANGELOG = "CHANGELOG.html"


def inline(text):
    text = html.escape(text, quote=False)
    text = re.sub(r"`([^`]+)`", r"<code>\1</code>", text)
    text = re.sub(r"\*\*([^*]+)\*\*", r"<strong>\1</strong>", text)
    text = re.sub(r"\[([^\]]+)\]\(([^)\s]+)\)", r'<a href="\2">\1</a>', text)
    return text


def to_html(version, notes):
    lines = []
    in_list = False
    for line in notes.replace("\r\n", "\n").split("\n"):
        line = line.strip()
        if line.startswith("**Full Changelog**"):
            continue
        is_item = line.startswith("- ") or line.startswith("* ")
        if in_list and not is_item:
            lines.append("    </ul>")
            in_list = False
        if not line:
            continue
        if line.startswith("### "):
            lines.append("    <h3>" + inline(line[4:]) + "</h3>")
        elif line.startswith("## "):
            lines.append("    <h2>" + inline(line[3:]) + "</h2>")
        elif is_item:
            if not in_list:
                lines.append("    <ul>")
                in_list = True
            lines.append("        <li>" + inline(line[2:]) + "</li>")
        else:
            lines.append("    <p>" + inline(line) + "</p>")
    if in_list:
        lines.append("    </ul>")
    if not lines or not lines[0].startswith("    <h2>"):
        lines.insert(0, "    <h2>SluMAN v" + version + "</h2>")
    return "\n".join(lines) + "\n"


def main():
    version = sys.argv[1]
    with open(CHANGELOG, encoding="utf-8") as f:
        page = f.read()
    if re.search(r"<h2>[^<]*v" + re.escape(version) + r"</h2>", page):
        print("CHANGELOG.html already has v" + version + ".")
        return
    body = re.search(r"<body[^>]*>\n", page)
    if body is None:
        sys.exit("CHANGELOG.html has no <body> tag")
    entry = to_html(version, sys.stdin.read())
    with open(CHANGELOG, "w", encoding="utf-8", newline="\n") as f:
        f.write(page[:body.end()] + entry + page[body.end():])
    print("Added v" + version + " to CHANGELOG.html.")


if __name__ == "__main__":
    main()
