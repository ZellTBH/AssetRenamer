# Asset Renamer - workflow

## Depots
- Ce package est un depot git IMBRIQUE (remote ZellTBH/asset-renamer), embarque dans le projet
  hote de developpement, qui l'ignore via son .gitignore. Un commit fait a la racine hote
  n'attrape pas le package, et inversement.
- L'arbre de travail du depot hote est sale en permanence (TutorialInfo, ProjectSettings, URP) :
  ne jamais stager ces fichiers.

## Theme
- Feuille unique : Editor/Resources/AssetRenamerTheme.uss. Tous les tokens (--ar-*) sont declares
  une seule fois, en tete, sur .ar-root (pas sur :root). Aucune couleur ni aucun rayon en dur
  ailleurs, ni dans le USS ni dans le C#. Statuts : classes ar-status--*, jamais de Color en C#.
- Squelette : Editor/Resources/AssetRenamerWindow.uxml. Les bascules d'affichage passent par la
  classe ar-hidden. Apres un changement d'option, seul RefreshPreview() reconstruit (la preview).
- Reference visuelle : le rendu de Balance Forge (BreakingFrameSandbox). La palette vient de
  BreakingFrameTheme.md (depot breaking-frame-launcher).
- Contraste WCAG 2.2 AA obligatoire, controle a chaque changement visuel :
  python Documentation~/wcag_check.py Editor/Resources/AssetRenamerTheme.uss
  Attendu : 0 echec hors exemptions. Le script compose les couleurs translucides sur leur pile
  de fonds : toute nouvelle paire couleur / fond doit y etre ajoutee.

## Logo
- Editor/Resources/BreakingFrameLogo.png est une copie locale du logo de Balance Forge.
  Pour le remplacer : copier le PNG SEUL, jamais son .meta. Deux packages avec le meme GUID dans
  un projet = conflit. Laisser Unity generer un nouveau .meta.

## Tests
- Tests/Editor (asmdef AssetRenamer.Editor.Tests). Le projet hote liste le package dans
  "testables" (Packages/manifest.json). Test Runner > EditMode > Run All.
- Les tests disque creent puis suppriment Assets/_ARTestTemp/ : le dossier ne doit pas rester.
- Les tests de l'Engine sont des tests de CARACTERISATION : une bizarrerie se documente, elle ne
  se corrige pas sans decision explicite.

## Backlog
Travail identifie, pas encore fait (le CHANGELOG ne liste que ce qui est livre).
- Bouton Update : il fait Client.Add(GIT_URL) sans revision, donc installe le haut de main et
  fait perdre l'epinglage d'un projet (ex. #0.4.0). Il devrait installer le tag de la derniere
  version : GIT_URL + "#" + version lue dans le manifeste.
- Bizarrerie n1 des tests de caracterisation, candidate a un fix: : avec un padding de numero,
  le DERNIER groupe de chiffres est extrait meme au milieu du nom puis recolle en suffixe
  (rock2_big -> RockBig02). Test qui la fige : BuildPlans_NumberPadding_MovesInnerDigitsToEnd.

## Regles
- Auto Refresh desactive : Ctrl+R dans Unity apres chaque modification, avant toute validation.
- Identifiants anglais ASCII, commentaires en francais sans accents, aucun em dash. Controle
  ASCII de chaque fichier cree ou modifie (l'outil d'ecriture convertit les sequences \u).
- Git : stage explicite fichier par fichier (jamais add -A ni add .), .meta toujours avec son
  asset, git mv / git rm pour deplacer ou supprimer, message dans un fichier puis commit -F,
  aucun trailer Co-Authored-By. Aucun push ni tag : le mainteneur s'en charge.
