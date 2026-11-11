package no.soprasteria.kaffebar.db;

import org.hibernate.resource.jdbc.spi.StatementInspector;

import java.util.Locale;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * Teller SQL-setningene Hibernate sender til databasen.
 * Registrert i persistence.xml via {@code hibernate.session_factory.statement_inspector}.
 *
 * <p>Teller hver gang Hibernate <em>forbereder</em> en setning. Med JDBC-batching gjenbrukes én
 * forberedt INSERT for mange rader, så tallet blir lavere enn antall rader som skrives.
 *
 * <pre>
 * QueryCountLogger.reset();
 * ... kode som snakker med databasen ...
 * log.info(QueryCountLogger.oppsummering());
 * </pre>
 */
public class QueryCountLogger implements StatementInspector {

    private static final AtomicInteger SELECT = new AtomicInteger();
    private static final AtomicInteger INSERT = new AtomicInteger();
    private static final AtomicInteger UPDATE = new AtomicInteger();
    private static final AtomicInteger DELETE = new AtomicInteger();

    @Override
    public String inspect(String sql) {
        String type = sql.stripLeading().toLowerCase(Locale.ROOT);
        if (type.startsWith("select") || type.startsWith("with")) {
            SELECT.incrementAndGet();
        } else if (type.startsWith("insert")) {
            INSERT.incrementAndGet();
        } else if (type.startsWith("update")) {
            UPDATE.incrementAndGet();
        } else if (type.startsWith("delete")) {
            DELETE.incrementAndGet();
        }
        return sql;
    }

    public static void reset() {
        SELECT.set(0);
        INSERT.set(0);
        UPDATE.set(0);
        DELETE.set(0);
    }

    public static int getSelectCount() {
        return SELECT.get();
    }

    public static int getInsertCount() {
        return INSERT.get();
    }

    public static int getUpdateCount() {
        return UPDATE.get();
    }

    public static int getDeleteCount() {
        return DELETE.get();
    }

    public static String oppsummering() {
        return "SQL-setninger: select=%d, insert=%d, update=%d, delete=%d"
                .formatted(getSelectCount(), getInsertCount(), getUpdateCount(), getDeleteCount());
    }
}
