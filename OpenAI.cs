using System.Diagnostics;
using System;
using System.IO;

namespace Agora {
    public class OpenAI
    {
        /// <summary>
        /// The path to the "Main.py" file, for test purposes its in the "bin\Debug" folder
        /// </summary>
        public static string PythonScriptPath = @"main.py";

        public OpenAI()
        {

        }

        public string GPT(string prompt)
        {
            // On va créer un script Python temporaire
            // Puis placer du code à l'intérieur, enregistrer le fichier
            // Et le lancer
            string ScriptTemporaire = $@"main{prompt.GetHashCode()}.py";

            string pythonSrc = @"
import g4f

# Automatic selection of provider

prompt = """"""

%here%

""""""

# Set with provider
response = g4f.ChatCompletion.create(
    model=""gpt-3.5-turbo"",
    messages=[{""role"": ""user"", ""content"": prompt}]
)

for message in response:
    print(message)

";
            // On place le prompt à envoyer à GPT à sa place dans le code Python
            string pythonSrcNew = pythonSrc.Replace("%here%", prompt);

            // On enregistrer le fichier
            File.WriteAllText(ScriptTemporaire, pythonSrcNew);

            string output;
        ENCORE:
            // On génère une instance du procéssus de Python3
            Process process = new Process();
            process.StartInfo.FileName = "python"; // Sous-entend que Python est dans le PATH de l'OS
            process.StartInfo.Arguments = $"{ScriptTemporaire}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            // Peut prendre jusqu'à 1 minute ! ...
            var outputTask = process.StandardOutput.ReadToEnd();
            var errorTask = process.StandardError.ReadToEnd();

            // On récupère la réponse de GPT et on la néttoie un peu
            output = outputTask.Trim().Replace("\r\n", "");
            if (output != "")
            {
                // TODO : Gérer les problèmes
                string errorOutput = errorTask;

                if (output.Contains("bool, temperature: float)"))
                {
                    output = output.Split(new string[] { "bool, temperature: float)" }, StringSplitOptions.None)[1];
                }

                if (output.Contains("pip install -U g4f"))
                {
                    output = output.Split(new string[] { "pip install -U g4f" }, StringSplitOptions.None)[1];
                }

                output = NéttoyerUTF8(output);
            }
            else
            {
                goto ENCORE;
            }

            File.Delete(ScriptTemporaire);
            return output;
        }

        /// <summary>
        /// Une fonction pour néttoyer manuellement
        /// le message que renvoit les APIs de 
        /// g4free v2
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        private static string NéttoyerUTF8(string output)
        {
            output = output.Replace("Ú", "é");
            output = output.Replace("Þ", "è");
            output = output.Replace("Ó", "à");
            output = output.Replace("╔", "É");
            output = output.Replace("╩", "Ê");
            output = output.Replace("þ", "ç");
            output = output.Replace("¨", "ù");
            output = output.Replace("Ô", "â");
            output = output.Replace("└", "À");
            output = output.Replace("¯", "î");
            output = output.Replace("Û", "ê");
            output = output.Replace("¶", "ô");
            output = output.Replace("░", "");
            output = output.Replace("ñ", "¤");
            output = output.Replace("Ã", "Ç");
            output = output.Replace("¹", "û");
            output = output.Replace("£", "œ");

            return output;
        }

    }
}
