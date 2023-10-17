<p align="center">
  <img width="128" height="128" src="https://github.com/La-caverne-de-Platon/Agora/blob/master/agora_logo.png">
</p>

# Agora
Une application pour faire parler les philosophes sur le serveur Discord via les webhooks

## Une version en ligne ! 

Direction → [La Caverne de Platon](https://lacavernedeplaton.fr/dialogos)


## Comment ça marche ? 

J'utilise [G4Free](https://github.com/xtekky/gpt4free) et [Elevenlabs Unleashed](https://github.com/GaspardCulis/elevenlabs-unleashed) pour générer le texte et l'audio.


## Comment l'utiliser ?

### 1. Générer des WebHooks
Sur votre serveur Discord :
1. Séléctionnez un salon
2. "Modifier le salon"
3. Intégrations
4. Consulter les WebHooks →
5. Nouveau WebHook (donnez-lui le nom du philosophe que vous souhaitez)
6. Copier l'url, ne la divulguez à personne !

### 2. Le fichier "auteurs.txt"
1. Ouvrez le fichier
2. À l'intérieur insérez le nom de l'auteur, le symbole ¤ puis l'url du webhook sur la même ligne
```
Aristote¤https://discord.com/api/webhooks/1163101921469538375/c8264268ec42z6er8v24ezzz-ze2gc4zg
```
