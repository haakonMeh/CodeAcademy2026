package no.soprasteria.kaffebar.error;

import jakarta.servlet.http.HttpServletRequest;
import jakarta.validation.ConstraintViolation;
import jakarta.validation.ConstraintViolationException;
import no.soprasteria.kaffebar.model.ValidationError;
import org.springframework.context.MessageSourceResolvable;
import org.springframework.http.HttpStatus;
import org.springframework.http.ProblemDetail;
import org.springframework.http.converter.HttpMessageNotReadableException;
import org.springframework.validation.FieldError;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import org.springframework.web.method.annotation.HandlerMethodValidationException;
import org.springframework.web.method.annotation.MethodArgumentTypeMismatchException;
import tools.jackson.core.JacksonException;
import tools.jackson.databind.exc.MismatchedInputException;
import tools.jackson.databind.exc.ValueInstantiationException;

import java.net.URI;
import java.util.List;
import java.util.stream.Collectors;

/**
 * Oppgave 6: all feilhåndtering ett sted.
 *
 * Alt som går galt havner her og kommer ut som RFC 7807 Problem Details med
 * {@code Content-Type: application/problem+json}. Spring har {@link ProblemDetail}
 * innebygd og setter media typen selv når en handler returnerer den — vi trenger ikke
 * den genererte {@code Problem}-klassen for å svare, bare for å beskrive svaret i
 * kontrakten.
 *
 * {@code errors}-lista legges på som en RFC 7807-extension. Standarden sier
 * eksplisitt at man kan utvide objektet med egne felt, og det er nettopp der en liste
 * over felt som ikke validerte hører hjemme.
 */
@RestControllerAdvice
public class ApiExceptionHandler {

    private static final String TYPE_BASE = "https://codeacademy.soprasteria.no/problems";

    /** Ukjent ordre eller ukjent kaffe. */
    @ExceptionHandler(NotFoundException.class)
    ProblemDetail handleNotFound(NotFoundException exception, HttpServletRequest request) {
        return problem(HttpStatus.NOT_FOUND, "not-found", "Ikke funnet",
                exception.getMessage(), request, null);
    }

    /**
     * Oppgave 4: {@code @Valid} på request-bodyen feilet. Dette er annotasjonene
     * generatoren laget av minLength/maxLength/pattern/minimum/maximum i YAML-en.
     */
    @ExceptionHandler(MethodArgumentNotValidException.class)
    ProblemDetail handleInvalidBody(MethodArgumentNotValidException exception,
                                    HttpServletRequest request) {
        List<ValidationError> errors = exception.getBindingResult().getAllErrors().stream()
                .map(error -> new ValidationError(
                        error instanceof FieldError fieldError ? fieldError.getField() : error.getObjectName(),
                        error.getDefaultMessage() == null ? "er ugyldig" : error.getDefaultMessage()))
                .toList();

        return validationProblem(errors, request);
    }

    /**
     * Validering av metodeparametere — {@code @Min(1) @Max(100)} på {@code limit} og
     * {@code offset} i det genererte interfacet. Spring kaster denne i stedet for
     * {@code MethodArgumentNotValidException} fordi det ikke er en request-body som
     * feilet, men et enkeltstående argument.
     */
    /**
     * Validering av query-parametere — {@code @Min(1) @Max(100)} på {@code limit} i
     * det genererte interfacet.
     *
     * Det generete interfacet er annotert med {@code @Validated}, og da er det Springs
     * AOP-baserte metodevalidering som slår inn. Den kaster
     * {@link ConstraintViolationException}, ikke {@code MethodArgumentNotValidException}.
     * Uten denne handleren gir {@code ?limit=200} en 500 — og det er en av de
     * vanligste overraskelsene i dette oppsettet.
     */
    @ExceptionHandler(ConstraintViolationException.class)
    ProblemDetail handleConstraintViolations(ConstraintViolationException exception,
                                             HttpServletRequest request) {
        List<ValidationError> errors = exception.getConstraintViolations().stream()
                .map(violation -> new ValidationError(lastNode(violation), violation.getMessage()))
                .toList();

        return validationProblem(
                errors.isEmpty() ? List.of(new ValidationError("request", "er ugyldig")) : errors,
                request);
    }

    /**
     * Samme feil, men fra Spring MVC sin innebygde metodevalidering (den som brukes
     * når {@code @Validated} ikke står på klassen). Begge veier skal ende i det samme
     * svaret.
     */
    @ExceptionHandler(HandlerMethodValidationException.class)
    ProblemDetail handleInvalidParameters(HandlerMethodValidationException exception,
                                          HttpServletRequest request) {
        List<ValidationError> errors = exception.getParameterValidationResults().stream()
                .flatMap(result -> {
                    String field = result.getMethodParameter().getParameterName();
                    String name = field == null ? "request" : field;
                    return result.getResolvableErrors().stream()
                            .map(MessageSourceResolvable::getDefaultMessage)
                            .map(message -> new ValidationError(
                                    name, message == null ? "er ugyldig" : message));
                })
                .toList();

        if (errors.isEmpty()) {
            errors = List.of(new ValidationError("request", "er ugyldig"));
        }
        return validationProblem(errors, request);
    }

