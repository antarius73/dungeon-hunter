# Dungeon Hunter

Jeu de donjon en 2D, au tour par tour et case par case, développé avec **Godot 4.7.2 (.NET / C#)**.

Le joueur dirige un elfe qui explore un ancien complexe nain plongé dans l'obscurité. Guidé par sa pierre de lumière, il doit trouver l'escalier qui mène au niveau inférieur, en évitant ou en combattant des gobelins qui voient dans le noir et entendent le moindre bruit.

## État du projet

Prototype **V0** : un niveau de 30 × 30 cases, 5 gobelins, des portes, un escalier de sortie.

- Tour par tour : une action du joueur, puis tous les monstres agissent.
- Lumière : la pierre éclaire à 3 cases, les murs et portes fermées bloquent la lumière et la vue, brouillard de guerre avec mémoire des cases vues.
- Bruit : léger (3 cases), fort (10 cases), énorme (tout le niveau). Les gobelins enquêtent sur les bruits et sont attirés par la lumière.
- Gobelins : garde, repos ou exploration, puis investigation, alerte, recherche et chasse.
- Combat : dague au contact (4 directions), arc jusqu'à 8 cases dans toutes les directions.

## Lancer le jeu

Prérequis : [Godot 4.7.2 .NET](https://godotengine.org/download) et le SDK .NET 8.

1. Ouvrir `project.godot` dans Godot.
2. Cliquer sur **Build**, puis **Play** (F5).

## Commandes

Toutes les actions se font au clavier ou à la souris. **H** affiche l'aide en jeu.

| Action | Clavier | Souris |
|---|---|---|
| Se déplacer | Flèches, ZQSD, pavé numérique ; diagonales A E W C | Clic sur une case voisine |
| Attendre | Espace | Bouton Attendre ou clic sur le héros |
| Dague / Arc | 1 / 2 pour changer, F pour attaquer | Boutons de la barre |
| Pierre de lumière | L (lancer), L deux fois (allumer / éteindre), R (récupérer) | Bouton Pierre, clic sur la pierre au sol |
| Interagir | G (ramasser, fermer une porte) | Bouton Interagir, clic sur un objet |
| Annuler | Échap | Clic droit |

## Organisation

```
assets/      graphismes (tileset 0x72, interface Free Fantasy GUI)
docs/        document de conception et catalogues des graphismes
levels/      niveaux au format JSON (un caractère par case)
scenes/      scène principale
scripts/
  Core/      grille, atlas du tileset
  Game/      règles du jeu (monde, héros, monstres, bruit)
  View/      affichage (niveau, brouillard, personnages, interface)
```

## Documentation

- [Document de conception](docs/conception.md) (version Word : `docs/conception.docx`)
- Catalogues des graphismes, à ouvrir dans un navigateur : `docs/catalogue_tileset.html`, `docs/catalogue_gui.html`

## Crédits

- Tileset : *Dungeon Tileset II* v1.7, par 0x72.
- Interface : pack *Free Fantasy GUI*.
