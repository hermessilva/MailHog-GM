package api

import (
	gohttp "net/http"
	"sort"

	"github.com/gorilla/pat"
	"github.com/mailhog/MailHog-Server/config"
	"github.com/mailhog/data"
)

// sortMessagesDesc ordena as mensagens da mais nova para a mais antiga
// (por Created, decrescente). Garante o comportamento "mais novo primeiro"
// independente do backend de armazenamento (memory, mongodb ou maildir).
func sortMessagesDesc(messages []data.Message) {
	sort.SliceStable(messages, func(i, j int) bool {
		return messages[i].Created.After(messages[j].Created)
	})
}

func CreateAPI(conf *config.Config, r gohttp.Handler) {
	apiv1 := createAPIv1(conf, r.(*pat.Router))
	apiv2 := createAPIv2(conf, r.(*pat.Router))

	go func() {
		for {
			select {
			case msg := <-conf.MessageChan:
				apiv1.messageChan <- msg
				apiv2.messageChan <- msg
			}
		}
	}()
}
