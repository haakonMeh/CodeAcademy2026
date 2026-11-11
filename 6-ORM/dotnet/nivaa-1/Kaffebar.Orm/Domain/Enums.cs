namespace Kaffebar.Orm.Domain;

/// <summary>
/// Livssyklusen til en bestilling. Samme flyt som i samling 3, pluss <see cref="PickedUp"/>:
/// kunden har hentet kaffen, og ordren er historie.
/// </summary>
public enum OrderStatus
{
    Pending,
    Brewing,
    Ready,
    PickedUp
}

public enum CoffeeSize
{
    Small,
    Medium,
    Large
}

public enum MilkType
{
    Whole,
    Skimmed,
    Oat,
    Soy
}
