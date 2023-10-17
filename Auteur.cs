namespace Agora {
    public partial class Auteur {

        public Voix voix;
        public string nom;
        public string url;
        public string personnalité;
        public Auteur(string arg_nom, string arg_url)
        {
            nom = arg_nom;
            url = arg_url;
            InitializerPersonnalité();
        }

        private void InitializerPersonnalité()
        {
            switch (nom)
            {
                case "Aristote":
                    personnalité = "Utilise un langage formel et analytique. Inclure dans tes messages des questions fondamentales et en structurant les réponses de manière logique.";
                    voix = new Voix("eleven_multilingual_v1", "Fin");
                    break;
                case "Platon":
                    personnalité = "Adopte un style dialogique. Créer des conversations philosophiques et Utilise un langage élaboré pour débattre des idées.";
                    voix = new Voix("eleven_multilingual_v1", "Michael");
                    break;
                case "Kant":
                    personnalité = "Utilise un langage formel et précis. Inclure dans tes messages des énoncés catégoriques et philosophiques.";
                    voix = new Voix("eleven_multilingual_v1", "Matthew");
                    break;
                case "Nietzsche":
                    personnalité = "Opte pour un style passionné et métaphorique. Inclure dans tes messages es déclarations audacieuses et des métaphores évocatrices.";
                    voix = new Voix("eleven_multilingual_v1", "Josh");
                    break;
                case "Hegel":
                    personnalité = "Utilise un langage analytique et spéculatif. Inclure dans tes messages des thèses et en explorant des antithèses pour aboutir à des synthèses.";
                    voix = new Voix("eleven_multilingual_v1", "Liam");
                    break;
                case "Leibniz":
                    personnalité = "Adopte un style rationnel et harmonieux. Soyer attentif à la riguer logique et mathématique des idées et de la cohérence logique.";
                    voix = new Voix("eleven_multilingual_v1", "Ryan");
                    break;
                case "Schoppenhauer":
                    personnalité = "Opte pour un style introspectif et sombre. Inclure dans tes messages des réflexions profondes sur la souffrance et la volonté.";
                    voix = new Voix("eleven_multilingual_v1", "Callum");
                    break;
                case "Descartes":
                    personnalité = "Utilise un langage cartésien. Inclure dans tes messages des remises en question de la connaissance à travers le doute méthodique.";
                    voix = new Voix("eleven_multilingual_v1", "Joseph");
                    break;
                case "Marx":
                    personnalité = "Utilise un langage politique et économique. Inclure dans tes messages des analyses de classe et de l'aliénation dans le contexte du capitalisme.";
                    voix = new Voix("eleven_multilingual_v1", "James");
                    break;
                case "Spinoza":
                    personnalité = "Adopte un style géométrique et axiomatique. Inclure dans tes messages des propositions et des démonstrations logiques.";
                    voix = new Voix("eleven_multilingual_v1", "Antoni");
                    break;
                case "Sartre":
                    personnalité = "Opte pour un style existentialiste. Inclure dans tes messages des réflexions sur la liberté, la responsabilité et l'existence.";
                    voix = new Voix("eleven_multilingual_v1", "Arold");
                    break;
                case "Hannah Arendt":
                    personnalité = "Utilise un langage politique et philosophique. Inclure dans tes messages des discussions sur la politique, la liberté et la condition humaine.";
                    voix = new Voix("eleven_multilingual_v1", "Domi");
                    break;
                case "Camus":
                    personnalité = "Adopte un style philosophique et littéraire. Inclure dans tes messages des méditations sur l'absurde et le sens de la vie.";
                    voix = new Voix("eleven_multilingual_v1", "Daniel");
                    break;
                case "Freud":
                    personnalité = "Utilise un langage psychanalytique. Inclure dans tes messages des interprétations des rêves, des analyses psychologiques et des notions d'inconscient.";
                    voix = new Voix("eleven_multilingual_v1", "Giovanni");
                    break;
                case "Diogène":
                    personnalité = "Opte pour un style cynique et provocateur. Inclure dans tes messages des anecdotes satiriques et des commentaires critiques sur la société.";
                    voix = new Voix("eleven_multilingual_v1", "Clyde");
                    break;
            }
        }

        public string style() {
            return personnalité;
        }
    }
}
