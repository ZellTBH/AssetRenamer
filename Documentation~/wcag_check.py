"""Controle WCAG 2.2 AA du theme Asset Renamer.

Lit les tokens --ar-* directement dans le USS (source unique). Une couleur rgba n'a pas de ratio
en soi : chaque fond est donc decrit comme une PILE de calques, du plus profond au plus haut
(ex. ["bg", "glass-2", "glass-3"] = bouton sur une section vitree sur le fond de la fenetre).
La pile est composee (alpha over) en une couleur opaque, puis le premier plan, s'il est
translucide, est compose a son tour sur ce resultat avant le calcul du ratio.

Usage :
    python wcag_check.py <chemin vers AssetRenamerTheme.uss>
Code de sortie 1 si une paire non exemptee echoue.
"""
import re
import sys

TEXT, UI, EXEMPT = 4.5, 3.0, None

# Piles de fonds reellement utilisees par la fenetre.
BG = ["bg"]                                       # fond de la fenetre, preview, pied de page
SECTION = ["bg", "glass-2"]                       # header, barre d'outils, sections
BUTTON = ["bg", "glass-2", "glass-3"]             # bouton secondaire au repos ou desactive
BUTTON_HOVER = ["bg", "glass-2", "accent-glass"]  # bouton secondaire ou '?' au survol
PRIMARY = ["bg", "glass-2", "accent-85"]          # Apply au repos, secondaire presse
PRIMARY_HOVER = ["bg", "glass-2", "accent"]       # Apply au survol
HELP_PANEL = ["bg", "glass-2", "glass-1"]         # panneau d'aide dans sa section
HELP_ACTIVE = ["bg", "glass-2", "glass-1", "accent-glass"]
ROW = ["bg", "glass-1"]                           # ligne de preview, zone de depot
ROW_HOVER = ["bg", "glass-3"]                     # ligne survolee, zone de depot en glisser
PILL = ["bg", "glass-1", "glass-3"]               # pastille de statut dans une ligne
PILL_HOVER = ["bg", "glass-3", "glass-3"]         # pastille dans une ligne survolee
BADGE = ["bg", "glass-3"]                         # pastille compteur PREVIEW
FIELD = ["bg"]                                    # interieur des champs et des cases (--ar-bg)

# (premier plan, pile de fond, usage, seuil)
PAIRS = [
    # Texte
    ("text", SECTION, "titre du header, toggles et texte des sections", TEXT),
    ("text-muted", SECTION, "libelles des champs", TEXT),
    ("link", SECTION, "titres de section, en-tete TRANSFORMATIONS", TEXT),
    ("link", BG, "titre PREVIEW", TEXT),
    ("warning", SECTION, "avertissement table manquante", TEXT),
    ("text", FIELD, "texte saisi dans les champs", TEXT),
    ("text", BUTTON, "boutons secondaires et '?' au repos", TEXT),
    ("text", BUTTON_HOVER, "boutons secondaires et '?' au survol", TEXT),
    ("on-accent", PRIMARY, "Apply au repos, secondaire presse", TEXT),
    ("on-accent", PRIMARY_HOVER, "Apply au survol et presse", TEXT),
    ("text-muted", HELP_PANEL, "noms du panneau d'aide", TEXT),
    ("text", HELP_PANEL, "exemples du panneau d'aide", TEXT),
    ("text", HELP_ACTIVE, "nom et exemple de la convention active", TEXT),
    ("text", ROW, "zone de depot : libelle", TEXT),
    ("text-muted", ROW, "zone de depot : types", TEXT),
    ("text", ROW_HOVER, "zone de depot pendant un glisser : libelle", TEXT),
    ("text-muted", ROW_HOVER, "zone de depot pendant un glisser : types", TEXT),
    ("text", BG, "toggle Hide unchanged", TEXT),
    ("text", BADGE, "pastille compteur PREVIEW", TEXT),
    ("text-muted", BG, "preview vide, compteurs, resultat, version", TEXT),
    ("warning", BG, "compteur d'issues", TEXT),
    ("orange", BG, "badge Update available", TEXT),
    ("bg", ["bg", "orange"], "badge Update au survol", TEXT),
    ("text", ROW, "nouveau nom (ligne)", TEXT),
    ("text", ROW_HOVER, "nouveau nom (ligne survolee)", TEXT),
    ("text-muted", ROW, "ancien nom, fleche, message, nouveau nom Excluded", TEXT),
    ("text-muted", ROW_HOVER, "idem, ligne survolee", TEXT),
    ("success", PILL, "pastille Ok", TEXT),
    ("success", PILL_HOVER, "pastille Ok, ligne survolee", TEXT),
    ("text-muted", PILL, "pastilles Unchanged et Excluded", TEXT),
    ("text-muted", PILL_HOVER, "pastilles Unchanged et Excluded, ligne survolee", TEXT),
    ("warning", PILL, "pastille Collision", TEXT),
    ("warning", PILL_HOVER, "pastille Collision, ligne survolee", TEXT),
    ("error-text", PILL, "pastille Invalid", TEXT),
    ("error-text", PILL_HOVER, "pastille Invalid, ligne survolee", TEXT),
    # Limites des composants a saisie, contre le fond reel des deux cotes
    ("border-strong", SECTION, "cadre des champs, listes, ObjectField et cases dans une section", UI),
    ("border-strong", FIELD, "cadre des champs et des cases contre leur interieur", UI),
    ("border-strong", ROW, "cadre des cases d'exclusion dans une ligne", UI),
    ("border-strong", ROW_HOVER, "cadre des cases d'exclusion, ligne survolee", UI),
    ("border-strong", BG, "cadre de la case Hide unchanged", UI),
    ("accent-hover", SECTION, "focus d'un champ contre la section", UI),
    ("accent-hover", FIELD, "focus d'un champ contre son interieur", UI),
    ("accent-hover", BG, "contour de la zone de depot pendant un glisser, cote fenetre", UI),
    ("accent-hover", ROW_HOVER, "contour de la zone de depot pendant un glisser, cote zone", UI),
    ("orange", BG, "contour du badge Update", UI),
    # Points de statut (doublent le mot) contre la pastille
    ("success", PILL, "point Ok", UI),
    ("warning", PILL, "point Collision", UI),
    ("error-text", PILL, "point Invalid", UI),
    ("text-muted", PILL, "points Unchanged et Excluded", UI),
    ("error-text", PILL_HOVER, "point Invalid, ligne survolee", UI),
    # Exemptions
    ("glass-border", SECTION, "bordures vitrees des sections, lignes, zone de depot : decoratif", EXEMPT),
    ("glass-3", SECTION, "fond des boutons secondaires : le texte identifie le bouton (1.4.11)", EXEMPT),
    ("accent-85", SECTION, "fond d'Apply : le texte identifie le bouton (1.4.11)", EXEMPT),
    ("accent", FIELD, "fond d'une case cochee : etat porte par la coche et le cadre border-strong", EXEMPT),
    ("text-disabled", BUTTON, "texte des boutons desactives", EXEMPT),
    ("glass-border", BG, "repos de la zone de depot : le libelle identifie la zone", EXEMPT),
]


