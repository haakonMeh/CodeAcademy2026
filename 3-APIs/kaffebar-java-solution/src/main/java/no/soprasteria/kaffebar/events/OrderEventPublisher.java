package no.soprasteria.kaffebar.events;

// Spring Boot 4 leverer Jackson 3 (tools.jackson) som JSON-mapper. Den genererte
// koden bruker fortsatt Jackson 2-annotasjonene, som Jackson 3 leser som før.
import tools.jackson.databind.ObjectMapper;
import no.soprasteria.kaffebar.model.Health;
import no.soprasteria.kaffebar.model.Order;
import no.soprasteria.kaffebar.model.OrderStatus;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.core.Message;
import org.springframework.amqp.core.MessageDeliveryMode;
import org.springframework.amqp.core.MessageProperties;
import org.springframework.amqp.rabbit.connection.Connection;
import org.springframework.amqp.rabbit.connection.ConnectionFactory;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.beans.factory.ObjectProvider;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Component;

import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.time.format.DateTimeFormatter;
import java.time.temporal.ChronoUnit;

/**
 * VALGFRITT SISTE STEG — ikke en del av oppgavesettet i samling 3.
 *
 * Oppgavene sier ingenting om hendelser. Men peker en deltaker frontenden i samling 4
 * på sitt eget API fra mai, får hen bare steg 1 (data over HTTP), ikke steg 2 (live
 * oppdatering i to vinduer samtidig). Derfor ligger publiseringen her — bak
 * {@code kaffebar.events.enabled}, som er {@code false} som standard.
 *
 * Er flagget av, gjør denne klassen ingenting i det hele tatt. Ingen tilkobling,
 * ingen logglinjer, ingen endret oppførsel på HTTP-siden.
 *
 * Samme exchange (`kaffebar`, topic, durable), samme routing keys
 * (`order.created`, `order.status-changed`) og samme meldingsformat som
 * 4-Frontend/kaffebar-api.
 */
@Component
public class OrderEventPublisher {

    private static final Logger log = LoggerFactory.getLogger(OrderEventPublisher.class);

    private final KaffebarProperties properties;
    private final ObjectProvider<RabbitTemplate> rabbitTemplate;
    private final ObjectProvider<ConnectionFactory> connectionFactory;
    private final ObjectMapper objectMapper;

    private volatile boolean connected = false;

    public OrderEventPublisher(KaffebarProperties properties,
                               ObjectProvider<RabbitTemplate> rabbitTemplate,
                               ObjectProvider<ConnectionFactory> connectionFactory,
                               ObjectMapper objectMapper) {
        this.properties = properties;
        this.rabbitTemplate = rabbitTemplate;
        this.connectionFactory = connectionFactory;
        this.objectMapper = objectMapper;
    }

    /**
     * Kobler til ved oppstart, slik at {@code GET /health} kan si sannheten med én gang
     * og exchangen blir deklarert av RabbitAdmin. Kaster aldri: API-et skal starte og
     * fungere selv om broker er nede, akkurat som referanse-implementasjonen.
     */
    @EventListener(ApplicationReadyEvent.class)
    void connect() {
        if (!properties.getEvents().isEnabled()) {
            return;
        }
        try (Connection connection = connectionFactory.getObject().createConnection()) {
            connected = connection.isOpen();
            log.info("Koblet til RabbitMQ. Exchange \"{}\" (topic, durable).",
                    properties.getEvents().getExchange());
        } catch (Exception exception) {
            connected = false;
            log.warn("Får ikke kontakt med RabbitMQ ({}). API-et kjører videre uten hendelser.",
                    exception.getMessage());
        }
    }

    public void orderCreated(Order order) {
        publish("order.created", new KaffebarEvent("order.created", timestamp(), order, null));
    }

    public void orderStatusChanged(Order order, OrderStatus previousStatus) {
        publish("order.status-changed",
                new KaffebarEvent("order.status-changed", timestamp(), order, previousStatus));
    }

    /** Hva {@code GET /health} skal rapportere. */
    public Health.RabbitmqEnum connectionState() {
        if (!properties.getEvents().isEnabled()) {
            return Health.RabbitmqEnum.DISABLED;
        }
        return connected ? Health.RabbitmqEnum.CONNECTED : Health.RabbitmqEnum.DISCONNECTED;
    }

    private void publish(String routingKey, KaffebarEvent event) {
        if (!properties.getEvents().isEnabled()) {
            return;
        }
        try {
            MessageProperties messageProperties = new MessageProperties();
            messageProperties.setContentType(MessageProperties.CONTENT_TYPE_JSON);
            messageProperties.setContentEncoding("UTF-8");
            messageProperties.setDeliveryMode(MessageDeliveryMode.PERSISTENT);

            Message message = new Message(objectMapper.writeValueAsBytes(event), messageProperties);
            rabbitTemplate.getObject().send(properties.getEvents().getExchange(), routingKey, message);
            connected = true;
        } catch (Exception exception) {
            // Hendelser er en bonus. En broker som er nede skal aldri gjøre et
            // HTTP-kall som ellers gikk bra om til en 500.
            connected = false;
            log.warn("Klarte ikke å publisere {}: {}", routingKey, exception.getMessage());
        }
    }

    private static String timestamp() {
        return DateTimeFormatter.ISO_OFFSET_DATE_TIME.format(
                OffsetDateTime.now(ZoneOffset.UTC).truncatedTo(ChronoUnit.MILLIS));
    }
}
