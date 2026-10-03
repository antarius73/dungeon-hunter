# Dungeon Hunter – Document de conception


Version révisée 13 – 3 octobre 2026

## 1. Présentation

**Dungeon Hunter** est un jeu de donjon en 2D, au tour par tour et case par case. Le joueur dirige un elfe qui explore un labyrinthe plongé dans l’obscurité. Il doit trouver le passage vers le niveau inférieur, jusqu’au dernier niveau, où il affronte **Zoltan**, le tyran qui terrorise la région.

Le jeu reprend la vue du tileset 0x72 : **vue de dessus légèrement inclinée**, sur une grille carrée de 16 × 16 px. Il n’y a pas de vue isométrique.

Le héros est l’**elfe**. C’est le seul personnage jouable ; les autres héros du tileset pourront servir d’ennemis. **Zoltan** est représenté par le grand démon du tileset.

### Univers

Le donjon est un ancien complexe nain, oublié puis passé sous le contrôle du mal. Cela explique qu’on y trouve encore des armes, des bombes et des pièges laissés par les nains.

On y rencontre des monstres, des pièges et des éléments de décor interactifs : portes, leviers, boutons, fontaines, caisses et tonneaux.

### Niveaux

Tous les niveaux sont dessinés à la main et décrits dans des fichiers JSON. Il n’y a pas de génération aléatoire : chaque niveau fait l’objet d’un level design travaillé.

Le passage au niveau suivant se fait par une échelle ou un escalier. La différence entre les deux est purement visuelle.

### Équipement de départ

Le héros commence avec un arc, une dague, des pièges, des potions et sa pierre de lumière. Il trouve d’autres potions et d’autres armes en explorant le donjon.

## 2. Boucle de jeu

Le jeu se joue au tour par tour :

1.  Le joueur effectue **une seule action**.

2.  Le monde réagit : chaque monstre et chaque élément actif (bombe, piège…) effectue son action.

3.  On revient au joueur.

Chaque action du joueur coûte exactement un tour, quelle qu’elle soit. Le joueur peut aussi **passer son tour**. Sous l’effet d’une potion d’énergie, il agit deux fois avant que le monde réagisse.

## 3. Actions du joueur

Toutes les actions sont réalisables au clavier ou à la souris. Le niveau de bruit de chaque action est détaillé au chapitre 5.

### Déplacement

- Déplacement d’une case dans les quatre directions.

- Déplacement en diagonale autorisé, sauf s’il faut passer par un coin de mur.

- Descendre une échelle ou un escalier pour changer de niveau.

### Combat

- Frapper avec l’arme équipée dans une direction donnée, sur un monstre, un mur, une porte ou dans le vide. La portée et les effets dépendent de l’arme.

- La dague frappe une case voisine dans les 4 directions orthogonales.

- Tirer à l’arc vers n’importe quelle case située de 2 à 8 cases du héros, dans toutes les directions. Une flèche ne rate jamais : elle touche le premier monstre sur sa trajectoire. Si elle ne touche personne, elle tombe en fin de course et peut être ramassée.

- Changer d’arme équipée.

### Objets

- Boire une fiole d’un type précis, s’il en reste.

- Poser un piège à pics sur une case adjacente, s’il en reste.

- Poser une bombe sur une case adjacente, s’il en reste.

- Allumer une bombe située sur une case adjacente.

- Lancer un crâne de 1 à 6 cases (leurre).

### Pierre de lumière

- L’allumer ou l’éteindre quand le héros la porte.

- La lancer (1 à 6 cases, en ligne droite ou en diagonale) ou la poser au sol. On peut viser une case dans le noir : seuls les murs et portes fermées déjà vus limitent le choix. Si un obstacle se trouve sur la trajectoire (mur, porte fermée, monstre), la pierre retombe sur la dernière case libre avant lui.

- La récupérer, où qu’elle soit : ramasser et rappeler sont une seule et même action, qui coûte 1 tour.

### Ramassage

- Ramasser une arme. Le héros porte deux armes au maximum : s’il en ramasse une troisième, il choisit celle qu’il laisse au sol à la place.

- Ramasser une potion ou un crâne.

