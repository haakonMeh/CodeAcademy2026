package no.soprasteria.kaffebar.controller;

import no.soprasteria.kaffebar.api.MenuApi;
import no.soprasteria.kaffebar.model.Coffee;
import no.soprasteria.kaffebar.store.Menu;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;

/**
 * Oppgave 2: én kontrollerklasse per generert API-interface.
 *
 * Denne står nesten som i starteren. Forskjellen er at menyen nå ligger i
 * {@link Menu} med faste uuid-er i stedet for {@code UUID.randomUUID()} inne i
 * metoden — se kommentaren der.
 */
@RestController
public class MenuController implements MenuApi {

    @Override
    public ResponseEntity<List<Coffee>> getMenu() {
        return ResponseEntity.ok(Menu.ITEMS);
    }
}
