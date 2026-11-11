package no.soprasteria.kaffebar;

import jakarta.persistence.EntityManagerFactory;
import no.soprasteria.kaffebar.api.KvitteringResource;
import no.soprasteria.kaffebar.db.Database;
import org.glassfish.hk2.utilities.binding.AbstractBinder;
import org.glassfish.jersey.grizzly2.httpserver.GrizzlyHttpServerFactory;
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

        var config = new ResourceConfig()
                .register(new AbstractBinder() {
                    @Override
                    protected void configure() {
                        bind(emf).to(EntityManagerFactory.class);
                    }
                })
                .register(KvitteringResource.class);

        var server = GrizzlyHttpServerFactory.createHttpServer(BASE_URI, config);
        Runtime.getRuntime().addShutdownHook(new Thread(() -> {
            server.shutdownNow();
            emf.close();
        }));

        LOG.info("Kaffebaren er åpen: {}kvitteringer (Ctrl+C for å stenge)", BASE_URI);
        Thread.currentThread().join();
    }
}
