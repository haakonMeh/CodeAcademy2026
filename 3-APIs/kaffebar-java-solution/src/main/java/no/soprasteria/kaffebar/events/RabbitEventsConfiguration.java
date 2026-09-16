package no.soprasteria.kaffebar.events;

import org.springframework.amqp.core.TopicExchange;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * VALGFRITT — hører sammen med {@link OrderEventPublisher}.
 *
 * Exchangen er deklarert som en bean og ikke i en oppstartsmetode. Det er ikke
 * kosmetikk: Spring AMQP sin {@code RabbitAdmin} lytter på tilkoblinger og deklarerer
 * alle slike beans i det øyeblikket en forbindelse opprettes — altså før den første
 * meldingen kan bli publisert på den. Deklarerer du i stedet exchangen fra en
 * {@code ApplicationReadyEvent}-metode, kan en {@code @Scheduled}-jobb (auto-baristaen
 * kjører første runde med en gang) rekke å publisere først, og du får
 * «NOT_FOUND - no exchange 'kaffebar'» og en tapt melding.
 *
 * Det gjelder også etter en reconnect: kommer broker tilbake, blir exchangen deklarert
 * på nytt uten at noen må gjøre noe.
 *
 * Hele klassen er betinget av {@code kaffebar.events.enabled}. Er flagget av, finnes
 * ingen av disse beanene og ingenting kobler til noe som helst.
 */
@Configuration
@ConditionalOnProperty(prefix = "kaffebar.events", name = "enabled", havingValue = "true")
public class RabbitEventsConfiguration {

    @Bean
    TopicExchange kaffebarExchange(KaffebarProperties properties) {
        return new TopicExchange(properties.getEvents().getExchange(), true, false);
    }
}
