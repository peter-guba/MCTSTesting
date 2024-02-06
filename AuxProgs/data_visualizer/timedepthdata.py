# Creates graphs showing the average time, depth and number of rounds for different algorithms.

import seaborn as sns
import pandas as pd
import matplotlib.pyplot as plt
from common_functions import *
from matplotlib.ticker import MultipleLocator
import numpy as np
import os

sns.set_theme(style="white", context="talk")
sns.set(rc={"figure.figsize": (7, 6), "figure.dpi": 300, 'savefig.dpi': 300}, font_scale=0.7)

# The upper bound of the y axis.
y_limit = 20

# The step between numbers displayed on the y axis.
step = y_limit / 10

# Determines the field according to which the graphs are supposed to be sorted.
sorting_field = "rounds"

# A boolean that determines the order in which the data items are supposed to be sorted.
ascending = False

# A number by which the time data are supposed to be divided.
time_divisor = 10000

# Creates a plot with 4 subplots.
fig, (p1, p2, p3, pa) = plt.subplots(4)

# Path to the files that contain data about time and depth for the different combat settings.
file = ""

# A palette for the plots, so that they are colored differently than the plots with the four primary metrics.
palette = sns.color_palette("bright")[7:]

# The name of the game the data of which is currently being processed.
game_name = "microRTS"


def make_subplot(data, p, title, add_conf_bounds):
    lines = list(map(lambda l: l.split(";"), data.readlines()))[1:]

    df = pd.DataFrame({
        "names": [convert_name(item[0]) for item in lines],
        "time": [float(item[1]) / time_divisor for item in lines],
        "depth": [float(item[3]) for item in lines],
        "rounds": [float(item[5]) for item in lines]
    })
    cfb = pd.DataFrame({
        "names": [convert_name(item[0]) for item in lines],
        "time": [float(item[2]) / time_divisor for item in lines],
        "depth": [float(item[4]) for item in lines],
        "rounds": [float(item[6]) for item in lines]
    })

    # Sort the data according to the given metric.
    sorted_indices = np.argsort(df[sorting_field])
    if not ascending:
        sorted_indices = np.flip(sorted_indices)
    df = df.iloc[sorted_indices]
    cfb = cfb.iloc[sorted_indices]
    dfm = df.melt(id_vars="names", var_name="data")
    cfbm = cfb.melt(id_vars="names", var_name="data")

    sns.barplot(x="names", y="value", hue="data", data=dfm, palette=palette, ax=p)
    p.axhline(0, color="k", clip_on=False)
    p.set_ylabel("")
    p.set_xlabel(title)
    p.set_ylim(0, y_limit)
    for container in p.containers:
        p.bar_label(container, fmt='%.2f')
    sns.move_legend(p, "upper left", bbox_to_anchor=(1, 1))
    p.yaxis.set_major_locator(MultipleLocator(step))

    if add_conf_bounds:
        # Compute the x coordinates of the error bars (formula obtained by trial and error,
        # doesn't seem to be quite correct yet).
        x_err_positions = []
        for i in range(11):
            x_err_positions += [i - 1/3.75, i, i + 1/3.75]

        # Compute the y coordinates of the error bars, which are just the y coordinates
        # of the bars. The values in dfm are in order wins, symwins, hull, damage, draws,
        # unfinished, so they have to be reordered so that the metrics alternate.
        y_err_positions = [None] * len(dfm["value"])
        for index in range(3):
            y_err_positions[index::3] = dfm["value"][index * 11:index * 11 + 11]

        # Get the actual error bar values. cfbm is ordered the same way as dfm, so it also
        # has to be reordered.
        y_err_values = [None] * len(cfbm["value"])
        for index in range(3):
            y_err_values[index::3] = cfbm["value"][index * 11:index * 11 + 11]

        p.errorbar(x=x_err_positions, y=y_err_positions, yerr=y_err_values, fmt='none', c='k')


bSettings = ["_4v4", "_8v8", "_16v16", ""]
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
        allFile = ".csv"

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
    make_subplot(data1000, p1000, "1000 playouts", True)
    make_subplot(data5000, p5000, "5000 playouts", True)
    make_subplot(data10000, p10000, "10000 playouts", True)
    make_subplot(dataa, pt, "total", True)

    sns.despine(bottom=True)
    plt.tight_layout(h_pad=2)

    os.makedirs(outputFolder + '\\' + folderName, exist_ok=True)

    # plt.show()
    plt.savefig(outputFolder + '\\' + folderName + "\\" + game_name + " " + plotFileNamePart + " td.png")

pSettings = ["_1000", "_5000", "_10000", ""]

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
        allFile = ".csv"

    div_multiplier = 3
    if p != "":
        div_multiplier = 1

    # Loads data for each of the battle settings.
    data4v4 = open(file + p + "_4v4.csv")
    data8v8 = open(file + p + "_8v8.csv")
    data16v16 = open(file + p + "_16v16.csv")
    dataa = open(file + allFile)

    # Creates a plot with 4 subplots - the first three are for different combat settings and the
    # last is for an average of all combat settings (including the ones not shown in the first
    # three plots).
    fig, (p4v4, p8v8, p16v16, pa) = plt.subplots(4)

    # Create the plots.
    make_subplot(data4v4, p4v4, "4 vs 4 " + titlePart + " playouts", True)
    make_subplot(data8v8, p8v8, "8 vs 8 " + titlePart + " playouts", True)
    make_subplot(data16v16, p16v16, "16 vs 16 " + titlePart + " playouts", True)
    make_subplot(dataa, pa, allPlotTitle, True)

    sns.despine(bottom=True)
    plt.tight_layout(h_pad=2)

    os.makedirs(outputFolder + '\\' + folderName, exist_ok=True)

    # plt.show()
    plt.savefig(outputFolder + '\\' + folderName + "\\" + game_name + " " + plotFileNamePart + " td.png")