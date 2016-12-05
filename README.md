# gwezboell-fonctionnel

BACK-END :
	- ATTENTION : l'exécutable du back-end a besoin des droits d'admin pour fonctionner correctement. Lancez donc Visual Studio en tant qu'admin pour pouvoir l'exécuter depuis le debug.
		Si vous utilisez l'exécutable directement, pensez également à l'exécuter en mode admin.
	- Init : 
		* Créer un projet Visual Studio (Console F#)
		* Ajouter les références manquantes : 
			clic droit références -> ajouter une référence -> chercher et ajouter System.ServiceModel et System.ServiceModel.Web
	- API : Préférez Postman au navigateur web pour tester les endpoint
		* Méthodes GET
		* Créer partie :
			-> http://{serveur}:{port}/{nickname}/{couleur}/
				-> couleur = "blanc" ou "noir"
				-> exemple : http://localhost:64385/Connard/noir
				-> le second appel attribuera la couleur automatiquement
		* Jouer coup : (Non fonctionnel pour le moment)
			-> http://{serveur}:{port}/{nickname}/{couleur}/{x1}:{y1}:{x2}:{y2}
				-> x1 et y1 correspondent à la position de la pièce à déplacer
				-> x2 et y2 correspondent à la position de la pièce à atteindre (contrôles de possibilité du coup traités lors du post)
				-> exemple : http://localhost:64385/Connard/noir/1:1:1:3
	- Structure de données :
		* exemple en début de partie : 	
			Ligne[CaseVide;CaseVide;CaseVide;Tour:Noir;Tour:Noir;Tour:Noir;CaseVide;CaseVide;CaseVide;]
			Ligne[CaseVide;CaseVide;CaseVide;CaseVide;Tour:Noir;CaseVide;CaseVide;CaseVide;CaseVide;]
			Ligne[CaseVide;CaseVide;CaseVide;CaseVide;Tour:Blanc;CaseVide;CaseVide;CaseVide;CaseVide;]
			Ligne[Tour:Noir;CaseVide;CaseVide;CaseVide;Tour:Blanc;CaseVide;CaseVide;CaseVide;Tour:Noir;]
			Ligne[Tour:Noir;Tour:Noir;Tour:Blanc;Tour:Blanc;Roi:Blanc;Tour:Blanc;Tour:Blanc;Tour:Noir;Tour:Noir;]
			Ligne[Tour:Noir;CaseVide;CaseVide;CaseVide;Tour:Blanc;CaseVide;CaseVide;CaseVide;Tour:Noir;]
			Ligne[CaseVide;CaseVide;CaseVide;CaseVide;Tour:Blanc;CaseVide;CaseVide;CaseVide;CaseVide;]
			Ligne[CaseVide;CaseVide;CaseVide;CaseVide;Tour:Noir;CaseVide;CaseVide;CaseVide;CaseVide;]
			Ligne[CaseVide;CaseVide;CaseVide;Tour:Noir;Tour:Noir;Tour:Noir;CaseVide;CaseVide;CaseVide;]
			Joueur1:Noir:Connard:1;
			Joueur2:Personne;
			PartieGagnee:Personne;
	- TODO :
		* Finir MAJ des données pour chaque coup
		* Voir comment splitter les fichiers pour une meilleure organisation
		* Ajouter contrôle sur le pseudo du joueur (les deux joueurs doivent avoir un pseudo différent)