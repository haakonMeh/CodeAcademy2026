package no.soprasteria.kaffebar;

import jakarta.persistence.EntityManagerFactory;
import no.soprasteria.kaffebar.db.Database;
import no.soprasteria.kaffebar.domene.Bestilling;
import org.glassfish.grizzly.http.server.HttpServer;
import org.junit.jupiter.api.AfterAll;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.net.ServerSocket;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;

import static org.assertj.core.api.Assertions.assertThat;

class KollisjonApiTest {

    private static final Logger LOG = LoggerFactory.getLogger(KollisjonApiTest.class);

    private static EntityManagerFactory emf;
    private static HttpServer server;
    private static URI baseUri;
    private static final HttpClient HTTP = HttpClient.newHttpClient();

    @BeforeAll
    static void startServer() throws IOException {
        emf = Database.start();
        try (var socket = new ServerSocket(0)) {
            baseUri = URI.create("http://localhost:" + socket.getLocalPort() + "/");
        }
        server = KaffebarApp.startServer(baseUri, emf);
    }

    @AfterAll
    static void stoppServer() {
        server.shutdownNow();
    }

    @Test
    void andreBaristaSomBrukerSammeVersjonFaarKonflikt() throws Exception {
        var id = Testbestillinger.id(emf, Testbestillinger.KOLLISJON_API);

        // Begge baristaene har sett versjon 0 av bestillingen
        var barista1 = put(id, "\"0\"");
        LOG.info("Barista 1: {} {}", barista1.statusCode(), barista1.body());
        assertThat(barista1.statusCode()).as("Første barista").isEqualTo(204);

        var barista2 = put(id, "\"0\"");
        LOG.info("Barista 2: {} {}", barista2.statusCode(), barista2.body());
        assertThat(barista2.statusCode())
                .as("Andre barista skal få beskjed om at bestillingen er endret (ikke 500)")
                .isIn(409, 412);
    }

    @Test
    void utenIfMatchBlirBestillingenMarkertSomHentet() throws Exception {
        var id = Testbestillinger.id(emf, Testbestillinger.KOLLISJON_API_UTEN_VERSJON);

        assertThat(put(id, null).statusCode()).isEqualTo(204);

        try (var em = emf.createEntityManager()) {
            assertThat(em.find(Bestilling.class, id).getStatus()).isEqualTo(Bestilling.Status.HENTET);
        }
    }

    private static HttpResponse<String> put(Long id, String ifMatch) throws Exception {
        var request = HttpRequest.newBuilder(baseUri.resolve("bestillinger/" + id + "/hentet"))
                .PUT(HttpRequest.BodyPublishers.noBody());
        if (ifMatch != null) {
            request.header("If-Match", ifMatch);
        }
        return HTTP.send(request.build(), HttpResponse.BodyHandlers.ofString());
    }
}
