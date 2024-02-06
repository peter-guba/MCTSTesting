# Creates graphs and heatmaps showing the average number of wins, symwins, hull, damage, draws and unfinished battles
# separated by playout setting. 

import os
import numpy as np
import seaborn as sns
import pandas as pd
import matplotlib.pyplot as plt
from matplotlib.ticker import MultipleLocator
from common_functions import *


chess_avg_max_hp = 5140
cotg_avg_max_hp = (2 + 4 + 8) * (21 + 10.5) / 3
microRTS_avg_max_hp = (2 + 4 + 8) * (4 + 4) / 3


sns.set_theme(style="white", context="talk")
sns.set(rc={"figure.figsize": (7, 6), "figure.dpi": 300, 'savefig.dpi': 300}, font_scale=0.9)

# Determines the field according to which the graphs are supposed to be sorted.
sorting_field = "wins"

# The upper bound of the y axis.
y_limit = 100

# The step between numbers displayed on the y axis.
step = 25

# The number of battles per playout setting performed by a single algorithm.
battles_per_ps = 10080 / 3

# The average of the maximum numbers of hitpoints (or an equivalent metric) that an algorithm
# can have at the end of a game.
avg_max_hp_per_battle = cotg_avg_max_hp

# The number by which the average number of wins is supposed to be divided.
wins_divisor = battles_per_ps / 100

# The number by which the average number of symwins is supposed to be divided.
symwins_divisor = (battles_per_ps / 2) / 100

# The number by which the remaining hull is supposed to be divided.
hull_divisor = battles_per_ps * avg_max_hp_per_battle / 100

# The number by which the amount of damage dealt to the enemy is supposed to be divided.
damage_divisor = battles_per_ps * avg_max_hp_per_battle / 100

# The number by which the average number of draws is supposed to be divided.
draws_divisor = battles_per_ps / 100

# The number by which the average number of unfinished battles is supposed to be divided.
unfinished_divisor = battles_per_ps / 100

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
# different playout settings.
file = ""

# The name of the game the data of which is currently being processed.
game_name = "Chess"


# Plots the given data to the give plot.
def make_subplot(data, p, divisor_multiplier, title, add_conf_bounds):
    # Get the names of MCTS variants.
    names = data.readline().split(';')[1:-1]

    # Create the dataframe for the values and another one for their
    # confidence bounds.
    lines = data.readlines()
    df = pd.DataFrame({
        "names": names,
        "wins": get_data(get_wins, wins_divisor * divisor_multiplier, lines),
        "symwins": get_data(get_symwins, symwins_divisor * divisor_multiplier, lines),
        "hull": get_data(get_hull, hull_divisor * divisor_multiplier, lines),
        "damage": get_data(get_damage, damage_divisor * divisor_multiplier, lines),
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
        "unfinished": get_data(get_unfinished_cfb, 0.01 * unfinished_divisor * divisor_multiplier, lines)
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

    if add_conf_bounds:
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
    plt.savefig(output + game_name + " playout heatmap " + field_to_display + " " + title + ".png")
    plt.close()

'''
bSettings = ["_4v4_2D-2B", "_8v8_4D-4B", "_16v16_8D-8B", ""]
outputFolder = game_name + " data"

for b in bSettings:
    folderName = "battles total"
    if b != "":
        folderName = b.split('v')[0][1:] + " vs " + b.split('v')[0][1:]

    plotFileNamePart = "battles total (playout data)"
    if b != "":
        plotFileNamePart = folderName

    titlePart = "total"
    if b != "":
        titlePart = folderName

    allPlotTitle = "total"
    if b != "":
        allPlotTitle = folderName

    allFile = b + ".csv"
    if b == "":
        allFile = "_all.csv"

    div_multiplier = 3
    if b != "":
        div_multiplier = 1

    # Load data for each of the playout settings.
    data1000 = open(file + "_1000" + b + ".csv")
    data5000 = open(file + "_5000" + b + ".csv")
    data10000 = open(file + "_10000" + b + ".csv")
    dataa = open(file + allFile)

    # Creates a plot with 4 subplots - one for every playout setting and one for the average of all
    # the playout settings.
    fig, (p1000, p5000, p10000, pt) = plt.subplots(4)

    # Create the plots.
    make_subplot(data1000, p1000, div_multiplier, "1000 playouts", True)
    make_subplot(data5000, p5000, div_multiplier, "5000 playouts", True)
    make_subplot(data10000, p10000, div_multiplier, "10000 playouts", True)
    make_subplot(dataa, pt, div_multiplier * 3, "total", True)

    sns.despine(bottom=True)
    plt.tight_layout(h_pad=2)

    os.makedirs(outputFolder + '\\' + folderName, exist_ok=True)

    # plt.show()
    plt.savefig(outputFolder + '\\' + folderName + "\\" + game_name + " " + plotFileNamePart + ".png")

    data1000.seek(0)
    data5000.seek(0)
    data10000.seek(0)
    dataa.seek(0)

    # Create a heatmap for each tracked value from each data variable.
    vals = ["wins", "symwins", "hull", "damage", "draws", "unfinished"]

    heatmapOutputFolder = outputFolder + '\\' + folderName + "\\heatmaps\\"
    os.makedirs(heatmapOutputFolder, exist_ok=True)
    for v in vals:
        make_heatmap(data1000, v, titlePart + " 1000 playouts", colors[v], heatmapOutputFolder)
        make_heatmap(data5000, v, titlePart + " 5000 playouts", colors[v], heatmapOutputFolder)
        make_heatmap(data10000, v, titlePart + " 10000 playouts", colors[v], heatmapOutputFolder)
        make_heatmap(dataa, v, allPlotTitle, colors[v], heatmapOutputFolder)
'''

dataa = open(file + "_all.csv")
make_heatmap(dataa, "wins", "Chess wins", colors["wins"], "./")