def parse_color(value):
    value = value.strip()
    if value.startswith("#"):
        return tuple(int(value[i:i + 2], 16) for i in (1, 3, 5)) + (1.0,)
    numbers = [float(n) for n in re.findall(r"[\d.]+", value)]
    return (numbers[0], numbers[1], numbers[2], numbers[3] if len(numbers) > 3 else 1.0)


def over(top, bottom):
    alpha = top[3]
    return tuple(top[i] * alpha + bottom[i] * (1 - alpha) for i in range(3)) + (1.0,)


def flatten(stack, tokens):
    result = tokens[stack[0]]
    assert result[3] == 1.0, "the bottom of a stack must be opaque: " + stack[0]
    for name in stack[1:]:
        result = over(tokens[name], result)
    return result


def luminance(color):
    linear = []
    for channel in color[:3]:
        c = channel / 255
        linear.append(c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4)
    return 0.2126 * linear[0] + 0.7152 * linear[1] + 0.0722 * linear[2]


def ratio(a, b):
    la, lb = luminance(a), luminance(b)
    return (max(la, lb) + 0.05) / (min(la, lb) + 0.05)


def to_hex(color):
    return "#%02X%02X%02X" % tuple(int(round(c)) for c in color[:3])


def read_tokens(uss_path):
    text = open(uss_path, encoding="utf-8").read()
    found = re.findall(r"--ar-([a-z0-9-]+):\s*(#[0-9A-Fa-f]{6}|rgba?\([^)]*\))", text)
    return {name: parse_color(value) for name, value in found}


def main():
    tokens = read_tokens(sys.argv[1])
    failures = 0
    print("| Premier plan | Fond (pile) | Fond compose | Usage | Ratio | Seuil | Resultat |")
    print("|---|---|---|---|---|---|---|")
    for fg, stack, usage, threshold in PAIRS:
        background = flatten(stack, tokens)
        foreground = over(tokens[fg], background)
        value = ratio(foreground, background)
        if threshold is None:
            verdict, shown = "exempt", "-"
        else:
            ok = value >= threshold
            failures += 0 if ok else 1
            verdict, shown = ("OK" if ok else "ECHEC"), "%.1f:1" % threshold
        print("| %s | %s | %s | %s | %.2f:1 | %s | %s |" % (fg, " > ".join(stack), to_hex(background), usage, value, shown, verdict))
    print("\nEchecs hors exemptions : %d" % failures)
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main()
