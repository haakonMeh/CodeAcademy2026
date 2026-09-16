package no.soprasteria.kaffebar;

import no.soprasteria.kaffebar.store.OrderStore;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Nested;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.webmvc.test.autoconfigure.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;

import static org.hamcrest.Matchers.containsInAnyOrder;
import static org.hamcrest.Matchers.containsString;
import static org.hamcrest.Matchers.hasSize;
import static org.hamcrest.Matchers.is;
import static org.hamcrest.Matchers.matchesPattern;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.patch;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.header;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

/**
 * Testene som holder kontrakten ærlig.
 *
 * De er skrevet mot HTTP-laget og ikke mot kontrollerklassene, fordi det er HTTP-laget
 * som er kontrakten. En test som kaller {@code ordersController.createOrder(...)}
 * direkte ville ikke fanget noe av det som faktisk kan gå galt her: statuskoder,
 * Location-headeren, media typen på feilsvar, eller at valideringen fra YAML-en
 * virkelig slår inn.
 */
@SpringBootTest
@AutoConfigureMockMvc
class KaffebarApiTest {

    private static final String LATTE = "c0ffee01-0000-4000-8000-000000000007";
    private static final String PROBLEM_JSON = "application/problem+json";

    /** Fra seed-dataene: Jonas sin espresso, status PENDING. */
    private static final String SEEDED_PENDING = "0de50001-0000-4000-8000-000000000003";

    @Autowired
    private MockMvc mockMvc;

    @Autowired
    private OrderStore store;

    @BeforeEach
    void resetStore() {
        // Lagringen er i minne og delt mellom testene. Uten denne ville rekkefølgen
        // på testene bestemt om de går grønt.
        store.reset();
    }

    @Nested
    @DisplayName("GET /menu — oppgave 2")
    class Menu {

        @Test
        @DisplayName("gir de ti faste varene med faste uuid-er")
        void returnsTenFixedItems() throws Exception {
            mockMvc.perform(get("/menu"))
                    .andExpect(status().isOk())
                    .andExpect(content().contentTypeCompatibleWith(MediaType.APPLICATION_JSON))
                    .andExpect(jsonPath("$", hasSize(10)))
                    .andExpect(jsonPath("$[0].id", is("c0ffee01-0000-4000-8000-000000000001")))
                    .andExpect(jsonPath("$[0].name", is("Filterkaffe")))
                    .andExpect(jsonPath("$[0].price", is(39)))
                    .andExpect(jsonPath("$[9].id", is("c0ffee01-0000-4000-8000-00000000000a")));
        }
    }

    @Nested
    @DisplayName("POST /orders — oppgave 1, 3 og 4")
    class CreateOrder {

        @Test
        @DisplayName("gir 201 med Location-header og hele ordren i body")
        void createsOrder() throws Exception {
            mockMvc.perform(post("/orders")
                            .contentType(MediaType.APPLICATION_JSON)
                            .content("""
                                    {
                                      "coffeeId": "%s",
                                      "customerName": "Ada",
                                      "size": "MEDIUM",
                                      "milkType": "OAT",
                                      "extraShot": true,
                                      "quantity": 2
                                    }""".formatted(LATTE)))
                    .andExpect(status().isCreated())
                    .andExpect(header().string("Location", matchesPattern("/orders/[0-9a-f-]{36}")))
                    .andExpect(jsonPath("$.status", is("PENDING")))
                    .andExpect(jsonPath("$.coffeeName", is("Kaffe Latte")))
                    .andExpect(jsonPath("$.customerName", is("Ada")))
                    .andExpect(jsonPath("$.quantity", is(2)))
                    .andExpect(jsonPath("$.milkType", is("OAT")))
                    .andExpect(jsonPath("$.extraShot", is(true)))
                    .andExpect(jsonPath("$.createdAt").exists())
                    .andExpect(jsonPath("$.updatedAt").exists());
        }

        @Test
        @DisplayName("fyller inn quantity = 1 når feltet mangler — default-verdien kommer fra kontrakten")
        void defaultsQuantityToOne() throws Exception {
            mockMvc.perform(post("/orders")
                            .contentType(MediaType.APPLICATION_JSON)
                            .content("""
                                    {"coffeeId": "%s", "customerName": "Ada", "size": "SMALL"}"""
                                    .formatted(LATTE)))
                    .andExpect(status().isCreated())
                    .andExpect(jsonPath("$.quantity", is(1)))
                    // Valgfrie felt som ikke ble sendt, skal ikke dukke opp som null.
                    .andExpect(jsonPath("$.milkType").doesNotExist())
                    .andExpect(jsonPath("$.extraShot").doesNotExist());
        }

