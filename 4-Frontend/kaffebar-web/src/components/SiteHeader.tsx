import Link from "next/link";

export function SiteHeader() {
  return (
    <header className="border-b border-line bg-card/70 backdrop-blur">
      <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-3 sm:px-6">
        <Link href="/" className="flex items-center gap-2.5">
          <span
            aria-hidden
            className="flex h-9 w-9 items-center justify-center rounded-xl bg-accent text-lg shadow-sm"
          >
            ☕
          </span>
          <span className="text-lg font-semibold tracking-tight">Kaffebar</span>
        </Link>

        <nav className="flex items-center gap-1 text-sm font-medium">
          <Link
            href="/"
            className="rounded-lg px-3 py-1.5 text-secondary transition-colors hover:bg-accent-soft hover:text-foreground"
          >
            Kundeskjerm
          </Link>
          <Link
            href="/barista"
            className="rounded-lg px-3 py-1.5 text-secondary transition-colors hover:bg-accent-soft hover:text-foreground"
          >
            Baristaskjerm
          </Link>
        </nav>
      </div>
    </header>
  );
}
