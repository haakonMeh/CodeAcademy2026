package no.soprasteria.kaffebar.config;

import io.swagger.v3.core.jackson.ModelResolver;
import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.info.Contact;
import io.swagger.v3.oas.models.info.Info;
import io.swagger.v3.oas.models.info.License;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.servlet.config.annotation.ViewControllerRegistry;
import org.springframework.web.servlet.config.annotation.WebMvcConfigurer;

/**
 * Tre små justeringer på spesifikasjonen springdoc serverer.
 *
 * Husk hva den fila faktisk er: springdoc leser den KJØRENDE koden — de genererte
 * interfacene og modellene — og bygger en spesifikasjon ut av den. Den er altså ikke
 * kontrakten vår; den er en beskrivelse av det vi endte opp med. At de to er like er
 * nettopp det contract-first skal gi deg, og et fint sted å kontrollere at du ikke har
 * rotet det til.
 */
@Configuration
public class OpenApiConfiguration implements WebMvcConfigurer {

    static {
        // 1. Uten denne skriver springdoc enum-verdiene rett inn i hvert felt i stedet
        //    for å referere til et gjenbrukbart skjema. Wire-formatet blir det samme,
        //    men `npx openapi-typescript` ville da ikke gitt deg noen `OrderStatus`-type
        //    — og src/types/domain.ts i kaffebar-web slår nettopp opp
        //    components["schemas"]["OrderStatus"]. Med denne på blir de genererte typene
        //    strukturelt like dem fra referansekontrakten.
        ModelResolver.enumsAsRef = true;
    }

    /** 2. Uten denne heter API-et «OpenAPI definition» versjon «v0» i Swagger-UI-et. */
    @Bean
    OpenAPI kaffebarOpenApi() {
        return new OpenAPI().info(new Info()
                .title("Kaffebar API")
                .version("1.0.0")
                .description("""
                        Et contract-first API for å administrere bestillinger i en kaffebar.

                        Fasit for samling 3, Java-sporet. Lagrer alt i minne.""")
                .contact(new Contact()
                        .name("Sopra Steria Code Academy")
                        .url("https://github.com/Sopra-Steria-Code-Academy/CodeAcademy2026"))
                .license(new License().name("MIT")));
    }

    /**
     * 3. Referanse-implementasjonen i 4-Frontend serverer spesifikasjonen på
     * {@code /openapi.json}, og det er den stien `yarn generate:types` i kaffebar-web
     * peker på. springdoc bruker {@code /v3/api-docs}. Denne omdirigeringen gjør at
     * bonussteget i samling 4 virker uten at noen må endre et script.
     *
     * Den er lagt inn som en view controller og ikke som en {@code @GetMapping}, fordi
     * springdoc ville dokumentert en {@code @GetMapping} — og da hadde halve Spring
     * sitt indre liv havnet i components/schemas.
     */
    @Override
    public void addViewControllers(ViewControllerRegistry registry) {
        registry.addRedirectViewController("/openapi.json", "/v3/api-docs");
    }
}
