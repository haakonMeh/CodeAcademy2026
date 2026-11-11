package no.soprasteria.kaffebar.api;

import jakarta.inject.Inject;
import jakarta.persistence.EntityManagerFactory;
import jakarta.ws.rs.GET;
import jakarta.ws.rs.HeaderParam;
import jakarta.ws.rs.NotFoundException;
import jakarta.ws.rs.PUT;
import jakarta.ws.rs.Path;
import jakarta.ws.rs.PathParam;
import jakarta.ws.rs.Produces;
import jakarta.ws.rs.core.HttpHeaders;
import jakarta.ws.rs.core.MediaType;
import jakarta.ws.rs.core.Response;
import no.soprasteria.kaffebar.repository.KaffebarRepository;
import no.soprasteria.kaffebar.tjeneste.BestillingTjeneste;

@Path("/bestillinger")
@Produces(MediaType.APPLICATION_JSON)
public class BestillingResource {

    private final EntityManagerFactory emf;

    @Inject
    public BestillingResource(EntityManagerFactory emf) {
        this.emf = emf;
    }

    /**
     * Henter én bestilling. Kassa-appen forventer dette formatet:
     *
     * <pre>
     * {
     *   "id": 1,
     *   "kundenavn": "Kari Nordmann",
     *   "tidspunkt": "2026-07-14T09:31:00",
     *   "status": "MOTTATT",
     *   "totalpris": 175.00,
     *   "linjer": [
     *     { "kaffebonne": "Gatomboya", "brenneri": "Tim Wendelboe", "antall": 2, "enhetspris": 55.00 }
     *   ],
     *   "tilpasninger": [ "Havremelk", "Ekstra shot" ]
     * }
     * </pre>
     */
    @GET
    @Path("/{id}")
    public BestillingDto hentBestilling(@PathParam("id") Long id) {
        // Entiteten kan ikke returneres direkte: Jackson serialiserer *etter* at metoden har returnert
        // og EntityManager er lukket, så lazy-samlingene gir LazyInitializationException. Og med alt
        // JOIN FETCH-et får vi i stedet uendelig rekursjon (Bestilling -> linje -> bestilling -> …).
        // Vi mapper derfor til en DTO mens EntityManager fortsatt er åpen.
        try (var em = emf.createEntityManager()) {
            return new KaffebarRepository(em).hentBestilling(id)
                    .map(BestillingDto::fra)
                    .orElseThrow(() -> new NotFoundException("Fant ikke bestilling " + id));
        }
    }

    /**
     * Baristaen markerer bestillingen som hentet.
     *
     * @param ifMatch valgfri versjon klienten så sist, f.eks. {@code "0"}. Er den med, avvises
     *                kallet hvis bestillingen er endret av noen andre i mellomtiden.
     */
    @PUT
    @Path("/{id}/hentet")
    public Response markerSomHentet(@PathParam("id") Long id, @HeaderParam(HttpHeaders.IF_MATCH) String ifMatch) {
        Long forventetVersjon = ifMatch == null ? null : Long.valueOf(ifMatch.replace("\"", "").strip());
        new BestillingTjeneste(emf).markerSomHentet(id, forventetVersjon);
        return Response.noContent().build();
    }
}
