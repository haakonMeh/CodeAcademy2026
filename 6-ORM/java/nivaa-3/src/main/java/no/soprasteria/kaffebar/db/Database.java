package no.soprasteria.kaffebar.db;

import jakarta.persistence.EntityManagerFactory;
import jakarta.persistence.Persistence;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.testcontainers.postgresql.PostgreSQLContainer;

import java.util.Map;

/**
 * Starter en PostgreSQL-container med Testcontainers, lager EntityManagerFactory
 * og fyller databasen med testdata. Brukes både av KaffebarApp og testene.
 */
public final class Database {

    private static final Logger LOG = LoggerFactory.getLogger(Database.class);

    private static final String POSTGRES_IMAGE = "postgres:18-alpine";

    private static PostgreSQLContainer postgres;
    private static EntityManagerFactory emf;

    private Database() {
    }

    public static synchronized EntityManagerFactory start() {
        if (emf == null) {
            postgres = new PostgreSQLContainer(POSTGRES_IMAGE)
                    .withDatabaseName("kaffebar")
                    .withUsername("barista")
                    .withPassword("barista");
            postgres.start();

            emf = Persistence.createEntityManagerFactory("kaffebar", Map.of(
                    "jakarta.persistence.jdbc.url", postgres.getJdbcUrl(),
                    "jakarta.persistence.jdbc.user", postgres.getUsername(),
                    "jakarta.persistence.jdbc.password", postgres.getPassword()));

            TestData.fyllDatabasen(emf);
            QueryCountLogger.reset();

            LOG.info("Postgres kjører på {} (bruker/passord: {}/{})",
                    postgres.getJdbcUrl(), postgres.getUsername(), postgres.getPassword());
        }
        return emf;
    }
}
