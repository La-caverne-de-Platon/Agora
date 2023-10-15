using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Agora {
    public class Discussion {

        /// <summary>
        /// La liste des auteurs de la conversation
        /// </summary>
        public List<Auteur> Auteurs;

        /// <summary>
        /// L'historique des messages de la conversation
        /// </summary>
        public List<Message> Messages;

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

        public Discussion()
        {
            Messages = new List<Message>();
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
                var str = File.ReadAllLines("convo.txt");
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

        /// <summary>
        /// Continuer la discussion
        /// </summary>
        internal void Continuer()
        {

            // On récupère un auteur au hasard
            Auteur current_Auteur = AuteurAuHasard();


            // Pour forcer la main au destin on change de sujet tous les 10 messages
            // Il n'y a qu'à vérifier si le n° du message actuel est un multiple de 10
            bool ChangerSujet = false;
            if (Messages.Count % 10 == 0)
                ChangerSujet = true;


            string prompt;
            // Si on ne doit pas changer de sujet
            if (!ChangerSujet)
            {
                // On répond normalement
                prompt = Répondre(current_Auteur);
            }
            else
            {
                // Sinon, on demande au site un sujet de dissertation
                string sujet = RécupérerSujet();
                prompt = RépondreEtChangerDeSujet(current_Auteur, sujet);
            }

            // On demander à GPT de générer la réponse
            OpenAI AI = new OpenAI();
            string réponseAuteur = AI.GPT(prompt);

            // Vu qu'on vient de faire parler un auteur
            // Alors cet auteur est le dernier auteur
            DernierAuteur = current_Auteur;

            // On corrige manuellement les soucis provoqués par GPT
            réponseAuteur = CorrigerPostGPT(current_Auteur, réponseAuteur);

            // On ajoute la réponse à la discussion
            Messages.Add(new Message(current_Auteur, $"({current_Auteur.nom}) {réponseAuteur}"));

            // Maintenant il faut envoyer le message au serveur
            // Discord avec les WebHooks
            réponseAuteur = EnvoyerWebHookDiscord(current_Auteur, réponseAuteur);

            // On sauvegarde la conversation dans le fichier convo.txt
            SauvegarderLaConversation();

        }

        private string EnvoyerWebHookDiscord(Auteur current_Auteur, string result)
        {
            string webhookUrl = current_Auteur.url;

            string arg = GenerateCurlCommand(result, webhookUrl);

            using (Process process = new Process())
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/c {arg}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                result = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                //
                Console.WriteLine(result);
            }

            return result;
        }

        /// <summary>
        /// Une méthode pour corriger manuellement les petits soucis
        /// parfois provoqués par GPT
        /// </summary>
        /// <param name="current_Auteur"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private static string CorrigerPostGPT(Auteur current_Auteur, string result)
        {
            char c = "'".ToCharArray()[0];
            result = result.Replace('"', c);
            result = result.Split('¤').Last();

            result = result.Replace(current_Auteur.nom, "ma personne");
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
        private string RépondreEtChangerDeSujet(Auteur current_Auteur, string sujet)
        {
            return $@"
    Tu incarnes {current_Auteur.nom}. Tu dois parler de lui uniquement à la première personne du singulier, comme un jeu de rôle.
    Tu dois répondre au philosophe {DernierAuteur.nom}. 
    Ne te présent pas, ne dis pas bonjour, ne fais pas de salutations !
    Ne sois pas polis, ne montre pas de marque de politesse !

    Tu écris à la première personne du singulier, tu dois argulenter uniquement selon les idées de la philosophie de {current_Auteur.nom} et tu dois rester en accord avec cette dernière.
    Tu dois synthétiser les arguments de {DernierAuteur.nom} ci dessous, les critiquer et proposer un contre argument pour démontrer que tu as raison. On doit ressentir une certaine personnalité en te lisant.
    À la fin de ton message tu dois poser une question pour ouvrir la conversation sur un nouveau sujet : '{sujet}'

    {DernierAuteur.nom} vient de dire : 
    ```
    {Messages.Last().contenu}
    ```

    Ton message doit faire MAXIMUM 100 mots. Ne répètes pas les mots du message de {DernierAuteur.nom} ! Utilises des autres formulations que lui. Ton message doit commencer par le nom du philosophe puis le symbole ¤.";
        }

        /// <summary>
        /// Une simple requête GET à un script php qui
        /// avec un certain paramètre codé à la main ici
        /// nous donne un sujet de dissertation au hasard
        /// </summary>
        /// <returns></returns>
        private static string RécupérerSujet()
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            var sujet = wc.DownloadString("https://lacavernedeplaton.fr/generateur-sujet.php?API=true");
            return sujet;
        }

        private string Répondre(Auteur current_Auteur)
        {
            return $@"
    Tu incarnes {current_Auteur.nom}. Tu dois parler de lui uniquement à la première personne du singulier, comme un jeu de rôle.
    Tu dois répondre au philosophe {DernierAuteur.nom}. 
    Ne te présent pas, ne dis pas bonjour, ne fais pas de salutations !
    Ne sois pas polis, ne montre pas de marque de politesse !

    Tu écris à la première personne du singulier, tu dois argulenter uniquement selon les idées de la philosophie de {current_Auteur.nom} et tu dois rester en accord avec cette dernière.
    Tu dois synthétiser les arguments de {DernierAuteur.nom} ci dessous en une seule phrase, si {current_Auteur.nom} n'est pas d'accord il faut les critiquer de façon virulente et proposer un contre argument pour démontrer que tu as raison. Puis tu dois reprendre la question qu'il te pose et proposer une réponse. On doit ressentir une certaine personnalité en te lisant.
    À la fin de ton message tu peux poser une question sur un point précis de la conversation pour continuer la conversation.

    {DernierAuteur.nom} vient de dire : 
    ```
    {Messages.Last().contenu}
    ```

    Ton message doit faire MAXIMUM 100 mots. Ne répètes pas les mots du message de {DernierAuteur.nom} ! Utilises des autres formulations que lui. Ton message doit commencer par le nom du philosophe puis le symbole ¤.";
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
                toSave = toSave + $"{msg.contenu}" + Environment.NewLine;
            }
            File.WriteAllText("convo.txt", toSave);
        }
    }
}