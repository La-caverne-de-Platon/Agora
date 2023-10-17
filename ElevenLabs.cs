using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Agora {
    public partial class Discussion {
        public class ElevenLabs
        {
            public ElevenLabs()
            {

            }

            public bool Speak(Discussion discussion)
            {

                // récupérer le nombre de fichiers dans le dossier 'audio' pour le nom du prochain fichier


                string idFile = discussion.Messages.Count.ToString();

                string backup = @"
import sys
from elevenlabs_unleashed.manager import ELUAccountManager
from elevenlabs import generate, save, set_api_key, play, api
from elevenlabs_unleashed.tts import UnleashedTTS

def text_to_speech(input_string):
    audio = generate(
            text=input_string,
            voice=""VOIX"",
            model=""MODELE""
        )
    save(audio, ""001.wav"")
    print(""done"")

if __name__ == ""__main__"":
        input_string = ""HERE""
        tts = UnleashedTTS(nb_accounts=1, create_accounts_threads=0)
        text_to_speech(input_string)
";

                string textToSpeak = $"{discussion.Messages.Last().contenu}";
                if (textToSpeak.Contains(")"))
                    textToSpeak = textToSpeak.Split(')')[1].Trim();
                string py_content = backup.Replace("HERE", textToSpeak);
                py_content = py_content.Replace("VOIX", discussion.Messages.Last().auteur.voix.narrateur);
                py_content = py_content.Replace("MODELE", discussion.Messages.Last().auteur.voix.modèle);
                string py_name = @"C:\Users\Shadow\Documents\eleveb\src\elevenlabs_unleashed\" + "main" + py_content.GetHashCode().ToString() + ".py";
                File.WriteAllText(py_name, py_content);

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "\"C:\\Users\\Shadow\\AppData\\Local\\Programs\\Python\\Python311\\python.exe\"",
                    Arguments = $"{py_name}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process process = new Process { StartInfo = startInfo };

                process.Start();

                string output = process.StandardOutput.ReadToEnd(); 
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (File.Exists(@"001.wav"))
                {
                    File.Copy(@"001.wav", $"audios/{idFile}.wav");
                    File.Delete(@"001.wav");
                    return true;
                }
                else
                    return false;
            }
        }
    }
}