# Creates graph or heatmap from extra WP MCTS tests.

import os
import seaborn as sns
import pandas as pd
import matplotlib.pyplot as plt
import numpy as np
from matplotlib.ticker import MultipleLocator
from common_functions import *


chess_avg_max_hp = 5140
cotg_avg_max_hp = (21 + 10.5) / 2
microRTS_avg_max_hp = 4


sns.set_theme(style="white", context="talk")
sns.set(rc={"figure.figsize": (7, 6), "figure.dpi": 300, 'savefig.dpi': 300}, font_scale=1.2)

# The upper bound of the y axis.
y_limit = 100

# The step between numbers displayed on the y axis.
step = 25

# The number of battles per battle setting performed by a single algorithm.
battles_per_bs = 10080 / 3

# The average of the maximum numbers of hitpoints (or an equivalent metric) that a unit
# can have at the end of a game.
avg_max_hp_per_unit = chess_avg_max_hp

# Color palettes for different values used in heatmaps.
colors = {
    "wins": "Reds_r",
    "symwins": "Greens_r",
    "hull": "Blues_r",
    "damage": "Oranges_r",
    "draws": "Greys_r",
    "unfinished": "Purples_r"
}

# Path to the files which contain data about the number of wins, symwins, hull and damage for the
# different combat settings.
file = ""

# The name of the game the data of which is currently being processed.
game_name = "Chess"

'''
# Plots the given data to the give plot.
def make_subplot(data, p, divisor_multiplier, h_d_divisor_multiplier, title):
    # Get the names of MCTS variants.
    names = data.readline().split(';')[1:-1]

    # Create the dataframe for the values and another one for their
    # confidence bounds.
    lines = data.readlines()
    df = pd.DataFrame({
        "names": names,
        "wins": get_data(get_wins, wins_divisor * divisor_multiplier, lines),
        "symwins": get_data(get_symwins, symwins_divisor * divisor_multiplier, lines),
        "hull": get_data(get_hull, h_d_divisor_multiplier * hull_divisor * divisor_multiplier, lines),
        "damage": get_data(get_damage, h_d_divisor_multiplier * damage_divisor * divisor_multiplier, lines),
        "draws": get_data(get_draws, draws_divisor * divisor_multiplier, lines),
        "unfinished": get_data(get_unfinished, unfinished_divisor * divisor_multiplier, lines)
    })
    cfb = pd.DataFrame({
        "names": names,
        "wins": get_data(get_wins_cfb, 0.01 * divisor_multiplier, lines),
        "symwins": get_data(get_symwins_cfb, 0.01 * divisor_multiplier, lines),
        "hull": get_data(get_hull_cfb, 0.01 * divisor_multiplier, lines),
        "damage": get_data(get_damage_cfb, 0.01 * divisor_multiplier, lines),
        "draws": get_data(get_draws_cfb, 0.01 * divisor_multiplier, lines),
        "unfinished": get_data(get_unfinished_cfb, 0.01 * divisor_multiplier, lines)
    })

    # Sort the data according to the given metric.
    sorted_indices = np.argsort(df[sorting_field])
    df = df.iloc[sorted_indices]
    cfb = cfb.iloc[sorted_indices]
    dfm = df.melt(id_vars="names", var_name="data")
    cfbm = cfb.melt(id_vars="names", var_name="data")

    # Plot the data.
    sns.barplot(x="names", y="value", hue="data", data=dfm, palette="deep", ax=p)
    p.axhline(0, color="k", clip_on=False)
    p.set_ylabel("")
    p.set_xlabel(title)
    p.set_ylim(0, y_limit)
    sns.move_legend(p, "upper left", bbox_to_anchor=(1, 1))
    p.yaxis.set_major_locator(MultipleLocator(step))

    # Compute the x coordinates of the error bars (formula obtained by trial and error).
    x_err_positions = [(a / 7.5 - 1 / 3 + (a // 7) * 1 / 15) for a in range(77)]
    del x_err_positions[6::7]

    # Compute the y coordinates of the error bars, which are just the y coordinates
    # of the bars. The values in dfm are in order wins, symwins, hull, damage, draws,
    # unfinished, so they have to be reordered so that the metrics alternate.
    y_err_positions = [None] * len(dfm["value"])
    for index in range(6):
        y_err_positions[index::6] = dfm["value"][index * 11:index * 11 + 11]

    # Get the actual error bar values. cfbm is ordered the same way as dfm, so it also
    # has to be reordered.
    y_err_values = [None] * len(cfbm["value"])
    for index in range(6):
        y_err_values[index::6] = cfbm["value"][index * 11:index * 11 + 11]

    p.errorbar(x=x_err_positions, y=y_err_positions, yerr=y_err_values, fmt='none', c='k')'''


