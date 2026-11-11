package no.soprasteria.kaffebar.api;

import jakarta.inject.Inject;
import jakarta.persistence.EntityManagerFactory;
import jakarta.ws.rs.GET;
import jakarta.ws.rs.NotFoundException;
import jakarta.ws.rs.Path;
import jakarta.ws.rs.PathParam;
import jakarta.ws.rs.Produces;
import jakarta.ws.rs.core.MediaType;
import no.soprasteria.kaffebar.domene.Bestilling;

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
    public Bestilling hentBestilling(@PathParam("id") Long id) {
        try (var em = emf.createEntityManager()) {
            var bestilling = em.find(Bestilling.class, id);
            if (bestilling == null) {
                throw new NotFoundException("Fant ikke bestilling " + id);
            }
            return bestilling;
        }
    }
}
