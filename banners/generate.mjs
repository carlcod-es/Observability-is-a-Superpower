// Generates rubber-duck banner SVGs (1600x900, 16:9; subjects kept in the vertical middle so the
// 18rem-high banner crop and the 16:9 card crop both show them). File names = page node names.
// Usage: node banners/generate.mjs banners
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const out = process.argv[2];
mkdirSync(out, { recursive: true });

const C = {
  duck: "#F5B700", shade: "#D99A00", bill: "#FF8C42", billShade: "#E0702A",
  ink: "#1B3A4B", white: "#FFFFFF", cream: "#FFF9EC", sand: "#FFF0D1",
  water: "#6EC1E4", waterDark: "#3E9CC6", primary: "#B03F00", muted: "#4A6270",
};

// Side-view rubber duck facing right, origin at body centre.
function duck(x, y, s = 1, { flip = false, extra = "", hat = "" } = {}) {
  const t = `translate(${x} ${y}) scale(${flip ? -s : s} ${s})`;
  return `<g transform="${t}">
<path d="M-150-40C-175-95-150-120-120-90-90-60-40-75 0-75 90-75 140-40 140 10 140 70 80 90 0 90-90 90-150 60-150 10Z" fill="${C.duck}"/>
<ellipse cx="-20" cy="15" rx="70" ry="40" fill="${C.shade}" transform="rotate(-12 -20 15)"/>
<circle cx="65" cy="-105" r="65" fill="${C.duck}"/>
<path d="M118-110C160-118 190-105 186-88 182-72 150-70 120-78Z" fill="${C.bill}"/>
<path d="M120-84C150-80 176-80 184-86 178-70 150-66 122-72Z" fill="${C.billShade}"/>
<circle cx="85" cy="-125" r="11" fill="${C.ink}"/><circle cx="89" cy="-129" r="4" fill="${C.white}"/>
<ellipse cx="40" cy="-140" rx="22" ry="12" fill="${C.white}" opacity=".45" transform="rotate(-30 40 -140)"/>
${hat}${extra}</g>`;
}

const waves = (y, color, amp = 18) => {
  let d = `M0 ${y}`;
  for (let x = 0; x < 1600; x += 100) d += ` q25 ${-amp} 50 0 t50 0`;
  return `<path d="${d} V900 H0Z" fill="${color}"/>`;
};
let front = "";
const water = (y = 600) => { front = waves(y + 45, C.waterDark, 14); return waves(y, C.water); };
const bubbles = (pts) => pts.map(([x, y, r]) => `<circle cx="${x}" cy="${y}" r="${r}" fill="${C.white}" opacity=".6"/>`).join("");

// Small reusable props.
const hardHat = `<path d="M5-160A62 55 0 0 1 125-160Z" fill="${C.primary}"/><rect x="-5" y="-166" width="140" height="14" rx="7" fill="${C.primary}"/>`;
const gradCap = `<path d="M10-180L70-205 130-180 70-155Z" fill="${C.ink}"/><path d="M118-185V-150" stroke="${C.ink}" stroke-width="4"/><circle cx="118" cy="-148" r="7" fill="${C.bill}"/>`;
const bowTie = `<path d="M40-38L70-50 70-26ZM100-38L70-50 70-26Z" fill="${C.primary}"/><circle cx="70" cy="-38" r="7" fill="${C.primary}"/>`;
const glasses = `<circle cx="88" cy="-125" r="22" fill="none" stroke="${C.ink}" stroke-width="6"/><path d="M110-128H125" stroke="${C.ink}" stroke-width="6"/>`;
const sun = (x, y) => `<circle cx="${x}" cy="${y}" r="70" fill="${C.duck}" opacity=".35"/><circle cx="${x}" cy="${y}" r="48" fill="${C.bill}" opacity=".5"/>`;
const cloud = (x, y, s = 1) => `<g transform="translate(${x} ${y}) scale(${s})" fill="${C.white}"><circle cx="0" cy="0" r="40"/><circle cx="45" cy="-15" r="50"/><circle cx="95" cy="0" r="38"/><rect x="0" y="0" width="95" height="38"/></g>`;
const bunting = (y) => {
  let s = `<path d="M0 ${y} Q800 ${y + 70} 1600 ${y}" stroke="${C.muted}" stroke-width="4" fill="none"/>`;
  const cols = [C.primary, C.duck, C.water, C.bill];
  for (let i = 0; i < 16; i++) {
    const x = 50 + i * 100, yy = y + 70 * (1 - Math.pow((x - 800) / 800, 2)) * 0.5 + 4;
    s += `<path d="M${x - 28} ${yy}L${x + 28} ${yy} ${x} ${yy + 50}Z" fill="${cols[i % 4]}"/>`;
  }
  return s;
};

function svg(bg, body) {
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1600 900" width="1600" height="900"><rect width="1600" height="900" fill="${bg}"/>${body}${front}</svg>\n`;
}

