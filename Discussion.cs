using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

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

            // On récupère un auteur au hasard
            Auteur current_Auteur = AuteurAuHasard();


            // Pour forcer la main au destin on change de sujet tous les 10 messages
            // Il n'y a qu'à vérifier si le n° du message actuel est un multiple de 10
            bool ChangerSujet = false;
            if (Messages.Count % 10 == 0)
                ChangerSujet = true;

            // Vérifier si au moins un des six derniers messages a été écrit par current_Auteur
            for (int i = Messages.Count - 1; i >= Math.Max(0, Messages.Count - 6); i--)
            {
                if (Messages[i].auteur == current_Auteur)
                {
                    ChangerSujet = true;
                    break; // Sortez de la boucle dès que vous trouvez un message de current_Auteur
                }
            }

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
                RécupérerSujet();
                prompt = RépondreEtChangerDeSujet(current_Auteur);
            }

            // On demander à GPT de générer la réponse
            OpenAI AI = new OpenAI();
            string réponseAuteur = AI.GPT(prompt);           

            // On corrige manuellement les soucis provoqués par GPT
            réponseAuteur = CorrigerPostGPT(current_Auteur, réponseAuteur);

            // Quelque chose s'est mal passé et on va l'oublier :)
            if (réponseAuteur.Length < 5)
                return;

            // Vu qu'on vient de faire parler un auteur
            // Alors cet auteur est le dernier auteur
            DernierAuteur = current_Auteur;

            // On ajoute la réponse à la discussion
            Messages.Add(new Message(current_Auteur, $"({current_Auteur.nom}) {réponseAuteur}"));

            //On affiche la réponse ici pour l'UI
            Console.WriteLine($"[{current_Auteur.nom}] {réponseAuteur}");


            // Maintenant il faut envoyer le message au serveur
            // Discord avec les WebHooks
            réponseAuteur = EnvoyerWebHookDiscord(current_Auteur, réponseAuteur);

            

            // On génère l'audio de la réponse
            ElevenLabs elevenLabs = new ElevenLabs();
            if (!elevenLabs.Speak(this))
            {
                Console.WriteLine("Error ElevenLabs !");
            }
            else {
                // On sauvegarde la conversation dans le fichier convo.txt
                SauvegarderLaConversation();
                Console.Write("... audio generated !" + Environment.NewLine);
            }

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
            result = result.Replace("¤¤", "¤");

            if(Regex.Matches(result, "¤").Count > 1)
            {
                result = result.Split('¤').Last();
            }
           

            result = result.Replace(current_Auteur.nom, "ma personne");

            if (result.Contains($"Cher {DernierAuteur.nom},"))
                result = FirstLetterToUpper(result.Replace($"Cher {DernierAuteur.nom},", "").Trim());
            if (result.Contains($"cher {DernierAuteur.nom},"))
                result = FirstLetterToUpper(result.Replace($"cher {DernierAuteur.nom},", "").Trim());
            if (result.Contains($"{DernierAuteur.nom},"))
                result = FirstLetterToUpper(result.Replace($"{DernierAuteur.nom},", "").Trim());
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
            Sujet = "Le sujet fil conducteur de la conversation est : " + wc.DownloadString("https://lacavernedeplaton.fr/generateur-sujet.php?API=true");
        }

        /// <summary>
        /// Répondre à la discussion en cours
        /// </summary>
        /// <param name="current_Auteur"></param>
        /// <returns></returns>
        private string Répondre(Auteur current_Auteur)
        {
            return $@"
Tu es une IA qui joue à un jeu de rôle. Tu dois immiter {current_Auteur.nom}. Personnalité de {current_Auteur.nom} : {current_Auteur.style()}.

{Sujet}

Historique de la conversation :
```
{Messages[Messages.Count - 3].contenu}
{Messages[Messages.Count - 2].contenu}
{Messages[Messages.Count -1].contenu}
```

Tu répond à {Messages[Messages.Count - 1].auteur.nom}. Si tu n'est pas d'accord avec lui tu dois le lui faire savoi.

Pas de politesse, tu le tutois, ce n'est pas ton ami pour autant. Tu lui répond directement, addresse toi à lui, alterque-le. Dynamise la conversation ! Mets de l'humeur !!

Ta réponse doit commencer par le nom du philosophe puis le symbole ¤. Tu dois écrire en français. Max 80 mots.";
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
    }
}