- Ramasser des flèches sur sa case ou sur l’une des 8 cases voisines (pas en diagonale à travers un angle de mur) : toutes les flèches de la case sont ramassées d’un coup, en une seule action.

### Interactions avec le décor

- Actionner un levier ou un bouton. Il n’y a pas de clé : les portes verrouillées s’ouvrent uniquement par ces mécanismes. Le fichier du niveau indique quel mécanisme commande quelle porte, et l’effet est réversible : actionner de nouveau referme la porte.

- Ouvrir ou fermer une porte non verrouillée.

- Ouvrir un coffre non verrouillé.

- Casser une caisse ou un tonneau. Ce sont les seuls objets destructibles : portes et coffres ne peuvent pas être détruits. Leur contenu éventuel tombe au sol.

- Frapper une porte ou un mur : cela ne fait que du bruit (bruit fort), ce qui peut servir de leurre.

- Boire à une fontaine d’eau (voir le chapitre 4).

### Portes

Comme dans le tileset, une porte occupe **deux cases côte à côte** dans un mur horizontal. Ses deux battants s’ouvrent et se ferment ensemble. Dans le fichier du niveau, une porte s’écrit toujours ++ (fermée) ou // (ouverte).

### Trajectoire d’une flèche

- La flèche suit une ligne droite du héros vers la case visée, et la prolonge jusqu’à 8 cases. La distance se compte en cases, diagonales comprises.

- Une case adjacente au héros est trop proche : on ne tire pas sur un monstre au contact.

- On peut tirer dans le vide ou contre un mur : contre un mur adjacent, la flèche tombe aux pieds du héros, avec un bruit fort à l’impact.

- Comme pour les déplacements, une flèche ne passe pas en diagonale entre deux angles de mur.

- Elle touche automatiquement le premier monstre rencontré : il n’y a pas de jet de réussite.

- Les murs et les portes fermées l’arrêtent : elle tombe sur la dernière case libre avant l’obstacle.

- Si rien ne l’arrête, elle tombe sur la 8ᵉ case.

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr class="odd">
<td><p><strong>Objet verrouillé</strong></p>
<p>Tenter d’ouvrir une porte ou un coffre verrouillé consomme quand même le tour. Un message prévient le joueur. Seul un mécanisme (levier, bouton) peut les ouvrir.</p></td>
</tr>
</tbody>
</table>

### Fioles

Il existe 4 fioles, chacune en 2 tailles : la **petite fiole contient 1 dose**, la **grande 2 doses**. Le nombre de doses de chaque couleur est affiché à côté de son icône dans la barre d’équipement. Tous les effets se cumulent, entre couleurs différentes comme dans une même couleur.

| **Fiole**          | **Effet d’une dose**                                                                                       | **Cumul**                                                  |
|--------------------|------------------------------------------------------------------------------------------------------------|------------------------------------------------------------|
| Rouge – Soin       | Rend 1 point de vie immédiatement, sans dépasser 3 cœurs.                                                  | Une dose par point de vie, plafond de 3 cœurs.             |
| Bleue – Énergie    | Le héros agit deux fois par tour pendant 5 tours du monde.                                                 | +5 tours par dose.                                         |
| Verte – Poison     | La prochaine touche avec l’arme équipée fait +1 dégât. Un coup dans le vide ou sur un objet ne compte pas. | +1 dégât par dose, toujours sur la seule prochaine touche. |
| Jaune – Résistance | La prochaine source de dégâts, et elle seule, est réduite de 1.                                            | Avec X doses, cette prochaine source est réduite de X.     |

### Bombes

- En allumant la bombe, le joueur choisit dans une fenêtre la longueur de la mèche : de 1 à 10 tours.

- Explosion : 3 dégâts sur la case de la bombe, 2 dégâts sur les 8 cases autour (zone de 3 × 3 cases).

- L’explosion fait un bruit énorme, entendu dans tout le niveau.

- Elle détruit les caisses et les tonneaux, mais ni les portes, ni les coffres, ni les murs.

- Une caisse ou un tonneau peut contenir des objets, qui tombent au sol quand on le détruit.

- Elle blesse aussi le héros s’il se trouve dans la zone.

### Pièges à pics du héros

- Le héros pose le piège sur une case adjacente.

- Le monstre qui marche dessus subit 1 dégât, puis le piège disparaît.

