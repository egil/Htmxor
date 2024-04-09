// @ts-nocheck

function overflowMenu(subtree = document) {
  subtree.querySelectorAll("[data-overflow-menu]").forEach(menuRoot => {
    const
    button = menuRoot.querySelector("[aria-haspopup]"),
    menu = menuRoot.querySelector("[role=menu]"),
    items = [...menu.querySelectorAll("[role=menuitem]")];

    const isOpen = () => !menu.hidden;

    items.forEach(item => item.setAttribute("tabindex", "-1"));

    function toggleMenu(open = !isOpen()) {
      if (open) {
        menu.hidden = false;
        button.setAttribute("aria-expanded", "true");
        items[0].focus();
      } else {
        menu.hidden = true;
        button.setAttribute("aria-expanded", "false");
      }
    }

    toggleMenu(isOpen());
    button.addEventListener("click", () => toggleMenu());
    menuRoot.addEventListener("blur", e => console.log(e) || toggleMenu(false));

    window.addEventListener("click", function clickAway(event) {
      if (!menuRoot.isConnected) window.removeEventListener("click", clickAway);
      if (!menuRoot.contains(event.target)) toggleMenu(false);
    })

    const currentIndex = () => {
      const idx = items.indexOf(document.activeElement);
      if (idx === -1) return 0;
      return idx;
    }

    menuRoot.addEventListener("keydown", e => {
      if (e.key === "ArrowUp") {
        items[currentIndex() - 1]?.focus();

      } else if (e.key === "ArrowDown") {
        items[currentIndex() + 1]?.focus();

      } else if (e.key === "Space") {
        items[currentIndex()].click();

      } else if (e.key === "Home") {
        items[0].focus();

      } else if (e.key === "End") {
        items[items.length - 1].focus();

      } else if (e.key === "Escape") {
        toggleMenu(false);
        button.focus();

      } else if (e.key === "Tab") {
        toggleMenu(false);
      }
    })
  })
}

addEventListener("htmx:load", e => overflowMenu(e.target));

