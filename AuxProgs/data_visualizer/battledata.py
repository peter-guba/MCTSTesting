# Creates graphs and heatmaps showing the average number of wins, symwins, hull, damage, draws and unfinished battles
# separated by combat setting.

import os
import seaborn as sns
import pandas as pd
import matplotlib.pyplot as plt
import numpy as np
from matplotlib.ticker import MultipleLocator
from common_functions import *


cotg_avg_max_hp = (21 + 10.5) / 2
microRTS_avg_max_hp = 4


sns.set_theme(style="white", context="talk")
sns.set(rc={"figure.figsize": (7, 6), "figure.dpi": 300, 'savefig.dpi': 300}, font_scale=1.0)

# Determines the field according to which the graphs are supposed to be sorted.
sorting_field = "wins"

# The upper bound of the y axis.
y_limit = 100

# The step between numbers displayed on the y axis.
step = 25

# The number of battles per battle setting performed by a single algorithm.
battles_per_bs = 10080 / 3

# The average of the maximum numbers of hitpoints (or an equivalent metric) that a unit
# can have at the end of a game.
avg_max_hp_per_unit = microRTS_avg_max_hp

# The number by which the average number of wins is supposed to be divided.
wins_divisor = battles_per_bs / 100

# The number by which the average number of symwins is supposed to be divided.
symwins_divisor = (battles_per_bs / 2) / 100

# The number by which the remaining hull is supposed to be divided.
hull_divisor = battles_per_bs * avg_max_hp_per_unit / 100

# The number by which the amount of damage dealt to the enemy is supposed to be divided.
damage_divisor = battles_per_bs * avg_max_hp_per_unit / 100

# The number by which the average number of draws is supposed to be divided.
draws_divisor = battles_per_bs / 100

# The number by which the average number of unfinished battles is supposed to be divided.
unfinished_divisor = battles_per_bs / 100

# Color palettes for different values used in heatmaps.
colors = {
    "wins": "Reds_r",
    "symwins": "Greens_r",
    "hull": "Blues_r",
    "damage": "Oranges_r",
    "draws": "Greys_r",
    "unfinished": "Purples_r"
}

algo_colors = {
    "FAP": "gold",
    "HP": "grey",
    "QB": "magenta",
    "RB": "red",
    "RQB": "crimson",
    "Sig": "darkgreen",
    "SR": "olivedrab",
    "UCT": "lightcoral",
    "U-T": "darkorange",
    "VOI": "sienna",
    "WP": "darkred"
}

# Path to the files which contain data about the number of wins, symwins, hull and damage for the
# different combat settings.
file = ""

# The name of the game the data of which is currently being processed.
game_name = "μRTS"


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

    p.errorbar(x=x_err_positions, y=y_err_positions, yerr=y_err_values, fmt='none', c='k')


# Creates a heatmap from the given data which displays the given field.
# (e.g. the number of wins). The title is used as the title of the map
# as well as a part of the name of the output file and the color is
# the palette used by the map.
def make_heatmap(data, field_to_display, title, color, output):
    # Get the names of MCTS variants.
    first_row = data.readline().split(';')[1:-1]

    # Create the dataframe for the values and another one for their
    # confidence bounds.
    lines = data.readlines()

    names = []
    wins = []
    symwins = []
    hull = []
    damage = []
    draws = []
    unfinished = []
    opponents = []

    for l in lines:
        fields = l.split(';')
        for i in range(1, len(fields) - 1):
            if fields[i] != " X":
                names.append(fields[0])
                wins.append(get_wins(fields[i]))
                symwins.append(get_symwins(fields[i]))
                hull.append(get_hull(fields[i]))
                damage.append(get_damage(fields[i]))
                draws.append(get_draws(fields[i]))
                unfinished.append(get_unfinished(fields[i]))
                opponents.append(first_row[i - 1])

    data.seek(0)

    df = pd.DataFrame({
        "names": names,
        "wins": wins,
        "symwins": symwins,
        "hull": hull,
        "damage": damage,
        "draws": draws,
        "unfinished": unfinished,
        "opponents": opponents
    })

    xlabels = opponents[:11]
    xlabels.sort()

    _, ax = plt.subplots(1, 1)

    pivotted = df.pivot(index=["names"], columns=["opponents"], values=[field_to_display])
    h = sns.heatmap(pivotted, fmt="n", annot=True, xticklabels=xlabels, cmap=color)
    h.set_facecolor("white")
    ax.set_ylabel('')
    ax.set_xlabel('')
    plt.xticks(rotation=0)
    plt.yticks(rotation=0)
    plt.title(title)

    #plt.show()
    plt.savefig(output + game_name + " battle heatmap " + field_to_display + " " + title + ".png")
    plt.close()