const scenes = {
  // Home: hero duck leading a row of ducklings across the pond.
  homepage: svg(C.sand, sun(1350, 200) + cloud(200, 200) + cloud(1000, 150, .8) + water(620)
    + duck(1050, 560, 1.4) + duck(700, 610, .8) + duck(450, 630, .7) + duck(230, 645, .6) + bubbles([[1300, 700, 10], [1340, 740, 6], [380, 760, 8]])),
  // About us: the team, four ducks side by side, one with a bow tie.
  "about-us": svg(C.cream, cloud(150, 180, .9) + cloud(1250, 160) + water(640)
    + duck(330, 600, 1) + duck(650, 590, 1.1, { extra: bowTie }) + duck(970, 600, 1, { extra: glasses }) + duck(1280, 615, .85)),
  // Contact us: a duck with a speech bubble and an envelope floating by.
  "contact-us": svg(C.sand, water(640)
    + duck(620, 590, 1.3)
    + `<path d="M820 230H1200A40 40 0 0 1 1240 270V430A40 40 0 0 1 1200 470H900L840 530 860 470H820A40 40 0 0 1 780 430V270A40 40 0 0 1 820 230Z" fill="${C.white}"/>`
    + `<circle cx="920" cy="350" r="22" fill="${C.bill}"/><circle cx="1010" cy="350" r="22" fill="${C.bill}"/><circle cx="1100" cy="350" r="22" fill="${C.bill}"/>`
    + `<g transform="translate(1180 600) rotate(-8)"><rect x="-90" y="-60" width="180" height="120" rx="10" fill="${C.white}"/><path d="M-90-60L0 10 90-60" stroke="${C.primary}" stroke-width="8" fill="none"/></g>`),
  // Events list: party bunting over a crowd of ducks.
  events: svg(C.cream, bunting(120) + water(650)
    + duck(260, 620, .8) + duck(560, 600, 1) + duck(860, 610, .9, { flip: true }) + duck(1150, 600, 1) + duck(1420, 625, .75, { flip: true })),
  // Services list: a duck with a toolbox and spanner.
  services: svg(C.sand, cloud(1200, 170) + water(650)
    + duck(560, 600, 1.3, { extra: hardHat })
    + `<g transform="translate(1050 560)"><rect x="-150" y="-80" width="300" height="160" rx="16" fill="${C.primary}"/><rect x="-60" y="-120" width="120" height="50" rx="12" fill="none" stroke="${C.primary}" stroke-width="16"/><rect x="-150" y="-20" width="300" height="14" fill="${C.primary.replace("B0", "8F")}"/></g>`
    + `<g transform="translate(1330 420) rotate(35)"><rect x="-14" y="-110" width="28" height="200" rx="14" fill="${C.muted}"/><circle cx="0" cy="-120" r="40" fill="${C.muted}"/><rect x="-14" y="-170" width="28" height="60" fill="${C.sand}"/></g>`),
  // Event: How to get the most out of your CMS: duck at a laptop.
  "how-to-get-the-most-out-of-your-cms": svg(C.cream, water(680)
    + duck(500, 600, 1.2)
    + `<g transform="translate(1040 560)"><rect x="-220" y="-210" width="440" height="280" rx="18" fill="${C.ink}"/><rect x="-195" y="-185" width="390" height="230" rx="8" fill="${C.water}"/>`
    + `<rect x="-165" y="-155" width="200" height="22" rx="11" fill="${C.white}"/><rect x="-165" y="-115" width="300" height="16" rx="8" fill="${C.white}" opacity=".8"/><rect x="-165" y="-85" width="260" height="16" rx="8" fill="${C.white}" opacity=".8"/>`
    + `<rect x="-165" y="-45" width="120" height="60" rx="8" fill="${C.duck}"/><rect x="-25" y="-45" width="120" height="60" rx="8" fill="${C.bill}"/>`
    + `<path d="M-260 70H260L230 110H-230Z" fill="${C.muted}"/></g>`),
  // Event (past): Creating great editor experiences: duck holding a giant pencil.
  "creating-great-editor-experiences": svg(C.sand, cloud(1250, 180, .9) + water(660)
    + `<g transform="translate(1050 420) rotate(-35)"><rect x="-30" y="-230" width="60" height="380" fill="${C.duck}"/><rect x="-30" y="-230" width="60" height="50" fill="${C.bill}"/><rect x="-30" y="-180" width="60" height="18" fill="${C.muted}"/><path d="M-30 150L0 230 30 150Z" fill="${C.sand}"/><path d="M-10 205L0 230 10 205Z" fill="${C.ink}"/></g>`
    + duck(620, 610, 1.25, { extra: glasses })),
  // Event: Migrating to Umbraco: ducks swimming from one pond to another along an arrow.
  "migrating-to-umbraco-from-other-cmss": svg(C.cream, water(660)
    + `<ellipse cx="260" cy="690" rx="230" ry="40" fill="${C.waterDark}"/><ellipse cx="1340" cy="690" rx="230" ry="40" fill="${C.waterDark}"/>`
    + `<path d="M380 380Q800 200 1220 380" stroke="${C.primary}" stroke-width="18" fill="none" stroke-dasharray="40 26" stroke-linecap="round"/><path d="M1180 330L1260 400 1160 420Z" fill="${C.primary}"/>`
    + duck(260, 620, .8) + duck(800, 600, .95) + duck(1340, 610, 1.1)),
  // Event: Umbraco Builders Day: ducks in hard hats stacking blocks.
  "umbraco-builders-day": svg(C.sand, water(690)
    + `<g fill="${C.primary}"><rect x="900" y="520" width="160" height="120" rx="10"/><rect x="1070" y="520" width="160" height="120" rx="10" fill="${C.bill}"/><rect x="985" y="390" width="160" height="120" rx="10" fill="${C.water}"/><rect x="1240" y="520" width="160" height="120" rx="10" fill="${C.muted}"/><rect x="1155" y="390" width="160" height="120" rx="10"/><rect x="1070" y="260" width="160" height="120" rx="10" fill="${C.duck}"/></g>`
    + duck(350, 620, 1, { extra: hardHat }) + duck(690, 630, .85, { extra: hardHat })),
  // Event: Upgrading - when is the best time?: duck beside a big clock.
  "upgrading-when-is-the-best-time": svg(C.cream, water(680)
    + `<g transform="translate(1080 400)"><circle r="210" fill="${C.white}" stroke="${C.ink}" stroke-width="18"/>`
    + Array.from({ length: 12 }, (_, i) => `<rect x="-6" y="-190" width="12" height="${i % 3 ? 22 : 40}" fill="${C.ink}" transform="rotate(${i * 30})"/>`).join("")
    + `<rect x="-9" y="-130" width="18" height="140" rx="9" fill="${C.ink}" transform="rotate(40)"/><rect x="-8" y="-170" width="16" height="180" rx="8" fill="${C.primary}" transform="rotate(-60)"/><circle r="18" fill="${C.ink}"/></g>`
    + duck(480, 610, 1.2)),
  // Service: AdWords audit: duck with a megaphone and a bar chart.
  "adwords-audit": svg(C.sand, cloud(250, 170, .8) + water(680)
    + duck(520, 610, 1.15)
    + `<g transform="translate(760 440) rotate(-15)"><path d="M0-30L170-100V100L0 30Z" fill="${C.primary}"/><rect x="-40" y="-35" width="50" height="70" rx="10" fill="${C.primary}"/></g>`
    + `<g fill="${C.water}"><rect x="1080" y="470" width="80" height="170" rx="8"/><rect x="1190" y="390" width="80" height="250" rx="8" fill="${C.bill}"/><rect x="1300" y="300" width="80" height="340" rx="8" fill="${C.duck}"/></g>`
    + `<path d="M1060 640H1420" stroke="${C.ink}" stroke-width="10" stroke-linecap="round"/>`),
  // Service: SEO audit: detective duck with a magnifying glass over a search bar.
  "seo-audit": svg(C.cream, water(680)
    + `<rect x="820" y="230" width="600" height="110" rx="55" fill="${C.white}" stroke="${C.muted}" stroke-width="8"/><rect x="890" y="270" width="320" height="28" rx="14" fill="${C.sand}"/>`
    + `<circle cx="1340" cy="285" r="26" fill="none" stroke="${C.primary}" stroke-width="9"/><path d="M1359 304L1380 325" stroke="${C.primary}" stroke-width="9" stroke-linecap="round"/>`
    + duck(560, 610, 1.25)
    + `<g transform="translate(900 520)"><circle r="95" fill="${C.water}" opacity=".35" stroke="${C.ink}" stroke-width="18"/><rect x="70" y="60" width="34" height="150" rx="17" fill="${C.ink}" transform="rotate(-45 70 60)"/></g>`),
  // Service: Umbraco upgrade: duck riding a rocket upwards.
  "umbraco-upgrade": svg(C.sand, cloud(250, 220) + cloud(1300, 650, .9) + water(740)
    + `<g transform="translate(900 470) rotate(-30)"><path d="M-260-60H160Q260-60 320 0 260 60 160 60H-260Z" fill="${C.white}" stroke="${C.muted}" stroke-width="8"/><circle cx="120" cy="0" r="30" fill="${C.water}" stroke="${C.muted}" stroke-width="8"/>`
    + `<path d="M-200-60L-280-140H-180L-120-60ZM-200 60L-280 140H-180L-120 60Z" fill="${C.primary}"/><path d="M-260-40L-380 0-260 40Z" fill="${C.bill}"/><path d="M-260-20L-330 0-260 20Z" fill="${C.duck}"/></g>`
    + duck(895, 375, .55)),
};

for (const [name, content] of Object.entries(scenes)) {
  writeFileSync(join(out, `${name}.svg`), content);
  console.log(`${name}.svg ${content.length} bytes`);
}
