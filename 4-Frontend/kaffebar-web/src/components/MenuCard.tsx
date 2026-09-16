import { formatPrice } from "@/lib/format";
import type { Coffee } from "@/types/domain";

export function MenuCard({
  coffee,
  selected = false,
  onSelect,
}: {
  coffee: Coffee;
  selected?: boolean;
  onSelect?: (coffee: Coffee) => void;
}) {
  const className = `flex w-full items-center justify-between gap-3 rounded-xl border px-4 py-3 text-left transition-all ${
    selected
      ? "border-accent bg-accent-soft shadow-sm"
      : "border-line bg-card hover:border-accent/50 hover:shadow-sm"
  }`;

  const content = (
    <>
      <span className="font-medium">{coffee.name}</span>
      <span className="shrink-0 font-mono text-sm text-secondary">
        {formatPrice(coffee.price)}
      </span>
    </>
  );

  if (!onSelect) {
    return <div className={className}>{content}</div>;
  }

  return (
    <button
      type="button"
      onClick={() => onSelect(coffee)}
      aria-pressed={selected}
      className={className}
    >
      {content}
    </button>
  );
}
