## Client-Serveur

Ce projet est une application de chat client-serveur en C#, permettant à plusieurs clients de communiquer ensemble via un serveur central. Les messages sont stockés dans une base de données MongoDB Cloud (Atlas) pour permettre à chaque nouveau client d’accéder à l’historique des 20 derniers messages à l’aide de la commande `/historique`.



## Fonctionnalités principales

- **Connexion simultanée de plusieurs clients**  
  Plusieurs clients peuvent se connecter et échanger des messages en temps réel via le serveur.
- **Historique des messages**  
  Un client qui rejoint plus tard peut saisir `/historique` pour afficher les 20 derniers messages stockés sur MongoDB Atlas.
- **Persistance des messages**  
  Tous les messages échangés sont enregistrés dans la base de données MongoDB Cloud, assurant leur accessibilité même après redémarrage des applications.
- **Gestion des erreurs**  
  Gestion des erreurs de connexion, de transmission, et robustesse face aux déconnexions.

## Technologies utilisées

- **Langage** : C#
- **Réseau** : `System.Net.Sockets`, `System.Threading`
- **Base de données** : MongoDB (hébergée sur Atlas, via `MongoDB.Driver`)


## Installation

1. **Clonez le dépôt :**
   ```bash
   git clone https://github.com/LucasPorcheron/Client_Serveur.git
   ```

2. **Placez-vous dans le dossier du projet :**
   ```bash
   cd Client_Serveur
   ```

3. **Restaurez les dépendances :**
   ```bash
   dotnet restore
   ```

4. **Configurez votre chaîne de connexion MongoDB**  
   Renseignez l’URI MongoDB Atlas dans le code ou dans un fichier de config (ex : `appsettings.json`).


## Utilisation

### Lancer le serveur

```bash
cd Client_Serveur/Client_Serveur
dotnet run
```

### Lancer un client

```bash
cd Client/Client
dotnet run
```

> Lancez plusieurs clients pour tester la communication multi-utilisateurs.

### Commandes côté client

- `/historique` : Affiche les 20 derniers messages enregistrés dans MongoDB



## Auteur

- Lucas Porcheron



## Remarques

Ce projet offre une base solide pour un système de chat réseau multi-utilisateurs en C#. Il est facilement extensible pour intégrer des fonctionnalités comme l’authentification, la gestion de salons, etc.

Pour toute suggestion ou problème, utilisez les issues GitHub !