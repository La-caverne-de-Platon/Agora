using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Agora {
    public partial class Discussion {

        /// <summary>
        /// La liste des auteurs de la conversation
        /// </summary>
        public List<Auteur> Auteurs;

        /// <summary>
        /// L'historique des messages de la conversation
        /// </summary>
        public List<Message> Messages;

        /// <summary>
        /// Le sujet de conversation actuel;
        /// </summary>
        public string Sujet;

        /// <summary>
        /// Une instance de Random pour générer des entiers aléatoires
        /// </summary>
        public static Random random = new Random();

        /// <summary>
        /// Le dernier auteur ayant participé à la conversation
        /// (on le récupère pour éviter qu'un même auteur parle
        /// deux fois de suite)
        /// </summary>
        public static Auteur DernierAuteur;

        /// <summary>
        /// L'auteur du message que le programme est en train de traiter
        /// </summary>
        public static Auteur CurrentAuteur;

        /// <summary>
        /// Pour activer ou non la génération du fichier audio
        /// </summary>
        public bool GénérerAudio = false;

        public bool EnvoyerSurDiscord = true;

        public bool MettreAJourLeFichierConversation = true;


        public Discussion()
        {
            Messages = new List<Message>();
            Auteurs = new List<Auteur>();
            random = new Random();

        }

        /// <summary>
        /// Récupérer la liste des auteurs de la discussion
        /// à partir du fichier auteurs.txt
        /// </summary>
        /// <param name="auteurs"></param>
        public void InitialiserAuteurs()
        {

            if (File.Exists("auteurs.txt"))
            {
                var str = File.ReadAllLines("auteurs.txt");
                foreach (var line in str)
                {
                    var auteur = line.Split('¤')[0];
                    var url = line.Split('¤')[1];
                    Auteurs.Add(new Auteur(auteur, url));
                }

            }
        }

        /// <summary>
        /// Récupérer le contenu de la conversation
        /// depuis le fichier "convo.txt"
        /// </summary>
        public void InitializerMessages()
        {
            if (File.Exists("convo.txt"))
            {
                var str = File.ReadAllLines("convo.txt");
                foreach (var line in str)
                {
                    foreach (var perso in Auteurs)
                    {
                        if (line.Trim().StartsWith($"({perso.nom.Trim()})"))
                            Messages.Add(new Message(perso, line.Trim().Replace($"({perso.nom.Trim()})", "")));
                    }
                }

            }
        }

        public static string FirstLetterToUpper(string str)
        {
            if (str == null)
                return null;

            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1);

            return str.ToUpper();
        }

        /// <summary>
        /// Continuer la discussion
        /// </summary>
        internal void Continuer()
        {

            string typo = "";

            typo = TypoAuHasard();

           

            /*
             réponse directe au précédent
             réponse qui continue le message précédent
             réponse qui s'immisce dans la conversation
             réponse qui vient changer de sujet
             */


            string prompt;
            switch (typo) // On ajoute un nouvel auteur dans la discussion
            {
                case "rep_immisce":
                    // On récupère un auteur au hasard
                    CurrentAuteur = AuteurAuHasard();
                    DernierAuteur = Messages.Last().auteur;
                    prompt = InsertionNouvellePersonne(CurrentAuteur);
                    break;
                case "rep_continue":
                    CurrentAuteur = Messages.Last().auteur;
                    DernierAuteur = Messages.Last().auteur;
                    prompt = ContinuerMessagePrécédent();
                    break;
                case "rep_change":
                    // On récupère un auteur au hasard
                    CurrentAuteur = AuteurAuHasard();
                    DernierAuteur = Messages[Messages.Count - 1].auteur;
                    prompt = RépondreEtChangerDeSujet(CurrentAuteur);
                    break;
                case "rep_direct":
                    CurrentAuteur = Messages[Messages.Count -2].auteur;
                    DernierAuteur = Messages[Messages.Count - 1].auteur;
                    prompt = RépondreDirectement();
                    break;
                default:
                    CurrentAuteur = Messages.Last().auteur;
                    DernierAuteur = Messages.Last().auteur;
                    prompt = ContinuerMessagePrécédent();
                    break;
            }

            // On demander à GPT de générer la réponse
            OpenAI AI = new OpenAI();
            string réponseAuteur = AI.GPT(prompt);       //TODO : mettre corriger dans cette classe là-bas     

            // On corrige manuellement les soucis provoqués par GPT
            réponseAuteur = CorrigerPostGPT(CurrentAuteur, réponseAuteur);

            Console.WriteLine(typo);
            Console.WriteLine(Sujet);

            if(CurrentAuteur == DernierAuteur)
                //réponseAuteur = réponseAuteur.Substring(réponseAuteur.IndexOf(".") + 1).Trim();
            // Quelque chose s'est mal passé et on va l'oublier :)
            if (réponseAuteur.Length < 5)
                return;

            réponseAuteur = FirstLetterToUpper(réponseAuteur);

            // Vu qu'on vient de faire parler un auteur
            // Alors cet auteur est le dernier auteur
            DernierAuteur = CurrentAuteur;

            // On ajoute la réponse à la discussion
            Messages.Add(new Message(CurrentAuteur, $"({CurrentAuteur.nom}) {réponseAuteur}"));

            //On affiche la réponse ici pour l'UI
            Console.WriteLine($"[{CurrentAuteur.nom}] {réponseAuteur}");



            if (GénérerAudio)
            {
                // On génère l'audio de la réponse
                ElevenLabs elevenLabs = new ElevenLabs();
                if (!elevenLabs.Speak(this))
                {
                    Console.WriteLine("Error ElevenLabs !");
                }
                else
                {
                    Console.Write("... audio generated !" + Environment.NewLine);


                }

            }

            if (MettreAJourLeFichierConversation)
                SauvegarderLaConversation();

            if (EnvoyerSurDiscord)
                EnvoyerWebHookDiscord(CurrentAuteur, réponseAuteur);



        }

        public string TypoAuHasard()
        {
            string typo;
            bool rep_directe = Chance(40);
            bool rep_continue = Chance(40);
            bool rep_immisce = Chance(20);
            bool rep_change = Chance(20);

            List<string> b = new List<string>();
            if (rep_directe)
            {
                b.Add("rep_direct");
            }
            else
            {
                if (rep_continue)
                    b.Add("rep_continue");
                if (rep_immisce)
                    b.Add("rep_immisce");
                if (rep_change)
                    b.Add("rep_change");
            }
            if (b.Count == 0)
            {
                List<string> c = new List<string>
                {
                    "rep_direct",
                    "rep_continue",
                    "rep_immisce",
                    "rep_change",
                    "rep_continue"
                };

                b.Add(c[GetRandomInt(0, 4)]);
            }
            
            typo = Outils.TypologieAuHasard(b);

            if (typo == "rep_direct" && Messages[Messages.Count - 1].auteur == Messages[Messages.Count - 2].auteur)
                typo = "rep_immisce";
            return typo;
        }

        private string RépondreDirectement()
        {
            return $@"
Contexte : Tu es une IA qui joue à un jeu de rôle. Tu joues le rôle de {Messages[Messages.Count -2].auteur.nom} dans une conversation.
Personnalité de {Messages[Messages.Count - 2].auteur.nom} : {Messages[Messages.Count - 2].auteur.style()}.
Philosophie de {Messages[Messages.Count - 2].auteur.nom} : {Messages[Messages.Count - 2].auteur.philosophie}

{Sujet}

Historique de la conversation :
```
{Messages[Messages.Count - 3].contenu}
{Messages[Messages.Count - 2].contenu}
{Messages[Messages.Count - 1].contenu}
```

Tu répond à {Messages.Last().auteur.nom}. Si tu n'est pas d'accord avec lui tu dois le lui faire savoir de façon nette : insiste sur les erreures logiques, utilises de la réthorique. Sinon, tu dois développer davantage ses arguments en les synthétisant et en développant le point de vue de la philosophie de {Messages[Messages.Count - 2].auteur.nom}.

Pas de politesse, tu le tutois, ce n'est pas ton ami pour autant. Tu lui répond directement, addresse toi à lui, alterque-le. Dynamise la conversation ! Mets de l'humeur !! On doit sentir ta personnalité forte en te lisant.

Tu dois toujours parler de {Messages[Messages.Count - 2].auteur.nom} à la première personne.

Ta réponse doit commencer par le nom du philosophe puis le symbole ¤. Tu dois écrire en français. Max 100 mots.";
        }

        private string ContinuerMessagePrécédent()
        {
            return $@"
Contexte : Tu es une IA qui joue à un jeu de rôle. Tu joues le rôle de {Messages.Last().auteur.nom} dans une conversation. 
Personnalité de {Messages.Last().auteur.nom} : {Messages.Last().auteur.style()}.
Philosophie de {Messages.Last().auteur.nom} : {Messages.Last().auteur.philosophie}

{Sujet}

Tu dois écrire la suite de ton message : 

```
{Messages[Messages.Count - 1].contenu}
```

Tu dois développer d'autres idées, analyser d'autres possibilités, et développer tes arguments. Tu dois appliquer ta philosophie.

Mets de l'humeur !! On doit sentir ta personnalité forte en te lisant.

Ne te répète surtout pas. Sois original.

Tu dois toujours parler de {Messages.Last().auteur.nom} à la première personne du singulier !

Ta réponse doit commencer par le symbole ¤. Tu dois écrire en français. Max 80 mots.";
        }

        



        private void EnvoyerWebHookDiscord(Auteur current_Auteur, string result)
        {
            string webhookUrl = current_Auteur.url;


            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var jsonContent = new
                    {
                        content = result,
                        embeds = new object[0],
                        attachments = new object[0]
                    };

                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(jsonContent);

                    StringContent httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = client.PostAsync(webhookUrl, httpContent).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Message posted successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode.ToString()}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }


        /// <summary>
        /// Une méthode pour corriger manuellement les petits soucis
        /// parfois provoqués par GPT
        /// </summary>
        /// <param name="current_Auteur"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static string CorrigerPostGPT(Auteur current_Auteur, string result)
        {
            char c = "'".ToCharArray()[0];
            result = result.Replace('"', c);
            result = result.Replace("¤¤", "¤");

            if(Regex.Matches(result, "¤").Count > 1)
            {
                result = result.Split('¤').Last();
            }
           

            result = result.Replace(current_Auteur.nom, "ma personne");
            result = result.Replace("Salut les gars !", "");

            //Mon cher Descartes
            if (result.Contains($"Mon Cher {DernierAuteur.nom},"))
                result = FirstLetterToUpper(result.Replace($"Mon Cher {DernierAuteur.nom},", "").Trim());
            
            if (result.Contains($"ma personne : "))
                result = FirstLetterToUpper(result.Replace($"ma personne : ", "").Trim());
            if (result.Contains($"Cependant,"))
                result = FirstLetterToUpper(result.Replace($"Cependant,", "Mais ").Trim());
            if (result.Contains($"En résumé,"))
                result = FirstLetterToUpper(result.Replace($"En résumé,", "Donc ").Trim());
            if (result.Contains($"Ma personne :"))
                result = FirstLetterToUpper(result.Replace($"Ma personne :", "").Trim());
            if (result.Contains($"Ma personne"))
                result = FirstLetterToUpper(result.Replace($"Ma personne", "").Trim());
            if (result.Contains($"(ma personne)"))
                result = FirstLetterToUpper(result.Replace($"(ma personne)", "").Trim());
            if (result.Contains($"ma personne"))
                result = FirstLetterToUpper(result.Replace($"ma personne", "").Trim());
            //essaie pour zapper TOUTES les politesses nulles au début des réponses :
            // TODO : trouver un meilleur moyen ?


            //remplacer les tournures redondentes ...
            result = result.Replace("Selon moi, ", "Il me semble quand-même que ");


            result = result.Replace(DernierAuteur.nom, DernierAuteur.discordID);

            //result = result.Split('.')[1];

            result = result.Replace("¤", "");
            result = FirstLetterToUpper(result.Trim());
            return result;
        }


        /// <summary>
        /// Répondre à la discussion et changer de sujet
        /// au hasard
        /// <see cref="RécupérerSujet"/>
        /// </summary>
        /// <param name="current_Auteur"></param>
        /// <param name="sujet"></param>
        /// <returns></returns>
        private string RépondreEtChangerDeSujet(Auteur current_Auteur)
        {
            RécupérerSujet();
            return $@"
    Tu es en plein dans une partie de jeu de rôle.
    Tu incarnes le philosophe {current_Auteur.nom} et ses idées.
    Tu dois tout faire comme si tu étais lui.

    Tu dois participer à la discussion en cours. Sois inventif, éloquant et imaginatif.
    Ne te présent pas, ne dis pas bonjour, ne fais pas de salutations !
    Tu tutoies les autres personnes de la conversation.
    Tu n'utilise pas un langage soutenu, tu dois utiliser un langage informel.
    Ne dis pas 'Cher', n'utilise pas de formule de politesse.
    Parles comme un jeune adulte de 20 ans.

    Le dernier message de la conversation est de {DernierAuteur.nom}, il vient de dire : 
    ```
    {Messages.Last().contenu}
    ```

    Ta réponse va devoir permettre à la conversation de changer son sujet actuel en : {Sujet}
    Ta réponse doit faire MAXIMUM 100 mots. Ta réponse doit commencer par le nom du philosophe puis le symbole ¤. Tu dois écrire en français.";
        }

        /// <summary>
        /// Une simple requête GET à un script php qui
        /// avec un certain paramètre codé à la main ici
        /// nous donne un sujet de dissertation au hasard
        /// </summary>
        /// <returns></returns>
        private void RécupérerSujet()
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            Sujet = "Le fil conducteur de la conversation est : " + wc.DownloadString("https://lacavernedeplaton.fr/generateur-sujet.php?API=true");
        }

        /// <summary>
        /// Répondre à la discussion en cours
        /// </summary>
        /// <param name="current_Auteur"></param>
        /// <returns></returns>
        private string InsertionNouvellePersonne(Auteur current_Auteur)
        {
            return $@"
Contexte : Tu es une IA qui joue à un jeu de rôle. Tu joues le rôle de {current_Auteur.nom} dans une conversation. 
Personnalité de {current_Auteur.nom} : {current_Auteur.style()}.
Philosophie de {current_Auteur.nom} : {current_Auteur.philosophie}
{Sujet}

Historique de la conversation :
```
{Messages[Messages.Count - 3].contenu}
{Messages[Messages.Count - 2].contenu}
{Messages[Messages.Count -1].contenu}
```

Tu répond à {Messages[Messages.Count - 1].auteur.nom}. Si tu n'est pas d'accord avec lui tu dois le lui faire savoir de façon nette : insiste sur les erreures logiques, utilises de la réthorique. Sinon, tu dois développer davantage ses arguments en les synthétisant et en développant le point de vue de la philosophie de {current_Auteur.nom}.

Pas de politesse, tu le tutois, ce n'est pas ton ami pour autant. Tu lui répond directement, addresse toi à lui, alterque-le. Dynamise la conversation ! Mets de l'humeur !! On doit sentir ta personnalité forte en te lisant.

Tu dois toujours parler de {current_Auteur.nom} à la première personne du singulier.

Ne te présente pas, ne dis pas bonjour.

Ta réponse doit commencer par le nom du philosophe puis le symbole ¤. Tu dois écrire en français. Max 100 mots.";
        }

        /// <summary>
        /// Renvoit un auteur au hasard dans la liste des auteurs
        /// <see cref="Auteurs"/>
        /// Important : il ne doit pas être le même que le précédent
        /// <see cref="DernierAuteur"/>
        /// </summary>
        /// <returns></returns>
        private Auteur AuteurAuHasard()
        {
            // Use the random index to retrieve the random item
            var current_Auteur = Auteurs[random.Next(0, Auteurs.Count)];

            DernierAuteur = Messages.Last().auteur;
            if (DernierAuteur != null)
            {
                while (current_Auteur.nom == DernierAuteur.nom)
                {
                    current_Auteur = Auteurs[random.Next(0, Auteurs.Count)];
                }
            }

            return current_Auteur;
        }

        /// <summary>
        /// Générer une commande CURL bien formée
        /// pour les WebHooks Discord
        /// </summary>
        /// <param name="message"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GenerateCurlCommand(string message, string url)
        {
            // Crée la commande cURL avec les paramètres fournis
            string curlCommand = $"curl -X POST -H \"Content-Type: application/json\" -d \"{{\\\"content\\\":\\\"{message}\\\",\\\"embeds\\\":[],\\\"attachments\\\":[]}}\" \"{url}\"";

            return curlCommand;
        }

        /// <summary>
        /// Enregistrer la conversation dans un fichier
        /// </summary>
        public void SauvegarderLaConversation()
        {
            string toSave = "";
            foreach (var msg in Messages)
            {
              toSave = toSave + $"({msg.auteur.nom})¤{msg.contenu}" + Environment.NewLine;
            }

            File.WriteAllText("convo.txt", toSave);
            File.WriteAllText($"txt/{Messages.Count}.txt", $"({Messages.Last().auteur.nom})¤ {Messages.Last().contenu}");
        }

        /// <summary>
        /// Renvoit 'true' avec une probabilité indiquée
        /// </summary>
        /// <param name="probability"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static bool Chance(int probability)
        {
            if (probability < 0 || probability > 100)
            {
                throw new ArgumentException("La probabilité doit être comprise entre 0 et 100.");
            }

            int randomNumber = randomGenerator.Value.Next(0, 100); // Generate a random number for the current thread.

            return randomNumber < probability;
        }

        private static ThreadLocal<Random> randomGenerator = new ThreadLocal<Random>(() =>
        {
            return new Random(Guid.NewGuid().GetHashCode());
        });

        public static int GetRandomInt(int min, int max)
        {
            if (min > max)
            {
                throw new ArgumentException("min should be less than or equal to max.");
            }

            return randomGenerator.Value.Next(min, max + 1); // Generate a random integer within the specified range.
        }

    }
}