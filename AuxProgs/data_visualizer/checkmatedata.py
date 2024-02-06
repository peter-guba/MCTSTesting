# Creates graphs and heatmaps showing the average numbers of moves it took for different algorithms to find the correct
# solutions in checkmate tests.


import numpy as np
import seaborn as sns
import pandas as pd
import matplotlib.pyplot as plt
from matplotlib.ticker import MultipleLocator
from common_functions import *
from matplotlib.colors import LogNorm

sns.set_theme(style="white", context="talk")
sns.set(rc={"figure.figsize": (7, 6), "figure.dpi": 300, 'savefig.dpi': 300}, font_scale=0.7)

# Path to the files which contain data about the number of wins, symwins, hull and damage for the
# different playout settings.
file = ""


# Fetches the number of moves it took an algorithm
# to correctly identify the best move n times where
# n is given by the move_count_to_find field.
def get_num_of_moves(field, move_count_to_find):
    for x in field.split(','):
        key = int(x.split(':')[0].strip())
        value = float(x.split(": ")[1].split('(')[0])
        if key == move_count_to_find:
            return value


# Fetches the number of moves it took an algorithm
# to correctly identify the best move n times where
# n is given by the move_count_to_find field.
def get_num_of_correct_attempts(field, move_count_to_find):
    for x in field.split(','):
        key = int(x.split(':')[0].strip())
        value = int(x.split(": ")[1].split('(')[1][:-1])
        if key == move_count_to_find:
            return value


# Fetches the variance of the number of moves it took
# an algorithm to correctly identify the best move
# n times where n is given by the move_count_to_find field.
def get_variance(field, move_count_to_find):
    for x in field.split(','):
        key = int(x.split(':')[0].strip())
        value = float(x.split(": ")[1])
        if key == move_count_to_find:
            if value == -1:
                return np.NaN
            else:
                return value


# Creates a heatmap from the given data which displays the given number
# of times the correct move was identified. The title is used as the title
# of the map as well as a part of the name of the output file and the color is
# the palette used by the map.
def make_heatmap(data, correct_move_count_to_display, title):
    test_ids = data.readline().split(';')[1:-1]
    lines = list(map(lambda l: l.split(";"), data.readlines()))

    data_dict = {
        "names": [convert_name(item[0]) for item in lines] * len(test_ids),
        "num_of_moves": [],
        "test_ids": np.repeat(test_ids, len(lines))
    }

    for test_index in range(1, len(test_ids) + 1):
        for l in lines:
            data_dict["num_of_moves"].append(get_num_of_moves(l[test_index], correct_move_count_to_display))

    df = pd.DataFrame(data_dict)

    _, ax = plt.subplots(1, 1)

    pivotted = df.pivot(index=["names"], columns=["test_ids"], values=["num_of_moves"])
    sns.heatmap(pivotted, fmt="n", annot=False, xticklabels=False, norm=LogNorm())
    ax.set_ylabel('')
    ax.set_xlabel('')
    plt.xticks(rotation=0)
    plt.yticks(rotation=0)
    plt.title(title)

    #plt.show()
    plt.savefig("checkmate test heatmap " + title + " " + str(correct_move_count_to_display) + ".png")
    plt.close()


def make_variance_heatmap(data, correct_move_count_to_display, title):
    test_ids = data.readline().split(';')[1:-1]
    lines = list(map(lambda l: l.split(";"), data.readlines()))

    data_dict = {
        "names": [convert_name(item[0]) for item in lines] * len(test_ids),
        "variances": [],
        "test_ids": np.repeat(test_ids, len(lines))
    }

    for test_index in range(1, len(test_ids) + 1):
        for l in lines:
            data_dict["variances"].append(get_variance(l[test_index], correct_move_count_to_display))

    df = pd.DataFrame(data_dict)

    _, ax = plt.subplots(1, 1)

    pivotted = df.pivot(index=["names"], columns=["test_ids"], values=["variances"])
    sns.heatmap(pivotted, fmt="n", annot=False, xticklabels=False)
    ax.set_ylabel('')
    ax.set_xlabel('')
    plt.xticks(rotation=0)
    plt.yticks(rotation=0)
    plt.title(title)

    #plt.show()
    plt.savefig("checkmate test heatmap " + title + " " + str(correct_move_count_to_display) + ".png")
    plt.close()


