using System.ComponentModel.DataAnnotations;
using Kaffebar.Auth;
using Kaffebar.Errors;
using Kaffebar.Events;
using Kaffebar.Models;
using Kaffebar.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaffebar.Controllers;

/// <summary>
/// Bestillinger. Oppgave 1, 2, 5, 7 og 8.
/// </summary>
/// <remarks>
/// MERK — og ikke «rydd» dette bort:
///
/// <c>GET /menu</c> ligger igjen som et Minimal API i Program.cs, mens ordrene bor her
/// i en controller. Det ser inkonsekvent ut, og det er helt med vilje: oppgave 2 ber
/// eksplisitt om nettopp den blandingen, slik at du kan se de to stilene ved siden av
/// hverandre i samme prosjekt og selv vurdere hvor valideringen og
/// OpenAPI-metadataene blir tydeligst. Se FASIT.md, oppgave 2.
///
/// <c>[ApiController]</c> gjør tre ting gratis her:
///   - modellvalidering kjøres automatisk, og returnerer 400 før koden din kjører
///   - parametere bindes fra body/route/query uten <c>[FromBody]</c> og venner
///   - feilsvar blir ProblemDetails
/// </remarks>
[ApiController]
[Route("orders")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
public class OrdersController(IOrderRepository repository, OrderEventPublisher events) : ControllerBase
{
    /// <summary>Hent bestillinger.</summary>
    /// <remarks>
    /// Oppgave 8. Default-verdiene står på parameterne, og havner dermed også i
    /// /openapi/v1.json.
    ///
    /// I et Minimal API ville dette vært <c>[AsParameters] OrderQuery query</c> med en
    /// <c>record OrderQuery(OrderStatus? Status, int Limit = 20, int Offset = 0)</c>.
    /// I en controller er vanlige parametere med default-verdier det som binder
    /// forutsigbart — en record bundet med <c>[FromQuery]</c> får ikke med seg
    /// konstruktørens default-verdier.
    /// </remarks>
    [HttpGet]
    [EndpointSummary("Hent bestillinger")]
    [EndpointDescription("Returnerer bestillinger, nyeste først. Kan filtreres på status og pagineres.")]
    [ProducesResponseType<IEnumerable<Order>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Order>> ListOrders(
        [FromQuery] OrderStatus? status = null,
        [FromQuery][Range(1, 100)] int limit = 20,
        [FromQuery][Range(0, int.MaxValue)] int offset = 0)
    {
        return Ok(repository.List(status, limit, offset));
    }

    /// <summary>Opprett bestilling.</summary>
    /// <remarks>Oppgave 1. 201 med Location-header — det er det «Created» betyr.</remarks>
    [HttpPost]
    [EndpointSummary("Opprett bestilling")]
    [ProducesResponseType<Order>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public ActionResult<Order> CreateOrder(CreateOrderRequest request)
    {
        var coffee = repository.FindCoffee(request.CoffeeId);
        if (coffee is null)
        {
            // 404 og ikke 400: kontrakten er overholdt, ressursen finnes bare ikke.
            return ProblemFactory
                .NotFound(HttpContext, $"Fant ingen kaffe med id {request.CoffeeId}. Se GET /menu.")
                .AsResult();
        }

        var order = repository.Create(request, coffee);
        events.OrderCreated(order);

        return Created($"/orders/{order.OrderId}", order);
    }

    /// <summary>Hent én bestilling.</summary>
    /// <remarks>
    /// Oppgave 5 og 6. Route-constrainten <c>:guid</c> er ikke bare dokumentasjon — den
    /// avgjør om ruten matcher i det hele tatt. Se FASIT.md, oppgave 5, om hvorfor
    /// <c>/orders/ikke-en-guid</c> gir 404 her og 400 i Java-sporet.
    /// </remarks>
    [HttpGet("{orderId:guid}")]
    [EndpointSummary("Hent én bestilling")]
    [ProducesResponseType<Order>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public ActionResult<Order> GetOrder(Guid orderId)
    {
        var order = repository.Find(orderId);
        return order is null ? NotFoundProblem(orderId) : Ok(order);
    }

    /// <summary>Oppdater status på en bestilling.</summary>
    /// <remarks>
    /// Oppgave 7, den anbefalte varianten. PATCH fordi vi oppdaterer én egenskap på en
    /// ressurs som finnes fra før, ikke erstatter hele den.
    /// </remarks>
    [HttpPatch("{orderId:guid}")]
    [Authorize(Policy = AuthPolicies.Barista)]
    [EndpointSummary("Oppdater status på en bestilling")]
    [ProducesResponseType<Order>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public ActionResult<Order> UpdateOrderStatus(Guid orderId, UpdateOrderStatusRequest request) =>
        ApplyStatus(orderId, request.Status);

    /// <summary>Oppdater status på en bestilling (action-variant).</summary>
    /// <remarks>
    /// Oppgave 7, variant 2. Alias for PATCH. Finnes fordi deltakerne valgte ulikt i
    /// mai, slik at en frontend kan peke på deltakerens eget API uten endringer.
    /// PATCH er anbefalingen — se FASIT.md.
    /// </remarks>
    [HttpPost("{orderId:guid}/status")]
    [Authorize(Policy = AuthPolicies.Barista)]
    [EndpointSummary("Oppdater status på en bestilling (action-variant)")]
    [ProducesResponseType<Order>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public ActionResult<Order> SetOrderStatus(Guid orderId, UpdateOrderStatusRequest request) =>
        ApplyStatus(orderId, request.Status);

    /// <summary>Én implementasjon, to endepunkter.</summary>
    private ActionResult<Order> ApplyStatus(Guid orderId, OrderStatus status)
    {
        var change = repository.SetStatus(orderId, status);
        if (change is null)
        {
            return NotFoundProblem(orderId);
        }

        // Publiser bare når noe faktisk endret seg. Ellers ville en frontend som setter
        // samme status to ganger lage støy på exchangen.
        if (change.Value.PreviousStatus != change.Value.Order.Status)
        {
            events.OrderStatusChanged(change.Value.Order, change.Value.PreviousStatus);
        }

        return Ok(change.Value.Order);
    }

    private ActionResult<Order> NotFoundProblem(Guid orderId) =>
        ProblemFactory
            .NotFound(HttpContext, $"Fant ingen bestilling med id {orderId}.")
            .AsResult();
}