# Creates a heatmap from the given data which displays the given field.
# (e.g. the number of wins). The title is used as the title of the map
# as well as a part of the name of the output file and the color is
# the palette used by the map.
def make_heatmap(data, field_to_display, title, color, output, lb, ub):
    # Get the names of MCTS variants.
    first_row = data.readline().split(';')[1:-1]

    # Create the dataframe for the values and another one for their
    # confidence bounds.
    lines = data.readlines()

    playouts = []
    wins = []
    symwins = []
    hull = []
    damage = []
    draws = []
    unfinished = []
    names = []

    for l in lines:
        fields = l.split(';')
        for i in range(1, len(fields) - 1):
            if fields[i] != " X":
                playouts.append(int(fields[0]))
                wins.append(get_wins(fields[i]))
                symwins.append(get_symwins(fields[i]))
                hull.append(get_hull(fields[i]))
                damage.append(get_damage(fields[i]))
                draws.append(get_draws(fields[i]))
                unfinished.append(get_unfinished(fields[i]))
                names.append(first_row[i - 1])

    data.seek(0)

    df = pd.DataFrame({
        "playouts": playouts,
        "wins": wins,
        "symwins": symwins,
        "hull": hull,
        "damage": damage,
        "draws": draws,
        "unfinished": unfinished,
        "losses": [1008 - w - d - u for (w, d, u) in zip(wins, draws, unfinished)],
        "names": names
    })

    xlabels = names[:10]
    xlabels.sort()

    _, ax = plt.subplots(figsize=(8, 2.2))

    pivotted = df.pivot(columns=["names"], index=["playouts"], values=[field_to_display])
    h = sns.heatmap(pivotted, fmt="n", annot=True, xticklabels=xlabels, cmap=color, vmin=lb, vmax=ub, square=True)
    h.set_facecolor("white")
    h.set_facecolor("white")
    cb = h.collections[0].colorbar
    cb.set_ticks([0, ub/5, 2*ub/5, 3*ub/5, 4*ub/5, ub])
    ax.set_ylabel('')
    ax.set_xlabel('')
    plt.xticks(rotation=0)
    plt.yticks(rotation=0)
    plt.title(title)

    #plt.show()
    plt.savefig(output + game_name + " wp extra " + field_to_display + " " + title + ".png")
    plt.close()


# Loads data for each of the battle settings.
data4v4 = open(file + "_4v4_2D-2B.csv")
data8v8 = open(file + "_8v8_4D-4B.csv")
data16v16 = open(file + "_16v16_8D-8B.csv")
dataa = open(file + "_all.csv")
'''
# Create a heatmap for each tracked value from each data variable.
vals = ["wins", "symwins", "hull", "damage", "draws", "unfinished"]

for v in vals:
    ub = 336
    if v == "hull" or v == "damage":
        ub *= chess_avg_max_hp

    if v == "symwins":
        ub /= 2

    make_heatmap(data4v4, v, "4 vs 4", colors[v], "./", 0, ub)
    make_heatmap(data8v8, v, "8 vs 8", colors[v], "./", 0, ub)
    make_heatmap(data16v16, v, "16 vs 16", colors[v], "./", 0, ub)
    make_heatmap(dataa, v, "total", colors[v], "./", 0, ub * 3)
'''

ub = 336

make_heatmap(dataa, "wins", game_name, colors["wins"], "./", 0, ub * 3)
