namespace Agora {
    public class Message {
        public Auteur auteur;
        public string contenu;
        public Message(Auteur auteur, string contenu)
        {
            this.auteur = auteur;
            this.contenu = contenu;
        }
    }
}
