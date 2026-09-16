package no.soprasteria.kaffebar.bakst;

import no.soprasteria.kaffebar.bakst.model.CoffeeItem;
import no.soprasteria.kaffebar.bakst.model.CreateMixedOrderRequest;
import no.soprasteria.kaffebar.bakst.model.OrderItem;
import no.soprasteria.kaffebar.bakst.model.PastryItem;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import tools.jackson.databind.json.JsonMapper;

import java.util.List;
import java.util.UUID;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertInstanceOf;
import static org.junit.jupiter.api.Assertions.assertTrue;

/**
 * Beviser at det generert oppsettet faktisk virker begge veier. Det er lett å se på
 * den genererte koden og tro at den gjør jobben; disse to testene er forskjellen på å
 * tro og å vite.
 *
 * Kjør: mvn -f oppgave-9/pom.xml test
 */
class PolymorfismeTest {

    private final JsonMapper json = JsonMapper.builder().build();

    @Test
    @DisplayName("serialisering: Jackson skriver diskriminatoren selv")
    void serializesDiscriminator() {
        CoffeeItem coffee = new CoffeeItem();
        coffee.setType("coffee");
        coffee.setCoffeeId(UUID.fromString("c0ffee01-0000-4000-8000-000000000007"));
        coffee.setSize(CoffeeItem.SizeEnum.MEDIUM);
        coffee.setMilkType(CoffeeItem.MilkTypeEnum.OAT);

        PastryItem pastry = new PastryItem();
        pastry.setType("pastry");
        pastry.setPastryId(UUID.fromString("ba57ee01-0000-4000-8000-000000000001"));
        pastry.setIsVegan(true);

        CreateMixedOrderRequest request = new CreateMixedOrderRequest("Ada", List.of(coffee, pastry));
        String body = json.writeValueAsString(request);

        assertTrue(body.contains("\"type\":\"coffee\""), body);
        assertTrue(body.contains("\"type\":\"pastry\""), body);
        assertTrue(body.contains("\"milkType\":\"OAT\""), body);
        assertTrue(body.contains("\"isVegan\":true"), body);
    }

    @Test
    @DisplayName("deserialisering: diskriminatoren velger riktig klasse")
    void deserializesIntoCorrectSubtype() {
        String body = """
                {
                  "customerName": "Ada",
                  "items": [
                    {"type": "coffee", "coffeeId": "c0ffee01-0000-4000-8000-000000000007",
                     "size": "LARGE", "milkType": "SOY"},
                    {"type": "pastry", "pastryId": "ba57ee01-0000-4000-8000-000000000001",
                     "isVegan": false, "warmed": true}
                  ]
                }""";

        CreateMixedOrderRequest request = json.readValue(body, CreateMixedOrderRequest.class);

        List<OrderItem> items = request.getItems();
        assertEquals(2, items.size());

        CoffeeItem coffee = assertInstanceOf(CoffeeItem.class, items.get(0));
        assertEquals(CoffeeItem.SizeEnum.LARGE, coffee.getSize());
        assertEquals(CoffeeItem.MilkTypeEnum.SOY, coffee.getMilkType());
        assertEquals("coffee", coffee.getType());

        PastryItem pastry = assertInstanceOf(PastryItem.class, items.get(1));
        assertEquals(Boolean.FALSE, pastry.getIsVegan());
        assertEquals(Boolean.TRUE, pastry.getWarmed());
    }
}
