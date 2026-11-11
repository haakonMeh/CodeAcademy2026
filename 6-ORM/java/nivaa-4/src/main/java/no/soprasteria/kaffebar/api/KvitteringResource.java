package no.soprasteria.kaffebar.api;

import jakarta.inject.Inject;
import jakarta.persistence.EntityManagerFactory;
import jakarta.ws.rs.GET;
import jakarta.ws.rs.Path;
import jakarta.ws.rs.Produces;
import jakarta.ws.rs.core.MediaType;
import no.soprasteria.kaffebar.db.QueryCountLogger;
import no.soprasteria.kaffebar.kvittering.Kvitteringsskriver;
import no.soprasteria.kaffebar.repository.KaffebarRepository;

@Path("/kvitteringer")
public class KvitteringResource {

    private final EntityManagerFactory emf;

    @Inject
    public KvitteringResource(EntityManagerFactory emf) {
        this.emf = emf;
    }

    @GET
    @Produces(MediaType.TEXT_PLAIN)
    public String hentKvitteringer() {
        QueryCountLogger.reset();
        try (var em = emf.createEntityManager()) {
            var bestillinger = new KaffebarRepository(em).hentAlleBestillinger();
            var kvittering = new Kvitteringsskriver().lagKvittering(bestillinger);
            return kvittering + "\n" + QueryCountLogger.oppsummering() + "\n";
        }
    }
}
