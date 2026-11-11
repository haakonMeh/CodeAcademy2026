package no.soprasteria.kaffebar;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
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

class BestillingApiTest {

    private static final Logger LOG = LoggerFactory.getLogger(BestillingApiTest.class);

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
    void henterBestillingSomJson() throws Exception {
        var forventet = enBestillingMedTilpasninger();

        var respons = get("bestillinger/" + forventet.id());
        LOG.info("Svar fra API-et: {}", respons.body());

        assertThat(respons.statusCode()).as("HTTP-status").isEqualTo(200);

        JsonNode json = new ObjectMapper().readTree(respons.body());
        assertThat(json.get("id").asLong()).isEqualTo(forventet.id());
        assertThat(json.get("kundenavn").asText()).isEqualTo(forventet.kundenavn());
        assertThat(json.get("status").asText()).isEqualTo(forventet.status());
        assertThat(json.get("tidspunkt").asText()).matches("\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}.*");

        var linjer = json.get("linjer");
        assertThat(linjer.size()).isEqualTo(forventet.antallLinjer());
        assertThat(linjer.get(0).get("kaffebonne").asText()).isNotBlank();
        assertThat(linjer.get(0).get("brenneri").asText()).isNotBlank();
        assertThat(linjer.get(0).get("antall").asInt()).isPositive();

        var tilpasninger = json.get("tilpasninger");
        assertThat(tilpasninger.size()).isEqualTo(forventet.antallTilpasninger());
        assertThat(tilpasninger.get(0).isTextual()).as("Tilpasninger er en liste med tekster").isTrue();
    }

    @Test
    void ukjentBestillingGir404() throws Exception {
        assertThat(get("bestillinger/999999").statusCode()).isEqualTo(404);
    }

    private static HttpResponse<String> get(String sti) throws Exception {
        var request = HttpRequest.newBuilder(baseUri.resolve(sti)).GET().build();
        return HTTP.send(request, HttpResponse.BodyHandlers.ofString());
    }

    private record Forventet(Long id, String kundenavn, String status, int antallLinjer, int antallTilpasninger) {
    }

    private static Forventet enBestillingMedTilpasninger() {
        try (var em = emf.createEntityManager()) {
            var bestilling = em.createQuery(
                            "SELECT b FROM Bestilling b WHERE size(b.tilpasninger) > 0 ORDER BY b.id DESC", Bestilling.class)
                    .setMaxResults(1)
                    .getSingleResult();
            return new Forventet(bestilling.getId(), bestilling.getKundenavn(), bestilling.getStatus().name(),
                    bestilling.getLinjer().size(), bestilling.getTilpasninger().size());
        }
    }
}