- Le monstre est ensuite **ralenti jusqu’à sa mort** : il n’agit plus qu’un tour sur deux. Un monstre qui se régénère n’est plus ralenti dès qu’il retrouve tous ses points de vie.

- Le héros peut aussi se blesser sur son propre piège : 1 dégât, et il est ralenti pendant 5 tours seulement.

### Crâne

- Le crâne sert de **leurre** : il se lance de 1 à 6 cases et s’arrête au premier obstacle.

- Il ne fait aucun dégât, mais son impact fait du bruit et peut déclencher un piège du donjon.

## 4. Lumière et visibilité

Le donjon est plongé dans l’obscurité totale. Les monstres, eux, voient dans le noir.

### Les trois états d’une case

| **État**                | **Affichage**                                                                                            | **Monstres visibles** |
|-------------------------|----------------------------------------------------------------------------------------------------------|-----------------------|
| Jamais vue              | Case noire                                                                                               | Non                   |
| Déjà vue, plus éclairée | Case dans l’ombre, telle qu’elle était la dernière fois qu’on l’a vue (porte ouverte ou fermée, objets…) | Non                   |
| Éclairée                | Case clairement visible                                                                                  | Oui                   |

### La pierre de lumière

Sans lumière, le héros ne voit que les 8 cases qui l’entourent. La pierre de lumière change ce rayon selon son état :

| **État de la pierre**        | **Zone éclairée**                                           |
|------------------------------|-------------------------------------------------------------|
| Portée par le héros, allumée | Rayon d’environ 3 cases autour du héros, en forme de cercle |
| Portée par le héros, éteinte | Les 8 cases voisines du héros (vision par défaut)           |
| Posée ou lancée au sol       | Rayon de 3 cases autour de la pierre                        |

Une case est soit éclairée, soit dans le noir : il n’y a pas de demi-éclairage. Le « cercle » de 3 cases est donc l’ensemble des cases dont le centre est à 3 cases ou moins de la source.

**Les murs et les portes fermées bloquent la lumière et la vue.** Une case cachée derrière un mur ou une porte fermée, par rapport à la source, reste dans le noir. Les monstres ne bloquent ni la lumière ni la vue.

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr class="odd">
<td><p><strong>Règle de lisibilité</strong></p>
<p>Une zone éclairée par la pierre reste toujours visible pour le joueur, même si le héros n’a pas de ligne de vue dessus. C’est un choix ludique assumé : le joueur peut s’en servir pour surveiller un couloir.</p></td>
</tr>
</tbody>
</table>

La pierre est magique : le héros peut la rappeler à lui en une action, où qu’elle soit. Elle revient automatiquement quand il change de niveau.

### La lumière attire les monstres

Les monstres sont attirés par la lumière de la pierre **comme par un bruit fort**. Un monstre qui voit la pierre allumée à 10 cases ou moins vient voir, qu’elle soit portée par le héros ou posée au sol. Posée au sol, elle devient un leurre ; portée, elle trahit le héros, qui a intérêt à l’éteindre pour se cacher.

### Fontaines

| **Fontaine** | **Effet**                                                                                                                                                                                                                       |
|--------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Lave         | Éclaire les cases dans un rayon de 3 cases. Elle est donc visible dès l’arrivée sur le niveau. Les monstres ne sont pas attirés par cette lumière. Un monstre ou le héros poussé dedans subit 1 dégât.                          |
| Eau          | Boire apporte la **clarté** : le plan du niveau est révélé, mais il reste dans le brouillard (les cases s’affichent comme déjà vues, sans les monstres). On peut y boire de nouveau, mais sans effet une fois le niveau révélé. |

## 5. Bruit

### Niveaux de bruit

Les portées sont comptées en nombre de cases, diagonales comprises : une case en diagonale compte pour 1.

| **Niveau** | **Portée**     | **Exemples**                                                                                         |
|------------|----------------|------------------------------------------------------------------------------------------------------|
| Aucun      | —              | Attendre, passer son tour                                                                            |
| Léger      | 3 cases        | Marcher, changer d’arme, boire, frapper dans le vide, impact au sol d’une flèche ou d’un objet lancé |
| Fort       | 10 cases       | Frapper un monstre, un mur, une porte ou un objet, flèche qui touche un monstre                      |
| Énorme     | Tout le niveau | Explosion de bombe                                                                                   |

