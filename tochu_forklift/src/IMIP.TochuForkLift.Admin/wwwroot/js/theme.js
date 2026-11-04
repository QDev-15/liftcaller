// Theme persistence for the Admin app — dark is the default (matches the approved prototype),
// light is opt-in and remembered per browser via localStorage.
window.adminTheme = {
    STORAGE_KEY: "admin.theme",
    get: function () {
        try { return localStorage.getItem(this.STORAGE_KEY) === "light" ? "light" : "dark"; }
        catch (e) { return "dark"; }
    },
    set: function (theme) {
        document.documentElement.setAttribute("data-theme", theme);
        try { localStorage.setItem(this.STORAGE_KEY, theme); } catch (e) { /* ignore */ }
    },
    toggle: function () {
        var next = this.get() === "light" ? "dark" : "light";
        this.set(next);
        return next;
    }
};
