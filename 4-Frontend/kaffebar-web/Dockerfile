# EKSTRAOPPGAVE 7 — containeriser frontenden.  Se 4-Frontend/oppgave.md.
#
# Du har gjort dette før: Dockerfilene i 3-APIs/kaffebar-*-solution er det samme
# multi-stage-mønsteret i ditt eget språk. Bruk dem som mal.
#
# Fasit: Dockerfile-solution i denne mappa.
#
# Det du skal ende opp med:
#   Lag 1 — deps:    node:24-alpine, `corepack enable`, kopier package.json /
#                     yarn.lock / .yarnrc.yml, kjør `yarn install --immutable`
#   Lag 2 — builder: kopier node_modules fra deps + resten av koden, `yarn build`
#   Lag 3 — runner:  ikke-root bruker, kopier `.next/standalone`, `.next/static`
#                     og `public`, EXPOSE 3000, `CMD ["node", "server.js"]`
#
# Hvorfor standalone? next.config.ts setter `output: "standalone"`. Next samler
# da akkurat de filene fra node_modules som appen faktisk bruker, i én mappe.
# Bildet blir en brøkdel av størrelsen, og du slipper å kopiere node_modules.
#
# Når du er ferdig, bygg og kjør hele miljøet fra rota av repoet:
#   docker compose --profile java --profile frontend up --build

# TODO (oppgave 7): erstatt de to linjene under med multi-stage-bygget ditt.
FROM node:24-alpine
RUN echo "kaffebar-web/Dockerfile er ikke skrevet enda. Se 4-Frontend/oppgave.md, oppgave 7, eller kopier inn Dockerfile-solution." >&2 && exit 1