- Toute action du héros, sauf attendre, produit au moins un bruit léger.

- L’environnement et les monstres produisent aussi du bruit, que le joueur peut entendre.

- Le bruit traverse les murs : aucun effet d’assourdissement.

- Les objets lancés (crâne, pierre) et les flèches font du bruit à l’impact.

### Effet du bruit sur les monstres

- Les monstres ne réagissent qu’aux bruits inhabituels : une fontaine ne les attire pas, les bruits des autres monstres non plus.

- Tout bruit produit par le héros les attire.

- Un monstre qui entend un bruit mémorise la case d’origine et s’y dirige. Si un nouveau bruit survient, il change de cible.

- Priorité : énorme \> fort \> léger. À niveau égal et en même temps, il choisit au hasard.

- Le bruit ne compte plus dès que le monstre voit le héros (voir le chapitre 6).

### Effet du bruit sur le joueur

Quand le héros entend un bruit venant d’une case qu’il ne voit pas, une onomatopée s’affiche sur cette case (« Clac ! », « Grrr… »).

## 6. Comportement des monstres

### Comportement par défaut

| **Comportement** | **Description**                                                     |
|------------------|---------------------------------------------------------------------|
| Garde            | Reste immobile à son poste.                                         |
| Repos            | Se déplace dans un rayon de 5 cases autour de sa position initiale. |
| Explorateur      | Se promène dans le niveau (algorithme de promenade à définir).      |

### Vision

Les monstres voient dans le noir. Leur portée de vue dépend de leur type. Comme pour la lumière, leur vue est bloquée par les murs et les portes fermées, mais pas par les autres monstres.

| **Monstre**  | **Portée de vue**                            |
|--------------|----------------------------------------------|
| Gobelin      | 8 cases                                      |
| Autres types | Définie par type, en général de 1 à 10 cases |

### Règles générales

- Les monstres peuvent ouvrir et fermer les portes non verrouillées.

- Un monstre qui se promène (garde, repos, explorateur) referme la porte derrière lui. En investigation, en alerte, en recherche ou en chasse, il la laisse ouverte.

- Ils connaissent les pièges du donjon et ne marchent jamais dessus, ni sur leurs déclencheurs.

- Ils peuvent en revanche marcher sur les pièges posés par le héros (voir le chapitre 3).

- Ils ignorent la lumière des fontaines de lave.

- Les gobelins frappent au contact dans les 8 directions, sauf en diagonale à travers un angle de mur.

### États et transitions

| **État**      | **Déclencheur**     | **Comportement**                                                                     | **Sortie**                                                                                           |
|---------------|---------------------|--------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------|
| Normal        | Début du niveau     | Garde, repos ou explorateur.                                                         | Bruit → Investigation. Héros vu → Alerte.                                                            |
| Investigation | Bruit entendu       | Va à la source du bruit, puis reste 3 à 6 tours dans un rayon de 5 cases.            | Fin → retour au poste (garde, repos) ou reprise de l’exploration depuis ce point. Héros vu → Alerte. |
| Alerte        | Héros vu            | Va vers le héros et l’attaque dès qu’il est à portée.                                | Héros perdu de vue → Recherche.                                                                      |
| Recherche     | Héros perdu de vue  | Va à l’endroit où il a perdu le héros. Prioritaire sur tous les bruits.              | Fin → Chasse.                                                                                        |
| Chasse        | Fin de la recherche | Explore le donjon pour retrouver le héros. Ne revient jamais à la garde ni au repos. | Héros vu → Alerte.                                                                                   |

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr class="odd">
<td><p><strong>À retenir</strong></p>
<p>Un monstre qui a vu le héros une fois ne redevient jamais calme : il passe en alerte, puis en chasse.</p></td>
</tr>
</tbody>
</table>

## 7. Caractéristiques

### Héros

- 3 points de vie, représentés par des cœurs. C’est aussi le maximum : la fiole rouge ne soigne jamais au-delà de 3 cœurs.

- Le nombre de cœurs est toujours affiché.

- À 0, le héros meurt : partie perdue.

### Monstres

