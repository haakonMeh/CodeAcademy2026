package no.soprasteria.kaffebar;

import jakarta.persistence.EntityManagerFactory;
import no.soprasteria.kaffebar.api.BestillingResource;
import no.soprasteria.kaffebar.api.JsonKonfigurasjon;
import no.soprasteria.kaffebar.api.KampanjeResource;
import no.soprasteria.kaffebar.api.KvitteringResource;
import no.soprasteria.kaffebar.api.UventetFeilMapper;
import no.soprasteria.kaffebar.db.Database;
import no.soprasteria.kaffebar.tjeneste.KampanjeTjeneste;
import org.glassfish.grizzly.http.server.HttpServer;
import org.glassfish.hk2.utilities.binding.AbstractBinder;
import org.glassfish.jersey.grizzly2.httpserver.GrizzlyHttpServerFactory;
import org.glassfish.jersey.jackson.JacksonFeature;
import org.glassfish.jersey.server.ResourceConfig;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.slf4j.bridge.SLF4JBridgeHandler;

import java.net.URI;

/**
 * Starter Postgres (Testcontainers) og en Jersey-server på http://localhost:8080.
 * Kjør med: mvn compile exec:java
 */
public class KaffebarApp {

    private static final Logger LOG = LoggerFactory.getLogger(KaffebarApp.class);

    private static final URI BASE_URI = URI.create("http://localhost:8080/");

    static {
        // Jersey og Grizzly logger med java.util.logging. Send det videre til SLF4J/Logback.
        SLF4JBridgeHandler.removeHandlersForRootLogger();
        SLF4JBridgeHandler.install();
    }

    public static void main(String[] args) throws InterruptedException {
        var emf = Database.start();
        var server = startServer(BASE_URI, emf);
        Runtime.getRuntime().addShutdownHook(new Thread(() -> {
            server.shutdownNow();
            emf.close();
        }));

        LOG.info("Kaffebaren er åpen (Ctrl+C for å stenge):\n  {}kvitteringer\n  {}bestillinger/1\n  {}kampanje/1",
                BASE_URI, BASE_URI, BASE_URI);
        Thread.currentThread().join();
    }

    public static HttpServer startServer(URI baseUri, EntityManagerFactory emf) {
        var config = new ResourceConfig()
                .register(new AbstractBinder() {
                    @Override
                    protected void configure() {
                        bind(emf).to(EntityManagerFactory.class);
                        bind(new KampanjeTjeneste(emf)).to(KampanjeTjeneste.class);
                    }
                })
                // Uten Jacksons egne exception mappers, som ellers gjør feil ved JSON-skriving om til 400
                .register(JacksonFeature.withoutExceptionMappers())
                .register(JsonKonfigurasjon.class)
                .register(UventetFeilMapper.class)
                .register(KvitteringResource.class)
                .register(BestillingResource.class)
                .register(KampanjeResource.class);

        return GrizzlyHttpServerFactory.createHttpServer(baseUri, config);
    }
}
