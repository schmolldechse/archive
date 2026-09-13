FROM oven/bun:1 AS build
WORKDIR /app
COPY package.json bun.lock ./
RUN bun install --frozen-lockfile
COPY . .
ARG PUBLIC_API_BASE_URL=http://localhost:5200
ENV PUBLIC_API_BASE_URL=$PUBLIC_API_BASE_URL
RUN bun run build

FROM oven/bun:distroless AS runtime
WORKDIR /app
ENV NODE_ENV=production
ENV HOST=0.0.0.0
ENV PORT=3000
COPY --from=build --chown=65532:65532 /app/build ./build
USER 65532:65532
EXPOSE 3000
ENTRYPOINT ["/usr/local/bin/bun", "./build/index.js"]
