package no.soprasteria.kaffebar.api;

import jakarta.inject.Inject;
import jakarta.ws.rs.GET;
import jakarta.ws.rs.Path;
import jakarta.ws.rs.PathParam;
import jakarta.ws.rs.Produces;
import jakarta.ws.rs.core.MediaType;
import no.soprasteria.kaffebar.tjeneste.KampanjeKvittering;
import no.soprasteria.kaffebar.tjeneste.KampanjeTjeneste;

@Path("/kampanje")
@Produces(MediaType.APPLICATION_JSON)
public class KampanjeResource {

    private final KampanjeTjeneste kampanjeTjeneste;

    @Inject
    public KampanjeResource(KampanjeTjeneste kampanjeTjeneste) {
        this.kampanjeTjeneste = kampanjeTjeneste;
    }

    @GET
    @Path("/{bestillingId}")
    public KampanjeKvittering hentKampanjeKvittering(@PathParam("bestillingId") Long bestillingId) {
        return kampanjeTjeneste.genererKampanjeKvittering(bestillingId);
    }
}