# Creates a lineplot for the given data which displays how many playouts were needed before each algorithm
# identified the correct move n times.
def make_plot(data, test_id, max_attempts):
    test_ids = data.readline().split(';')[1:-1]
    test_index = test_ids.index(test_id)
    lines = list(map(lambda l: l.split(";"), data.readlines()))
    names = [convert_name(item[0]) for item in lines]

    alphas = []
    alphas_index_map = [1, 2, 3, 5, 4, 7, 6, 8, 0, 9, 10]

    data_dict = {
        "names": np.repeat(names, 10),
        "num_of_correct": [10, 20, 30, 40, 50, 60, 70, 80, 90, 100] * len(lines),
        "avg_move_count": []
    }

    for l in lines:
        for moves in range(10, 101, 10):
            data_dict["avg_move_count"].append(get_num_of_moves(l[test_index + 1], moves))

            if moves == 100:
                alphas.append(get_num_of_correct_attempts(l[test_index + 1], moves) / max_attempts)

    df = pd.DataFrame(data_dict)
    pivotted = df.pivot(index=["num_of_correct"], columns=["names"], values=["avg_move_count"])

    colors = {
        "UCT": "lightcoral",
        "FAP": "gold",
        "HP": "grey",
        "QB": "magenta",
        "RB": "red",
        "RQB": "crimson",
        "Sig": "darkgreen",
        "SR": "olivedrab",
        "U-T": "darkorange",
        "VOI": "sienna",
        "WP": "darkred"
    }

    sns.lineplot(data=pivotted["avg_move_count"], errorbar=None, palette=colors, dashes=False)

    for index in range(len(plt.gca().lines) // 2):
        plt.gca().lines[index].set_alpha(alphas[alphas_index_map[index]])

    plt.yscale("log")
    plt.savefig("checkmate plot " + test_id + ".png")
    plt.clf()


data1 = open(file + "1.csv")
data2 = open(file + "2.csv")
data3 = open(file + "3.csv")
data4 = open(file + "4.csv")
data5 = open(file + "5.csv")
data6 = open(file + "6.csv")
data7 = open(file + "7.csv")
dataa = open(file + "all.csv")

datavars1 = open(file + "vars_1.csv")
datavars2 = open(file + "vars_2.csv")
datavars3 = open(file + "vars_3.csv")
datavars4 = open(file + "vars_4.csv")
datavars5 = open(file + "vars_5.csv")
datavars6 = open(file + "vars_6.csv")
datavars7 = open(file + "vars_7.csv")

all_data = [data1, data2, data3, data4, data5, data6, data7, dataa]

all_vars_data = [datavars1, datavars2, datavars3, datavars4, datavars5, datavars6, datavars7]

correct_move_counts = [x for x in range(10, 101, 10)]
str_move_counts = ["one", "two", "three", "four", "five", "six", "seven", "all"]
'''
for cmc in correct_move_counts:
    for data_index in range(8):
        make_heatmap(all_data[data_index], cmc, str_move_counts[data_index] + " move tests")
        all_data[data_index].seek(0)
'''
for cmc in correct_move_counts:
    for data_index in range(7):
        make_variance_heatmap(all_vars_data[data_index], cmc, "vars " + str_move_counts[data_index] + " move tests")
        all_vars_data[data_index].seek(0)
'''        
test_ids = []
for a in range(1, 8):
    for b in range(1, 101):
        test_ids.append(str(a) + '.' + str(b))

for i in range(len(test_ids)):
    make_plot(all_data[(i // 100)], test_ids[i], 10)
    all_data[(i // 100)].seek(0)

for ti in range(1, 8):
    make_plot(dataa, str(ti), 1000)
    dataa.seek(0)
'''