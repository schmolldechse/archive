FROM oven/bun:1 AS build
WORKDIR /app
COPY package.json bun.lock ./
RUN bun install --frozen-lockfile
COPY . .
RUN bun run build
RUN bun install --production --frozen-lockfile

FROM oven/bun:distroless AS runtime
WORKDIR /app
ENV NODE_ENV=production
ENV HOST=0.0.0.0
ENV PORT=3000
COPY --from=build --chown=65532:65532 /app/build ./build
COPY --from=build --chown=65532:65532 /app/node_modules ./node_modules
USER 65532:65532
EXPOSE 3000
ENTRYPOINT ["/usr/local/bin/bun", "./build/index.js"]
