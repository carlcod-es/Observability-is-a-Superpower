// Main navigation toggle (progressive enhancement). Without this script the button stays
// hidden and the navigation is always visible.
const toggle = document.querySelector(".site-nav__toggle");

if (toggle) {
  toggle.hidden = false;
  toggle.setAttribute("aria-expanded", "false");
  toggle.addEventListener("click", () => {
    const expanded = toggle.getAttribute("aria-expanded") === "true";
    toggle.setAttribute("aria-expanded", String(!expanded));
  });
}
