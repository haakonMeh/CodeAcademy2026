namespace Kaffebar.Events;

/// <summary>
/// VALGFRITT SISTE STEG — ikke en del av oppgavesettet i samling 3.
/// AV som standard. Se FASIT.md.
/// </summary>
public sealed class EventOptions
{
    public const string SectionName = "Kaffebar:Events";

    public bool Enabled { get; set; }
    public string Exchange { get; set; } = "kaffebar";
    public string Uri { get; set; } = "amqp://guest:guest@localhost:5672";
}

/// <summary>Auto-baristaen. Også valgfri, også av som standard.</summary>
public sealed class BaristaOptions
{
    public const string SectionName = "Kaffebar:Barista";

    public bool AutoEnabled { get; set; }
    public int IntervalMs { get; set; } = 8000;
}
