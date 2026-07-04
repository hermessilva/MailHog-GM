#
# MailHog-GM Dockerfile
#
# Compila o fork a partir do código-fonte LOCAL (incluindo as alterações
# vendorizadas), diferente do MailHog original que era baixado via `go install`.
#

FROM golang:1.18-alpine AS builder

# GOPATH mode: não há go.mod; as dependências vêm de ./vendor.
ENV GO111MODULE=off
ENV GOPATH=/go
ENV CGO_ENABLED=0

RUN apk --no-cache add git

# O import path do projeto é github.com/mailhog/MailHog.
WORKDIR /go/src/github.com/mailhog/MailHog
COPY . .

# GOPATH mode usa o diretório vendor/ automaticamente.
RUN go build -o /MailHog .

# ---------------------------------------------------------------------------

FROM alpine:3

# Usuário/grupo mailhog com uid/gid 1000 (workaround boot2docker #581).
RUN adduser -D -u 1000 mailhog

COPY --from=builder /MailHog /usr/local/bin/MailHog

USER mailhog
WORKDIR /home/mailhog

ENTRYPOINT ["MailHog"]

# SMTP, SMTPS e HTTP (UI/API).
EXPOSE 1025 1465 8025
