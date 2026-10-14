// Copies the shared stylesheet and script to the demo site (ADR-0020).
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.join(path.dirname(fileURLToPath(import.meta.url)), "..");
const wwwroot = path.join(root, "..", "src", "SuperPower.Web", "wwwroot");

const files = [
  [path.join(root, "src", "css", "site.css"), path.join(wwwroot, "css", "site.css")],
  [path.join(root, "src", "js", "site.js"), path.join(wwwroot, "js", "site.js")],
];

for (const [from, to] of files) {
  fs.mkdirSync(path.dirname(to), { recursive: true });
  fs.copyFileSync(from, to);
  console.log(`${path.relative(root, from)} -> ${path.relative(root, to)}`);
}
