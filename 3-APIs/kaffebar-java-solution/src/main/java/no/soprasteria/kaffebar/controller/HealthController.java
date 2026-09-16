package no.soprasteria.kaffebar.controller;

import no.soprasteria.kaffebar.api.HealthApi;
import no.soprasteria.kaffebar.events.OrderEventPublisher;
import no.soprasteria.kaffebar.model.Health;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.RestController;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.time.Duration;
import java.time.Instant;

/**
 * Ikke en del av oppgavesettet, men en del av referansekontrakten — Docker sin
 * healthcheck bruker den. Tatt med her slik at de tre implementasjonene eksponerer
 * nøyaktig de samme endepunktene.
 */
@RestController
public class HealthController implements HealthApi {

    private final Instant startedAt = Instant.now();
    private final OrderEventPublisher events;

    public HealthController(OrderEventPublisher events) {
        this.events = events;
    }

    @Override
    public ResponseEntity<Health> getHealth() {
        BigDecimal uptime = BigDecimal
                .valueOf(Duration.between(startedAt, Instant.now()).toMillis() / 1000.0)
                .setScale(3, RoundingMode.HALF_UP);

        return ResponseEntity.ok(new Health(
                Health.StatusEnum.OK,
                events.connectionState(),
                uptime));
    }
}
