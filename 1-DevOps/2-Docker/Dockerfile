# Stage 1 — deps: install dependencies
FROM node:20-alpine AS deps

RUN apk add --no-cache libc6-compat

WORKDIR /app

RUN corepack enable

COPY package.json yarn.lock .yarnrc.yml ./

RUN yarn install --immutable

# Stage 2 — builder: build the application
FROM node:20-alpine AS builder

WORKDIR /app

RUN corepack enable

ARG NEXT_PUBLIC_ENABLE_IDEM_FORM=false
ARG NEXT_PUBLIC_SHOW_SEEDED_IDEMS=false

ENV NEXT_PUBLIC_ENABLE_IDEM_FORM=$NEXT_PUBLIC_ENABLE_IDEM_FORM
ENV NEXT_PUBLIC_SHOW_SEEDED_IDEMS=$NEXT_PUBLIC_SHOW_SEEDED_IDEMS

COPY --from=deps /app/node_modules ./node_modules
COPY --from=deps /app/.yarn ./.yarn

COPY . .

RUN yarn build

# Stage 3 — runner: production image
FROM node:20-alpine AS runner

WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

RUN addgroup --system --gid 1001 nodejs \
    && adduser --system --uid 1001 nextjs

COPY --from=builder /app/public ./public
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

USER nextjs

EXPOSE 3000

ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

CMD ["node", "server.js"]
