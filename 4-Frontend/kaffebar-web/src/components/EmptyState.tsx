export function EmptyState({
  icon = "☕",
  title,
  description,
}: {
  icon?: string;
  title: string;
  description: string;
}) {
  return (
    <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-line bg-card/50 px-6 py-12 text-center">
      <span aria-hidden className="mb-3 text-3xl opacity-70">
        {icon}
      </span>
      <p className="font-semibold text-foreground">{title}</p>
      <p className="mt-1 max-w-xs text-sm text-muted">{description}</p>
    </div>
  );
}
