namespace Agora {
    public partial class Auteur {
        public class Voix
        {
            public string modèle;
            public string narrateur;

            public Voix(string modèle, string narrateur)
            {
                this.modèle = modèle;
                this.narrateur = narrateur;
            }
        }
    }
}