        @Test
        @DisplayName("gir 400 med en liste over feltene som feilet ved blankt navn og for stort antall")
        void rejectsBlankNameAndTooLargeQuantity() throws Exception {
            mockMvc.perform(post("/orders")
                            .contentType(MediaType.APPLICATION_JSON)
                            .content("""
                                    {"coffeeId": "%s", "customerName": "  ", "size": "SMALL", "quantity": 99}"""
                                    .formatted(LATTE)))
                    .andExpect(status().isBadRequest())
                    .andExpect(content().contentTypeCompatibleWith(PROBLEM_JSON))
                    .andExpect(jsonPath("$.type", containsString("validation-error")))
                    .andExpect(jsonPath("$.status", is(400)))
                    .andExpect(jsonPath("$.instance", is("/orders")))
                    .andExpect(jsonPath("$.errors", hasSize(2)))
                    .andExpect(jsonPath("$.errors[*].field",
                            containsInAnyOrder("customerName", "quantity")));
        }

        @Test
        @DisplayName("gir 400 med feltnavn ved ugyldig enum-verdi, selv om Jackson stopper før valideringen")
        void rejectsUnknownEnumValue() throws Exception {
            mockMvc.perform(post("/orders")
                            .contentType(MediaType.APPLICATION_JSON)
                            .content("""
                                    {"coffeeId": "%s", "customerName": "Ada", "size": "HUGE"}"""
                                    .formatted(LATTE)))
                    .andExpect(status().isBadRequest())
                    .andExpect(content().contentTypeCompatibleWith(PROBLEM_JSON))
                    .andExpect(jsonPath("$.errors[0].field", is("size")))
                    .andExpect(jsonPath("$.errors[0].message", containsString("SMALL")));
        }

        @Test
        @DisplayName("gir 400 når et påkrevd felt mangler")
        void rejectsMissingRequiredField() throws Exception {
            mockMvc.perform(post("/orders")
                            .contentType(MediaType.APPLICATION_JSON)
                            .content("""
                                    {"customerName": "Ada", "size": "SMALL"}"""))
                    .andExpect(status().isBadRequest())
                    .andExpect(jsonPath("$.errors[*].field", containsInAnyOrder("coffeeId")));
        }

        @Test
        @DisplayName("gir 404 når kaffen ikke finnes på menyen")
        void rejectsUnknownCoffee() throws Exception {
            mockMvc.perform(post("/orders")
                            .contentType(MediaType.APPLICATION_JSON)
                            .content("""
                                    {"coffeeId": "00000000-0000-4000-8000-000000000000",
                                     "customerName": "Ada", "size": "SMALL"}"""))
                    .andExpect(status().isNotFound())
                    .andExpect(content().contentTypeCompatibleWith(PROBLEM_JSON))
                    .andExpect(jsonPath("$.detail", containsString("GET /menu")));
        }
    }

    @Nested
    @DisplayName("GET /orders/{orderId} — oppgave 5 og 6")
    class GetOrder {

        @Test
        @DisplayName("gir 200 for en ordre som finnes")
        void returnsSeededOrder() throws Exception {
            mockMvc.perform(get("/orders/{orderId}", SEEDED_PENDING))
                    .andExpect(status().isOk())
                    .andExpect(jsonPath("$.customerName", is("Jonas")))
                    .andExpect(jsonPath("$.status", is("PENDING")));
        }

        @Test
        @DisplayName("gir 404 med Problem Details for en ukjent ordre")
        void returnsProblemForUnknownOrder() throws Exception {
            mockMvc.perform(get("/orders/{orderId}", "00000000-0000-0000-0000-000000000000"))
                    .andExpect(status().isNotFound())
                    .andExpect(content().contentTypeCompatibleWith(PROBLEM_JSON))
                    .andExpect(jsonPath("$.type", containsString("not-found")))
                    .andExpect(jsonPath("$.title", is("Ikke funnet")))
                    .andExpect(jsonPath("$.status", is(404)));
        }

        @Test
        @DisplayName("gir 400 og ikke 500 når uuid-en i pathen er ugyldig")
        void returnsBadRequestForMalformedUuid() throws Exception {
            mockMvc.perform(get("/orders/{orderId}", "ikke-en-uuid"))
                    .andExpect(status().isBadRequest())
                    .andExpect(content().contentTypeCompatibleWith(PROBLEM_JSON))
                    .andExpect(jsonPath("$.errors[0].field", is("orderId")));
        }
    }

    @Nested
    @DisplayName("GET /orders — oppgave 8")
    class ListOrders {

        @Test
        @DisplayName("gir alle fire seed-ordrene, nyeste først")
        void returnsNewestFirst() throws Exception {
            mockMvc.perform(get("/orders"))
                    .andExpect(status().isOk())
                    .andExpect(jsonPath("$", hasSize(4)))
                    .andExpect(jsonPath("$[0].customerName", is("Ingrid")))
                    .andExpect(jsonPath("$[3].customerName", is("Ada")));
        }

