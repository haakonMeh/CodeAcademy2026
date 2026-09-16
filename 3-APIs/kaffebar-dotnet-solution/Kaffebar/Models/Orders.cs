using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Kaffebar.Models;

/// <summary>
/// Det kunden sender inn for å bestille. Oppgave 1, 3 og 4.
/// </summary>
/// <remarks>
/// Merk at dette IKKE er <see cref="Order"/>. Request og response er to ulike ting:
/// klienten sender <c>CoffeeId</c>, serveren svarer med <c>OrderId</c>,
/// <c>CoffeeName</c>, <c>Status</c> og tidsstempler — felter klienten verken kan eller
/// skal sette. Gjenbrukte vi <see cref="Order"/> begge veier, måtte alle de feltene
/// vært valgfrie, og kontrakten ville sluttet å si noe presist.
///
/// Valideringen er deklarativ (oppgave 4). Attributtene er både reglene som håndheves
/// OG dokumentasjonen som havner i /openapi/v1.json.
///
/// VIKTIG DETALJ: dette er en record med <c>init</c>-egenskaper, ikke en POSISJONELL
/// record. Det er ikke tilfeldig, og det er verdt en slide (se FASIT.md, oppgave 4):
///
///   - Skriver du en posisjonell record og setter <c>[property: Range(1, 10)]</c> på
///     et parameter, kaster MVC en InvalidOperationException ved første request —
///     «validation metadata must be associated with the constructor parameter».
///     Altså 500, ikke 400.
///   - Flytter du attributtene til parameteret (<c>[Range(1, 10)] int Quantity</c>),
///     virker valideringen — men da forsvinner <c>minLength</c>, <c>maxLength</c> og
///     <c>pattern</c> fra den genererte spesifikasjonen. Skjemagenereringen leser
///     egenskaper, ikke konstruktør-parametere.
///
/// Med <c>init</c>-egenskaper får du begge deler: reglene håndheves OG de står i
/// kontrakten. Det er hele poenget med oppgave 4.
///
/// Nullable reference types styrer hva som blir <c>required</c> i spesifikasjonen:
/// <c>CoffeeSize Size</c> er ikke-nullable og blir påkrevd, <c>MilkType? MilkType</c>
/// er nullable og blir valgfri. Oppgave 3 ber deg eksperimentere med nettopp det.
/// </remarks>
/// <param name="CoffeeId">Id-en til en kaffe fra <c>GET /menu</c>.</param>
public record CreateOrderRequest
{
    /// <summary>Id-en til en kaffe fra <c>GET /menu</c>.</summary>
    [Required]
    public Guid CoffeeId { get; init; }

    /// <summary>Navnet bestillingen ropes opp på. Kan ikke være blankt.</summary>
    /// <remarks>
    /// Mønsteret <c>^(?!\s*$).+$</c> og ikke bare <c>\S</c>: kontrakten deles med
    /// Java-sporet, og der håndheves <c>pattern</c> av Jakarta Bean Validation, som
    /// krever at HELE strengen matcher. Dette mønsteret betyr det samme begge steder.
    /// </remarks>
    [Required(ErrorMessage = "customerName er påkrevd.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "customerName må være mellom 2 og 50 tegn.")]
    [RegularExpression(@"^(?!\s*$).+$", ErrorMessage = "customerName kan ikke være blankt.")]
    public required string CustomerName { get; init; }

    /// <summary>Størrelsen. Påkrevd.</summary>
    [Required(ErrorMessage = "size er påkrevd.")]
    public required CoffeeSize Size { get; init; }

    /// <summary>Melketype. Valgfri — en espresso trenger ingen.</summary>
    public MilkType? MilkType { get; init; }

    /// <summary>Ekstra shot espresso. Valgfri.</summary>
    public bool? ExtraShot { get; init; }

    /// <summary>
    /// Antall kopper i samme bestilling. Default-verdien ligger i
    /// egenskapsinitialisereren, og System.Text.Json lar den stå når feltet mangler i
    /// JSON-en. <c>[DefaultValue]</c> gjør at den også havner i spesifikasjonen.
    /// </summary>
    [Range(1, 10, ErrorMessage = "quantity må være mellom 1 og 10.")]
    [DefaultValue(1)]
    public int Quantity { get; init; } = 1;
}

/// <summary>
/// Oppgave 7: et dedikert request-objekt som kun inneholder det som skal endres.
/// </summary>
/// <remarks>
/// Hvorfor ikke gjenbruke <see cref="Order"/>? Fordi da kunne en klient sendt med
/// <c>CreatedAt</c> eller <c>CustomerName</c>, og vi måtte skrevet kode som ignorerer
/// dem. Den koden kan glemmes. Med et objekt som bare har <c>Status</c>, finnes ikke
/// feltene å sende — typen gjør feilen umulig i stedet for bare forbudt.
/// </remarks>
public record UpdateOrderStatusRequest([Required] OrderStatus Status);

/// <summary>En bestilling slik serveren svarer med den.</summary>
/// <param name="CoffeeName">
/// Denormalisert fra menyen, slik at en klient kan vise ordren uten et ekstra kall.
/// </param>
/// <remarks>
/// De to valgfrie feltene står SIST og har default-verdier. Det er ikke kosmetikk:
/// .NET markerer konstruktør-parametere uten default-verdi som <c>required</c> i den
/// genererte spesifikasjonen. Sto <c>MilkType</c> midt i lista, ville kontrakten lovet
/// at feltet alltid er med — mens serveren utelater det når det er null. Rekkefølgen på
/// nøklene i JSON spiller ingen rolle; <c>required</c>-lista gjør det.
/// </remarks>
public record Order(
    Guid OrderId,
    Guid CoffeeId,
    string CoffeeName,
    string CustomerName,
    CoffeeSize Size,
    int Quantity,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    MilkType? MilkType = null,
    bool? ExtraShot = null);
