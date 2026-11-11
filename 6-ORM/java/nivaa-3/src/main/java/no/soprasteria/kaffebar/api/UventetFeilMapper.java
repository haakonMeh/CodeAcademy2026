package no.soprasteria.kaffebar.api;

import jakarta.ws.rs.WebApplicationException;
import jakarta.ws.rs.core.MediaType;
import jakarta.ws.rs.core.Response;
import jakarta.ws.rs.ext.ExceptionMapper;
import jakarta.ws.rs.ext.Provider;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Map;

/**
 * Logger uventede feil med full stack trace og svarer med 500, slik at du ser hva som gikk galt.
 */
@Provider
public class UventetFeilMapper implements ExceptionMapper<Exception> {

    private static final Logger LOG = LoggerFactory.getLogger(UventetFeilMapper.class);

    @Override
    public Response toResponse(Exception exception) {
        if (exception instanceof WebApplicationException webException) {
            return webException.getResponse();
        }

        LOG.error("Uventet feil i REST-kallet", exception);
        return Response.serverError()
                .type(MediaType.APPLICATION_JSON)
                .entity(Map.of("feil", String.valueOf(exception.getMessage())))
                .build();
    }
}
