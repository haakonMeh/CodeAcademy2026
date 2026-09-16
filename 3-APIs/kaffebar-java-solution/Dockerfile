# Kaffebar API — Java-sporet. Fasit fra samling 3, datakilde for samling 4.
#
# Multi-stage, samme prinsipp som 1-DevOps/2-Docker/Dockerfile: bygg i et image
# med hele verktøykassa, kjør i et som bare har det du trenger.

# --- Stage 1: bygg ----------------------------------------------------------
FROM maven:3.9-eclipse-temurin-21 AS build
WORKDIR /build

# Avhengighetene først, i sitt eget lag. Endrer du bare kildekode, gjenbrukes
# dette laget og bygget går på sekunder i stedet for minutter.
COPY pom.xml .
RUN mvn -B -q dependency:go-offline

COPY src ./src
RUN mvn -B -q package -DskipTests

# --- Stage 2: kjøring -------------------------------------------------------
FROM eclipse-temurin:21-jre-alpine AS runner
WORKDIR /app

# curl trengs av healthchecken i docker-compose.yml.
RUN apk add --no-cache curl \
 && addgroup -S kaffebar && adduser -S kaffebar -G kaffebar

# Bare den ferdige jar-en. Ingen kildekode, ingen Maven, ingen JDK.
COPY --from=build /build/target/kaffebar-api-solution-*.jar app.jar

USER kaffebar
EXPOSE 8080
ENTRYPOINT ["java", "-jar", "app.jar"]
