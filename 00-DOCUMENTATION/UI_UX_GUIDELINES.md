# MAGIC — FRONTEND & UI/UX DESIGN GUIDELINES

**Document Version:** 1.1.0  
**Status:** Phase 0 Planning Specification (Pending Review)  
**Last Updated:** 2026-11-12  

---

## 1. Frontend Architectural Philosophy

The MAGIC user interface is built with **Angular (TypeScript)** using SCSS and CSS design tokens, running within Microsoft Edge **WebView2**.

### Key Tenets
1. **Zero Financial Calculation in Frontend:**
   The frontend is an **experience presentation layer**. It renders data, validates input formats (e.g., regex, required fields), and captures user interactions. All tax calculations, discounts, price tier evaluations, and ledger balance totals are calculated by the ASP.NET Core backend.
2. **Keyboard-First Design (High-Speed POS):**
   Cashiers and counter operators must be able to complete an entire sale, from item entry to multi-tender payment and print, without touching a mouse.
3. **High-Density Enterprise Layout:**
   Avoid excessive whitespace typical of mobile consumer apps. Business users require dense tabular views with visible column customization, sorting, and fast filtering.
4. **Resilient Local Connectivity:**
   Visual indicators must display connectivity status to the local backend service and PostgreSQL database.

---

## 2. Keyboard Navigation Standards (POS Accelerators)

| Key Combination | Scope | Action |
|---|---|---|
| `F1` | POS / Billing | Quick Item Lookup / Barcode Search Focus |
| `F2` | POS / Billing | Quantity Modification on active line |
| `F3` | POS / Billing | Price / Discount Override (triggers Supervisor modal if $>10\%$) |
| `F4` | POS / Billing | Park / Hold Active Cart |
| `F5` | POS / Billing | Recall Held Cart Dialog |
| `F8` | Global | Select Active Customer / Search Customer |
| `F9` | POS / Billing | Open Multi-Tender Payment Modal |
| `F10` | Global | Switch Active Branch / Terminal |
| `F12` | POS / Billing | Quick Cash Tender & Instant Print (Finalize) |
| `Esc` | Modal / Form | Close modal, cancel prompt, or step back |
| `Arrow Up / Down` | Grid / List | Move between cart line items or table rows |

---

## 3. UI Component Hierarchy, State Management & Design System

- **Angular Standalone Components:** Eliminate unnecessary `NgModule` boilerplate, enabling lazy-loaded feature modules (`features/*`) and fast initial load times inside WebView2.
- **Signals & Reactive Forms:** Leverage **Angular Signals** initially for high-frequency POS cart line-item reactivity and local UI state, with **NgRx** evaluated only if multi-branch state complexity justifies it later. Strict typed **Angular Reactive Forms** govern all commercial data entry.
- **Component Library Evaluation (Pending Decision):** Evaluate **Angular Material + CDK** (native accessibility, strong form/table primitives, zero third-party vendor risk) vs. **PrimeNG** (dense enterprise DataGrid) vs. **Custom SCSS + CSS Design Token Primitives** before locking a UI dependency.
- **State Separation:**
  - *UI State:* Active tab, modal visibility, table column ordering, search filter keywords (managed with Signals).
  - *Server State:* Centralized via Angular `HttpClient` services and RxJS pipelines with strict backend validation.

---

## 4. UI Feedback, Empty States & Accessibility

1. **Deterministic Loading States:**
   - Button spinners during in-flight network requests.
   - Non-blocking skeleton loaders for tabular reports.
2. **Double-Submission Prevention:**
   - Any button triggering an idempotent write (Checkout, Post Voucher, Save Invoice) is immediately disabled upon click until the API response resolves.
3. **High-Contrast Dark & Light Themes:**
   - Support crisp, contrast-optimized themes compliant with WCAG AA standards to reduce eye strain in 10-hour retail shift environments.


---

## Phase 1 Angular CDK & CSS Design Token Implementation (`01-SRC/Frontend/` — 2026-11-12)

- **Approved Foundation (`OPEN-002`):** `@angular/cdk ^22.2.7` (`BreakpointObserver`, `CdkTrapFocus`) paired with custom SCSS/CSS design tokens; zero Angular Material or PrimeNG packages.
- **Layered Stylesheet Architecture:** `src/styles.scss` orders `@layer reset, base, theme, components, utilities;` via `_layers.scss`, `_reset.scss`, and `_tokens.scss`.
- **Adaptive Light / Dark / Forced-Colors System:** Uses `<meta name="color-scheme" content="light dark">` + blocking FOUC-prevention inline script + `:root` `light-dark()` tokens with `@media (prefers-color-scheme: dark)` and `@media (forced-colors: active)` fallbacks, driven by `ThemeService` (`System` / `Light` / `Dark`).
- **Accessibility Foundation:** Single `<main id="main-content" tabindex="-1">`, skip link (`a.skip-link`), `<nav aria-label="Primary">` with `aria-current="page"`, 44px minimum target sizing (`2.75rem`), `3px` `:focus-visible` outlines, geometric + text status pills (`StatusBadgeComponent`), and ARIA live regions in `ConnectivityBannerComponent`.
- **Scope Boundary:** Phase 1 implements only diagnostic shell routes (`/system/status`, `/system/preferences`); business ERP screens begin in Phase 2+.
