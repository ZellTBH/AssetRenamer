"""Controle WCAG 2.2 AA du theme Asset Renamer.

Lit les tokens --ar-* directement dans le USS (source unique), puis calcule le ratio de
contraste de chaque paire couleur / fond utilisee. Usage :
    python wcag_check.py <chemin vers AssetRenamerTheme.uss>
Code de sortie 1 si une paire non exemptee echoue.
"""
import re
import sys

TEXT, LARGE, UI, EXEMPT = 4.5, 3.0, 3.0, None

# (premier plan, fond, usage, seuil). Les fonds sont ceux reellement sous l'element :
# bg = contenu de la fenetre et fond des champs, surface = groupes et lignes,
# surface-2 = ligne survolee, ligne d'aide active, bouton presse.
PAIRS = [
    # Texte courant
    ("text", "bg", "texte par defaut, etat vide de la preview", TEXT),
    ("text", "bg", "texte saisi dans les champs (fond --ar-bg)", TEXT),
    ("text", "surface", "libelles des toggles, noms dans les lignes", TEXT),
    ("text", "surface-2", "noms dans une ligne survolee", TEXT),
    ("text", "bg", "exemple du panneau d'aide, badge compteur", TEXT),
    ("text", "surface-2", "exemple sur la ligne d'aide active", TEXT),
    ("text-muted", "surface", "libelles des champs, foldout, '?' au repos", TEXT),
    ("text-muted", "bg", "boutons secondaires au repos (zone contenu), compteurs, resultat, version, zone de depot", TEXT),
    ("text-muted", "surface-2", "boutons secondaires presses, nom sur ligne d'aide active", TEXT),
    # Titres et liens
    ("link", "surface", "titres de section dans les groupes", TEXT),
    ("link", "bg", "titre PREVIEW, '?' et secondaires au survol (zone contenu)", TEXT),
    ("link", "surface-2", "secondaires au survol puis presses", TEXT),
    # Bouton primaire et badge update
    ("on-accent", "accent", "texte Apply au repos, coche des cases", TEXT),
    ("on-accent", "accent-hover", "texte Apply au survol", TEXT),
    ("orange", "bg", "texte du badge Update available", TEXT),
    ("bg", "orange", "texte du badge Update au survol", TEXT),
    # Statuts en texte (lignes sur surface, survol surface-2)
    ("success", "surface", "statut Ok", TEXT),
    ("success", "surface-2", "statut Ok, ligne survolee", TEXT),
    ("text-muted", "surface", "statuts Unchanged et Excluded", TEXT),
    ("text-muted", "surface-2", "statuts Unchanged et Excluded, ligne survolee", TEXT),
    ("warning", "surface", "statut Collision, avertissement table manquante", TEXT),
    ("warning", "surface-2", "statut Collision, ligne survolee", TEXT),
    ("warning", "bg", "compteur d'issues", TEXT),
    ("error-text", "surface", "statut Invalid", TEXT),
    ("error-text", "surface-2", "statut Invalid, ligne survolee", TEXT),
    # Composants interactifs (contours) contre le fond adjacent
    ("border-strong", "bg", "contour des champs (fond interieur) et secondaires sur le contenu", UI),
    ("border-strong", "surface", "contour des champs, cases, '?' et secondaires dans un groupe", UI),
    ("border-strong", "surface-2", "contour des cases dans une ligne survolee", UI),
    ("border-strong", "bg", "contour de la zone de depot", UI),
    ("accent-hover", "bg", "indicateur de focus contre le fond du champ", UI),
    ("accent-hover", "surface", "indicateur de focus contre la surface du groupe", UI),
    ("link", "bg", "contour des secondaires et du '?' au survol", UI),
    ("link", "surface", "contour des secondaires et du '?' au survol dans un groupe", UI),
    ("accent", "bg", "fond du bouton Apply contre le contenu", UI),
    ("orange", "bg", "contour du badge Update", UI),
    # Indicateurs d'etat non textuels
    ("success", "surface", "barre de statut Ok", UI),
    ("warning", "surface", "barre de statut Collision", UI),
    ("error", "surface", "barre de statut Invalid", UI),
    ("error", "surface-2", "barre de statut Invalid, ligne survolee", UI),
    ("link", "surface-2", "barre de la ligne d'aide active", UI),
    # Exemptions (decoratif, redondant avec le texte, ou desactive)
    ("text-faint", "surface", "barres Unchanged / Excluded : le mot du statut porte l'information", EXEMPT),
    ("border", "surface", "bordures des groupes et des lignes : decoratif", EXEMPT),
    ("border", "bg", "bordure du badge compteur, cadre de l'icone de depot : decoratif", EXEMPT),
    ("accent", "surface", "fond d'une case cochee : l'etat est porte par la coche (on-accent) et le contour border-strong", EXEMPT),
]


def luminance(hex_color):
    channels = [int(hex_color[i:i + 2], 16) / 255 for i in (1, 3, 5)]
    linear = [c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in channels]
    return 0.2126 * linear[0] + 0.7152 * linear[1] + 0.0722 * linear[2]


def ratio(a, b):
    la, lb = luminance(a), luminance(b)
    return (max(la, lb) + 0.05) / (min(la, lb) + 0.05)


def read_tokens(uss_path):
    text = open(uss_path, encoding="utf-8").read()
    return {name: value.upper() for name, value in re.findall(r"--ar-([a-z0-9-]+):\s*(#[0-9A-Fa-f]{6})", text)}


def main():
    tokens = read_tokens(sys.argv[1])
    failures = 0
    print("| Premier plan | Fond | Usage | Ratio | Seuil | Resultat |")
    print("|---|---|---|---|---|---|")
    for fg, bg, usage, threshold in PAIRS:
        value = ratio(tokens[fg], tokens[bg])
        if threshold is None:
            verdict, shown = "exempt", "-"
        else:
            ok = value >= threshold
            failures += 0 if ok else 1
            verdict, shown = ("OK" if ok else "ECHEC"), "%.1f:1" % threshold
        print("| %s `%s` | %s `%s` | %s | %.2f:1 | %s | %s |" % (fg, tokens[fg], bg, tokens[bg], usage, value, shown, verdict))
    print("\nEchecs hors exemptions : %d" % failures)
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main()
