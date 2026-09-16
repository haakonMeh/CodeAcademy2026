package no.soprasteria.kaffebar.events;

import org.springframework.boot.context.properties.ConfigurationProperties;

/**
 * Konfigurasjonen for det valgfrie siste steget: hendelser til RabbitMQ og
 * auto-baristaen. Begge er AV som standard — se application.properties og FASIT.md.
 */
@ConfigurationProperties(prefix = "kaffebar")
public class KaffebarProperties {

    private final Events events = new Events();
    private final Barista barista = new Barista();

    public Events getEvents() {
        return events;
    }

    public Barista getBarista() {
        return barista;
    }

    public static class Events {
        /** Publiser order.created og order.status-changed til RabbitMQ. */
        private boolean enabled = false;
        /** Topic exchange, durable. Samme navn som i 4-Frontend/kaffebar-api. */
        private String exchange = "kaffebar";

        public boolean isEnabled() {
            return enabled;
        }

        public void setEnabled(boolean enabled) {
            this.enabled = enabled;
        }

        public String getExchange() {
            return exchange;
        }

        public void setExchange(String exchange) {
            this.exchange = exchange;
        }
    }

    public static class Barista {
        /** Flytt ordrer PENDING -> BREWING -> READY av seg selv. */
        private boolean autoEnabled = false;
        private long intervalMs = 8000;

        public boolean isAutoEnabled() {
            return autoEnabled;
        }

        public void setAutoEnabled(boolean autoEnabled) {
            this.autoEnabled = autoEnabled;
        }

        public long getIntervalMs() {
            return intervalMs;
        }

        public void setIntervalMs(long intervalMs) {
            this.intervalMs = intervalMs;
        }
    }
}
