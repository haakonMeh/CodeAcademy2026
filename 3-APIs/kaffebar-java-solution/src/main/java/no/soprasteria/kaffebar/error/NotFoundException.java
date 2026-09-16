package no.soprasteria.kaffebar.error;

/**
 * Kastes når en ordre eller en kaffe ikke finnes. Oversettes til 404 med RFC
 * 7807-body i {@link ApiExceptionHandler}.
 *
 * Poenget med en egen exception er at kontrollerne slipper å bygge feilsvar. De sier
 * «dette finnes ikke», og ett sted i kodebasen bestemmer hvordan det ser ut på tråden.
 */
public class NotFoundException extends RuntimeException {

    public NotFoundException(String detail) {
        super(detail);
    }
}