Ils ont aussi des points de vie, en général 1 seul. Certains en ont plusieurs. Les héros non jouables du tileset peuvent servir d’ennemis.

**Régénération** : certains types de monstres ont un score de régénération. Avec une régénération de 5, le monstre regagne 1 point de vie tous les 5 tours. Revenu à son maximum de points de vie, il n’est plus ralenti.

## 8. Interface

### Disposition de l’écran

- Écran de référence 1920 × 1080. Le donjon est affiché à 2,4 : chaque pixel du tileset fait 2,4 pixels à l’écran. Textes et interface sont dimensionnés pour cet écran (doublés par rapport à la première version).

- Le niveau occupe toute la largeur de l’écran. Comme il est plus haut que l’écran, la caméra suit le héros verticalement.

- Une barre en bas d’écran affiche les cœurs et tout l’équipement.

### Contenu de la barre d’équipement

- Deux emplacements d’arme, l’arme équipée mise en évidence. Le nombre de flèches est affiché sur l’arc.

- Nombre de bombes, de pièges, de flèches et de crânes.

- Nombre de doses pour chacun des quatre types de fiole, à côté de son icône.

- Bouton de la pierre de lumière, grisé quand la pierre est au sol.

- Bouton Interagir et bouton Attendre.

- Icône d’aide (point d’interrogation).

- Bouton Menu : pause, recommencer, quitter (plus tard : sauvegarde, réglages).

- Chaque bouton affiche sa touche clavier.

### Contrôles

**Toutes les actions se font entièrement au clavier ou entièrement à la souris**, et on peut mélanger les deux : déclencher une action au clavier puis choisir la case d’un clic, par exemple.

### Principe des actions ciblées

1.  Déclencher l’action : une touche ou un bouton de la barre.

2.  Les cases possibles s’allument.

3.  Choisir la cible : clic sur une case allumée, ou au clavier une direction (et Entrée pour valider un curseur).

4.  Appuyer de nouveau sur la même touche, ou recliquer sur le même bouton, déclenche l’action secondaire s’il y en a une, sinon annule.

**Échap** ou **clic droit** annule l’action en cours. Sans action en cours, Échap ouvre le menu. Le clic droit ne sert qu’à annuler.

### Correspondance des actions

| **Action**                                                                                                      | **Clavier**                                                                                                             | **Souris**                                                                                                                                                                                                            |
|-----------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Se déplacer                                                                                                     | Flèches, ZQSD, pavé numérique ; diagonales A E W C                                                                      | Clic sur une case voisine                                                                                                                                                                                             |
| Ouvrir une porte                                                                                                | Avancer dedans                                                                                                          | Clic sur la porte voisine                                                                                                                                                                                             |
| Attendre                                                                                                        | Espace                                                                                                                  | Bouton Attendre, ou clic sur le héros                                                                                                                                                                                 |
| Changer d’arme                                                                                                  | 1 (dague) / 2 (arc)                                                                                                     | Clic sur l’arme qui n’est pas en main                                                                                                                                                                                 |
| Dague                                                                                                           | Avancer sur un gobelin, ou F puis une des 4 directions (gobelin, mur ou vide)                                           | Clic sur un gobelin voisin, ou bouton Dague puis clic sur une des 4 cases                                                                                                                                             |
| Arc                                                                                                             | F puis une direction (tir droit devant), ou Tab pour choisir un gobelin visible puis Entrée                             | Bouton Arc puis clic dans la direction voulue, ou clic direct sur une case lointaine. On choisit la direction, pas la distance : la flèche file jusqu’à 8 cases, ce qui permet de tirer dans le vide ou dans le noir. |
| Pierre de lumière                                                                                               | L : cases de lancer allumées ; direction pour placer le curseur, Entrée pour lancer. L une 2ᵉ fois : allumer / éteindre | Bouton Pierre : cases allumées ; clic sur une case pour lancer. Reclic sur le bouton : allumer / éteindre                                                                                                             |
| Récupérer la pierre                                                                                             | R                                                                                                                       | Clic sur la pierre au sol                                                                                                                                                                                             |
| Interagir (ramasser des flèches sur sa case ou une case voisine, fermer une porte voisine, récupérer la pierre) | G : s’il n’y a qu’un choix, il est fait tout de suite ; sinon une direction, ou G / Entrée pour sa propre case          | Bouton Interagir puis clic sur une case allumée                                                                                                                                                                       |
| Aide / Menu                                                                                                     | H / Échap                                                                                                               | Boutons Aide et Menu                                                                                                                                                                                                  |

