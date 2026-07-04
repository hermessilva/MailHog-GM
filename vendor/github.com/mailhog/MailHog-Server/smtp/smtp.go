package smtp

import (
	"crypto/tls"
	"io"
	"log"
	"net"

	"github.com/mailhog/MailHog-Server/config"
)

func Listen(cfg *config.Config, exitCh chan int) *net.TCPListener {
	log.Printf("[SMTP] Binding to address: %s\n", cfg.SMTPBindAddr)
	ln, err := net.Listen("tcp", cfg.SMTPBindAddr)
	if err != nil {
		log.Fatalf("[SMTP] Error listening on socket: %s\n", err)
	}
	defer ln.Close()

	for {
		conn, err := ln.Accept()
		if err != nil {
			log.Printf("[SMTP] Error accepting connection: %s\n", err)
			continue
		}

		if cfg.Monkey != nil {
			ok := cfg.Monkey.Accept(conn)
			if !ok {
				conn.Close()
				continue
			}
		}

		go Accept(
			conn.(*net.TCPConn).RemoteAddr().String(),
			io.ReadWriteCloser(conn),
			cfg.Storage,
			cfg.MessageChan,
			cfg.Hostname,
			cfg.Monkey,
			cfg,
		)
	}
}

// ListenTLS liga um listener SMTPS (TLS implícito, ex.: porta 465), onde a
// conexão já é criptografada antes de qualquer comando SMTP — como o Gmail em
// smtp.gmail.com:465.
func ListenTLS(cfg *config.Config, exitCh chan int) net.Listener {
	if cfg.TLSConfig == nil {
		log.Fatalf("[SMTPS] TLS config not initialised")
	}

	log.Printf("[SMTPS] Binding to address: %s\n", cfg.SMTPSBindAddr)
	inner, err := net.Listen("tcp", cfg.SMTPSBindAddr)
	if err != nil {
		log.Fatalf("[SMTPS] Error listening on socket: %s\n", err)
	}

	ln := tls.NewListener(inner, cfg.TLSConfig)
	defer ln.Close()

	for {
		conn, err := ln.Accept()
		if err != nil {
			log.Printf("[SMTPS] Error accepting connection: %s\n", err)
			continue
		}

		if cfg.Monkey != nil {
			ok := cfg.Monkey.Accept(conn)
			if !ok {
				conn.Close()
				continue
			}
		}

		go AcceptTLS(
			conn.RemoteAddr().String(),
			io.ReadWriteCloser(conn),
			cfg.Storage,
			cfg.MessageChan,
			cfg.Hostname,
			cfg.Monkey,
			cfg,
		)
	}
}
