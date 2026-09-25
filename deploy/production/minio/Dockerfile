# syntax=docker/dockerfile:1
FROM golang:1.25-bookworm AS server-build
RUN CGO_ENABLED=0 go install github.com/minio/minio@latest

FROM golang:1.25-bookworm AS client-build
RUN CGO_ENABLED=0 go install github.com/minio/mc@latest

FROM alpine:3.22 AS server
RUN apk add --no-cache ca-certificates \
    && addgroup -S minio && adduser -S -G minio minio \
    && mkdir /data && chown minio:minio /data
COPY --from=server-build /go/bin/minio /usr/local/bin/minio
USER minio
EXPOSE 9000 9001
ENTRYPOINT ["/usr/local/bin/minio"]

# The existing local Compose volume was written by the old root-running image.
# Keep that development-only volume readable without changing its ownership.
FROM server AS server-local
USER root

FROM alpine:3.22 AS client
RUN apk add --no-cache ca-certificates
COPY --from=client-build /go/bin/mc /usr/local/bin/mc
ENTRYPOINT ["/usr/local/bin/mc"]