        @Test
        @DisplayName("filtrerer på status")
        void filtersByStatus() throws Exception {
            mockMvc.perform(get("/orders").param("status", "PENDING"))
                    .andExpect(status().isOk())
                    .andExpect(jsonPath("$", hasSize(2)))
                    .andExpect(jsonPath("$[*].status", containsInAnyOrder("PENDING", "PENDING")));
        }

        @Test
        @DisplayName("pagineres med limit og offset")
        void paginates() throws Exception {
            mockMvc.perform(get("/orders").param("limit", "2").param("offset", "1"))
                    .andExpect(status().isOk())
                    .andExpect(jsonPath("$", hasSize(2)))
                    .andExpect(jsonPath("$[0].customerName", is("Jonas")));
        }

        @Test
        @DisplayName("gir 400 når limit er over 100 — grensen står i kontrakten, ikke i koden")
        void rejectsTooLargeLimit() throws Exception {
            mockMvc.perform(get("/orders").param("limit", "200"))
                    .andExpect(status().isBadRequest())
                    .andExpect(content().contentTypeCompatibleWith(PROBLEM_JSON))
                    .andExpect(jsonPath("$.errors[0].field", is("limit")));
        }

        @Test
        @DisplayName("gir 400 ved en status som ikke finnes")
        void rejectsUnknownStatus() throws Exception {
            mockMvc.perform(get("/orders").param("status", "NOPE"))
                    .andExpect(status().isBadRequest())
                    .andExpect(jsonPath("$.errors[0].field", is("status")));
        }
    }

    @Nested
    @DisplayName("Statusoppdatering — oppgave 7")
    class UpdateStatus {

        @Test
        @DisplayName("PATCH oppdaterer status og updatedAt")
        void patchUpdatesStatus() throws Exception {
            mockMvc.perform(patch("/orders/{orderId}", SEEDED_PENDING)
                            .contentType(MediaType.APPLICATION_JSON)
                            .content("""
                                    {"status": "BREWING"}"""))
                    .andExpect(status().isOk())
                    .andExpect(jsonPath("$.status", is("BREWING")))
                    .andExpect(jsonPath("$.orderId", is(SEEDED_PENDING)));
        }

        @Test
        @DisplayName("POST /status gir nøyaktig det samme som PATCH")
        void aliasBehavesIdentically() throws Exception {
            String viaPatch = mockMvc.perform(patch("/orders/{orderId}", SEEDED_PENDING)
                            .contentType(MediaType.APPLICATION_JSON)
                            .content("""
                                    {"status": "READY"}"""))
                    .andExpect(status().isOk())
                    .andReturn().getResponse().getContentAsString();

            store.reset();

            String viaAlias = mockMvc.perform(post("/orders/{orderId}/status", SEEDED_PENDING)
                            .contentType(MediaType.APPLICATION_JSON)
                            .content("""
                                    {"status": "READY"}"""))
                    .andExpect(status().isOk())
                    .andReturn().getResponse().getContentAsString();

            // Tidsstemplene settes i det øyeblikket kallet kommer inn, og seed-dataene
            // er relative til oppstart, så de to kan aldri bli bit for bit like.
            org.junit.jupiter.api.Assertions.assertEquals(
                    withoutTimestamps(viaPatch), withoutTimestamps(viaAlias),
                    "PATCH og POST /status skal gi identiske svar");
        }

        @Test
        @DisplayName("gir 404 for en ukjent ordre")
        void returnsNotFound() throws Exception {
            mockMvc.perform(patch("/orders/{orderId}", "00000000-0000-0000-0000-000000000000")
                            .contentType(MediaType.APPLICATION_JSON)
                            .content("""
                                    {"status": "READY"}"""))
                    .andExpect(status().isNotFound())
                    .andExpect(content().contentTypeCompatibleWith(PROBLEM_JSON));
        }

        @Test
        @DisplayName("gir 400 ved en status som ikke finnes")
        void rejectsUnknownStatus() throws Exception {
            mockMvc.perform(patch("/orders/{orderId}", SEEDED_PENDING)
                            .contentType(MediaType.APPLICATION_JSON)
                            .content("""
                                    {"status": "DONE"}"""))
                    .andExpect(status().isBadRequest())
                    .andExpect(jsonPath("$.errors[0].field", is("status")));
        }

        private static String withoutTimestamps(String json) {
            return json.replaceAll("\"(createdAt|updatedAt)\":\"[^\"]+\"", "\"$1\":\"…\"");
        }
    }

    @Nested
    @DisplayName("GET /health")
    class Health {

        @Test
        @DisplayName("rapporterer rabbitmq = disabled når hendelser er avslått")
        void reportsDisabled() throws Exception {
            mockMvc.perform(get("/health"))
                    .andExpect(status().isOk())
                    .andExpect(jsonPath("$.status", is("ok")))
                    .andExpect(jsonPath("$.rabbitmq", is("disabled")));
        }
    }
}