    /**
     * En ugyldig uuid i pathen — {@code GET /orders/ikke-en-uuid}.
     *
     * Uten denne blir det 500. Spring klarer ikke å konvertere strengen til
     * {@code UUID}, og en konverteringsfeil er ikke serverens skyld. 400 er riktig
     * svar, og forskjellen er verdt å stoppe opp ved: .NET-sporet svarer 404 på det
     * samme kallet, fordi ruten {@code {orderId:guid}} ikke matcher i det hele tatt.
     */
    @ExceptionHandler(MethodArgumentTypeMismatchException.class)
    ProblemDetail handleTypeMismatch(MethodArgumentTypeMismatchException exception,
                                     HttpServletRequest request) {
        String field = exception.getName();
        String expected = exception.getRequiredType() == null
                ? "riktig type" : exception.getRequiredType().getSimpleName();

        return validationProblem(
                List.of(new ValidationError(field, "må være en gyldig " + expected)),
                request);
    }

    /**
     * Bodyen kunne ikke leses. Den vanligste årsaken i dette API-et er en enum-verdi
     * som ikke finnes — {@code "size": "HUGE"}. Jackson rekker aldri å lage objektet,
     * så {@code @Valid} kjører aldri, og vi må grave feltnavnet ut av Jackson-feilen
     * selv for å få det inn i {@code errors}-lista.
     *
     * Det er verdt å vite at dette er to ulike mekanismer: minLength og maximum
     * håndheves av Bean Validation ETTER at objektet er bygget, mens en ugyldig
     * enum-verdi stopper allerede i deserialiseringen. Begge skal ut som 400 med en
     * feltliste, men de kommer hit langs hver sin vei.
     */
    @ExceptionHandler(HttpMessageNotReadableException.class)
    ProblemDetail handleUnreadableBody(HttpMessageNotReadableException exception,
                                       HttpServletRequest request) {
        Throwable cause = exception.getCause();

        if (cause instanceof JacksonException jackson && !jackson.getPath().isEmpty()) {
            Class<?> targetType = targetTypeOf(jackson);
            String message = targetType != null && targetType.isEnum()
                    ? "må være en av " + allowedValues(targetType)
                    : "kunne ikke leses";
            return validationProblem(
                    List.of(new ValidationError(path(jackson), message)), request);
        }

        return problem(HttpStatus.BAD_REQUEST, "validation-error", "Ugyldig forespørsel",
                "Requesten kunne ikke leses som gyldig JSON.", request, null);
    }

    private ProblemDetail validationProblem(List<ValidationError> errors, HttpServletRequest request) {
        String fields = errors.stream()
                .map(ValidationError::getField)
                .distinct()
                .collect(Collectors.joining(", "));

        return problem(HttpStatus.BAD_REQUEST, "validation-error", "Ugyldig forespørsel",
                "Valideringen feilet for %d felt: %s.".formatted(errors.size(), fields),
                request, errors);
    }

    private ProblemDetail problem(HttpStatus status, String slug, String title, String detail,
                                  HttpServletRequest request, List<ValidationError> errors) {
        ProblemDetail problem = ProblemDetail.forStatusAndDetail(status, detail);
        problem.setType(URI.create(TYPE_BASE + "/" + slug));
        problem.setTitle(title);
        problem.setInstance(URI.create(request.getRequestURI()));
        if (errors != null) {
            problem.setProperty("errors", errors);
        }
        return problem;
    }

    /** "listOrders.limit" -> "limit". Klienten bryr seg ikke om metodenavnet vårt. */
    private static String lastNode(ConstraintViolation<?> violation) {
        String path = violation.getPropertyPath().toString();
        int dot = path.lastIndexOf('.');
        return dot < 0 ? path : path.substring(dot + 1);
    }

    private static String path(JacksonException exception) {
        return exception.getPath().stream()
                .map(reference -> reference.getPropertyName() == null
                        ? "[" + reference.getIndex() + "]" : reference.getPropertyName())
                .collect(Collectors.joining("."));
    }

    /** Jackson pakker feilen ulikt for en enum-fabrikk og for en vanlig typefeil. */
    private static Class<?> targetTypeOf(JacksonException exception) {
        if (exception instanceof ValueInstantiationException instantiation
                && instantiation.getType() != null) {
            return instantiation.getType().getRawClass();
        }
        if (exception instanceof MismatchedInputException mismatched) {
            return mismatched.getTargetType();
        }
        return null;
    }

    private static String allowedValues(Class<?> enumType) {
        return java.util.Arrays.stream(enumType.getEnumConstants())
                .map(Object::toString)
                .collect(Collectors.joining(", "));
    }
}