- Au survol d’un objet ramassable (flèches sur la case du héros ou une case voisine, pierre au sol), le curseur devient une main et l’objet est entouré d’un cercle lumineux. Un clic le ramasse.

- Le survol d’un monstre affiche une infobulle avec ses points de vie et son état d’alerte.

### Utiliser l’équipement à venir

Les fioles, bombes, pièges et crânes suivront le même principe :

| **Objet**      | **Utilisation**                                                                                                     |
|----------------|---------------------------------------------------------------------------------------------------------------------|
| Fiole          | Clic sur la fiole ou sa touche : le héros en boit une dose.                                                         |
| Bombe ou piège | Choisir une case adjacente où le poser. Pour une bombe, une fenêtre demande la longueur de la mèche (1 à 10 tours). |
| Crâne          | Choisir une case de 1 à 6 cases de distance, comme pour la pierre.                                                  |

Le crâne et la pierre ne font aucun dégât et s’arrêtent au premier obstacle.

### Animations

- Héros et monstres ont une animation de déplacement, une animation d’attente (respiration) et une animation quand ils encaissent un coup.

- L’arme équipée est visible dans la main du héros. Le tileset ne fournit pas d’animation d’attaque : l’arme est un sprite séparé, animé par code (coup de dague balayé vers la cible, arc orienté et bandé vers la case visée).

## 9. Enchaînement des écrans

1.  Écran d’accueil : histoire, objectif et bouton **Démarrer**.

2.  Partie : boucle du niveau.

3.  Changement de niveau : panneau de présentation du niveau et bouton **Continuer**.

4.  Partie perdue : panneau **Game Over**.

### Sauvegarde

- Sauvegarde automatique à chaque changement de niveau.

- Quand le joueur quitte, l’état exact de la partie est sauvegardé et repris au lancement suivant.

## 10. Prototype V0

Le premier prototype se limite à un seul niveau pour valider la boucle de jeu, la lumière et le bruit.

| **Élément**  | **Contenu de la V0**                                                                                                                                                                                                                                                                                               |
|--------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Niveau       | 1 niveau de 30 × 30 cases, construit avec le tileset et décrit dans levels/level_01.json.                                                                                                                                                                                                                          |
| Objectif     | Atteindre l’escalier : cela termine la V0.                                                                                                                                                                                                                                                                         |
| Monstres     | 5 gobelins, 1 point de vie, 1 dégât au contact (y compris en diagonale sans angle de mur), vue à 8 cases.                                                                                                                                                                                                          |
| Interactions | Uniquement des portes non verrouillées, sur deux cases.                                                                                                                                                                                                                                                            |
| Lumière      | Pierre de lumière : rayon de 3 cases, portée ou au sol. Elle attire les gobelins comme un bruit fort. Représentée par la pièce d’or animée du tileset.                                                                                                                                                             |
| Affichage    | Donjon à 2,4 sur un écran 1920 × 1080, interface doublée, caméra qui suit le héros.                                                                                                                                                                                                                                |
| Équipement   | Arc, 5 flèches, dague, pierre de lumière.                                                                                                                                                                                                                                                                          |
| Arc          | Tir vers n’importe quelle case de 2 à 8 cases, dans toutes les directions (pas au contact). 1 dégât, consomme 1 flèche. Pas de tir raté : le premier monstre sur la trajectoire est touché. Sans cible, la flèche tombe en fin de course et peut être ramassée. Un monstre tué lâche les flèches qui l’ont touché. |
| Dague        | Frappe une case adjacente dans les 4 directions orthogonales. 1 dégât. On peut frapper un mur ou le vide pour faire du bruit.                                                                                                                                                                                      |

## 11. Questions ouvertes

Points restant à trancher.

1.  **Poussée :** la fontaine de lave et les pièges du donjon supposent qu’on puisse pousser un monstre ou le héros. Quelle action ou quel monstre permet de pousser ?

2.  **Zoltan :** points de vie, attaques et phases du combat de boss, à définir plus tard.
