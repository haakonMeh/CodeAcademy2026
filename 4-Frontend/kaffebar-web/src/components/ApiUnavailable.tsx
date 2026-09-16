/**
 * Vises når server-komponenten ikke får kontakt med Kaffebar-API-et.
 *
 * Utlevert. Poenget er at en container som ikke er oppe skal gi en beskjed du
 * kan handle på, ikke en stack trace — og at du skal kunne jobbe videre uten
 * containere i det hele tatt.
 */
export function ApiUnavailable({ url, message }: { url: string; message: string }) {
  return (
    <div className="rounded-2xl border border-dashed border-line bg-card/50 px-6 py-10 text-center">
      <span aria-hidden className="mb-3 block text-3xl opacity-70">
        🔌
      </span>
      <p className="font-semibold text-foreground">Får ikke kontakt med Kaffebar-API-et</p>
      <p className="mx-auto mt-1 max-w-md text-sm text-muted">
        Frontenden prøvde <code className="font-mono">{url}</code> og fikk:{" "}
        <span className="font-mono">{message}</span>
      </p>
      <div className="mx-auto mt-5 max-w-md space-y-2 text-left text-sm text-secondary">
        <p className="font-semibold text-foreground">To veier videre:</p>
        <p>
          <span className="font-semibold">Med containere</span> — fra rota av repoet:
          <br />
          <code className="font-mono text-xs">
            docker compose --profile java up --build kaffebar-java rabbitmq
          </code>
        </p>
        <p>
          <span className="font-semibold">Uten containere</span> — stopp serveren og start den
          med mock-modus:
          <br />
          <code className="font-mono text-xs">USE_MOCK_API=true yarn dev</code>
        </p>
      </div>
    </div>
  );
}