def make_subplot_modified(data, p, lower_bound, upper_bound, _step, title):
    # Get the names of MCTS variants.
    names = data.readline().split(';')[1:-1]

    # Create the dataframe for the values and another one for their
    # confidence bounds.
    lines = data.readlines()
    df = pd.DataFrame({
        "names": names,
        "wins": get_data(get_wins, 1, lines)
    })
    clrs = pd.DataFrame({
        "names": names,
        "colors": [algo_colors[n.strip()] for n in names]
    })

    # Sort the data according to the given metric.
    sorted_indices = np.argsort(df[sorting_field])
    df = df.iloc[sorted_indices]
    clrs = clrs.iloc[sorted_indices]
    dfm = df.melt(id_vars="names", var_name="data")

    # Plot the data.
    b = sns.barplot(x="names", y="value", data=dfm, ax=p, palette=clrs["colors"])
    for i in b.containers:
        b.bar_label(i, )
    p.axhline(lower_bound, color="k", clip_on=False)
    p.set_ylabel("")
    p.set_xlabel(title)
    p.set_ylim(lower_bound, upper_bound)
    p.yaxis.set_major_locator(MultipleLocator(_step))


'''
pSettings = ["_1000", "_5000", "_10000", ""]
outputFolder = game_name + " data"

for p in pSettings:
    folderName = "playouts total"
    if p != "":
        folderName = p[1:]

    plotFileNamePart = "playouts total (battle data)"
    if p != "":
        plotFileNamePart = p[1:]

    titlePart = "total"
    if p != "":
        titlePart = p[1:]

    allPlotTitle = "total"
    if p != "":
        allPlotTitle = titlePart + " playouts"

    allFile = p + ".csv"
    if p == "":
        allFile = "_all.csv"

    div_multiplier = 3
    if p != "":
        div_multiplier = 1

    # Loads data for each of the battle settings.
    data4v4 = open(file + p + "_4v4_2D-2B.csv")
    data8v8 = open(file + p + "_8v8_4D-4B.csv")
    data16v16 = open(file + p + "_16v16_8D-8B.csv")
    dataa = open(file + allFile)

    # Creates a plot with 4 subplots - the first three are for different combat settings and the
    # last is for an average of all combat settings (including the ones not shown in the first
    # three plots).
    fig, (p4v4, p8v8, p16v16, pa) = plt.subplots(4)

    # Create the plots.
    make_subplot(data4v4, p4v4, div_multiplier, 4, "4 vs 4 " + titlePart + " playouts")
    make_subplot(data8v8, p8v8, div_multiplier, 8, "8 vs 8 " + titlePart + " playouts")
    make_subplot(data16v16, p16v16, div_multiplier, 16, "16 vs 16 " + titlePart + " playouts")
    make_subplot(dataa, pa, div_multiplier * 3, 28/3, allPlotTitle)

    sns.despine(bottom=True)
    plt.tight_layout(h_pad=2)

    os.makedirs(outputFolder + '\\' + folderName, exist_ok=True)

    # plt.show()
    plt.savefig(outputFolder + '\\' + folderName + "\\" + game_name + " " + plotFileNamePart + ".png")

    data4v4.seek(0)
    data8v8.seek(0)
    data16v16.seek(0)
    dataa.seek(0)

    # Create a heatmap for each tracked value from each data variable.
    vals = ["wins", "symwins", "hull", "damage", "draws", "unfinished"]

    heatmapOutputFolder = outputFolder + '\\' + folderName + "\\heatmaps\\"
    os.makedirs(heatmapOutputFolder, exist_ok=True)
    for v in vals:
        make_heatmap(data4v4, v, "4 vs 4 " + titlePart + " playouts", colors[v], heatmapOutputFolder)
        make_heatmap(data8v8, v, "8 vs 8 " + titlePart + " playouts", colors[v], heatmapOutputFolder)
        make_heatmap(data16v16, v, "16 vs 16 " + titlePart + " playouts", colors[v], heatmapOutputFolder)
        make_heatmap(dataa, v, allPlotTitle, colors[v], heatmapOutputFolder)
'''

data4v4 = open(file + "_4v4_2D-2B.csv")
data8v8 = open(file + "_8v8_4D-4B.csv")
data16v16 = open(file + "_16v16_8D-8B.csv")
dataa = open(file + "_all.csv")

fig, (p4v4, p8v8, p16v16) = plt.subplots(3)

# Create the plots.
make_subplot_modified(data4v4, p4v4, 2000, 8000, 1000, "4 vs 4")
make_subplot_modified(data8v8, p8v8, 2000, 8000, 1000, "8 vs 8")
make_subplot_modified(data16v16, p16v16, 2000, 8000, 1000, "16 vs 16")

sns.despine(bottom=True)
plt.tight_layout(h_pad=2)

# plt.show()
plt.savefig("./" + game_name + ".png")
