# Creates latex tables that give the average values and the rankings of the different algorithms with respect to the average number of wins,
# symwins, the average amount of remaining hull, average amount of damage dealt to the enemy, average time and average depth. The tables are
# created for various playout settings.

import pandas as pd
from operator import add
from common_functions import *


# This hasn't been adapted as it's not clear that you'll use it.


# The number by which the average number of wins is supposed to be divided.
wins_divisor = 1

# The number by which the average number of symwins is supposed to be divided.
symwins_divisor = 1

# The number by which the remaining hull is supposed to be divided.
hull_divisor = (4 + 8 + 16 + 32 + 48 + 64) * 6

# The number by which the amount of damage dealt to the enemy is supposed to be divided.
damage_divisor = (4 + 8 + 16 + 32 + 48 + 64) * 6

# Path to the files which contain data about the number of wins, symwins, hull and damage for the
# different playout settings.
file = ""

# Path to the files that contain data about time and depth for the different combat settings.
tdfile = ""

# A reordering applied to time and depth data, as the files that contain these values store them in a different
# order than the other files.
reorder = [2, 3, 4, 5, 6, 7, 1, 8, 9, 0]

# A list in which all the created dataframes are stored.
frames = []

# Loads data for each of the battle settings.

data100 = open(file + "100.csv")
data500 = open(file + "500.csv")
data1000 = open(file + "1000.csv")

tddata100 = open(tdfile + "_100.csv")
tddata500 = open(tdfile + "_500.csv")
tddata1000 = open(tdfile + "_1000.csv")
tddatat = open(tdfile + ".csv")

names = data100.readline().split(';')[1:]

# Create pandas dataframes for all the settings.

lines = data100.readlines()
tdlines = list(map(lambda l: l.split(";"), tddata100.readlines()))[1:]

tdnames = [convert_name(item[0]) for item in tdlines]

df100 = pd.DataFrame({
    "names": names,
    "wins": get_data(get_wins, wins_divisor, lines),
    "symwins": get_data(get_symwins, symwins_divisor, lines),
    "hull": get_data(get_hull, 4 * hull_divisor, lines),
    "damage": get_data(get_damage, 4 * damage_divisor, lines),
    "time": [[-float(item[1]) for item in tdlines][i] for i in reorder],
    "depth": [[float(item[3]) for item in tdlines][i] for i in reorder]
})
frames.append(df100)

lines = data500.readlines()[1:]
tdlines = list(map(lambda l: l.split(";"), tddata500.readlines()))[1:]
df500 = pd.DataFrame({
    "names": names,
    "wins": get_data(get_wins, wins_divisor, lines),
    "symwins": get_data(get_symwins, symwins_divisor, lines),
    "hull": get_data(get_hull, 8 * hull_divisor, lines),
    "damage": get_data(get_damage, 8 * damage_divisor, lines),
    "time": [[-float(item[1]) for item in tdlines][i] for i in reorder],
    "depth": [[float(item[3]) for item in tdlines][i] for i in reorder]
})
frames.append(df500)

lines = data1000.readlines()[1:]
tdlines = list(map(lambda l: l.split(";"), tddata1000.readlines()))[1:]
df1000 = pd.DataFrame({
    "names": names,
    "wins": get_data(get_wins, wins_divisor, lines),
    "symwins": get_data(get_symwins, symwins_divisor, lines),
    "hull": get_data(get_hull, 16 * hull_divisor, lines),
    "damage": get_data(get_damage, 16 * damage_divisor, lines),
    "time": [[-float(item[1]) for item in tdlines][i] for i in reorder],
    "depth": [[float(item[3]) for item in tdlines][i] for i in reorder]
})
frames.append(df1000)

tdlines = list(map(lambda l: l.split(";"), tddatat.readlines()))[1:]
dft = pd.DataFrame({
    "names": df100["names"],
    "wins": [x / 6 for x in map(add, map(add, df100["wins"], df500["wins"]), df1000["wins"])],
    "symwins": [x / 6 for x in map(add, map(add, df100["symwins"], df500["symwins"]), df1000["symwins"])],
    "hull": [x / 6 for x in map(add, map(add, df100["hull"], df500["hull"]), df1000["hull"])],
    "damage": [x / 6 for x in map(add, map(add, df100["damage"], df500["damage"]), df1000["damage"])],
    "time": [[-float(item[1]) for item in tdlines][i] for i in reorder],
    "depth": [[float(item[3]) for item in tdlines][i] for i in reorder]
})
frames.append(dft)

# The ordering in which the variants are supposed to be returned.
variant_ordering = frames[0].index

# Get the rankings for every metric.
wins = get_rankings("wins", frames, variant_ordering)
symwins = get_rankings("symwins", frames, variant_ordering)
hull = get_rankings("hull", frames, variant_ordering)
damage = get_rankings("damage", frames, variant_ordering)
time = get_rankings("time", frames, variant_ordering)
depth = get_rankings("depth", frames, variant_ordering)

# The order in which the data is supposed to be printed.
ordering = [6, 5, 8, 7, 4, 3, 2, 1, 0, 9]

endings1 = ["100 playouts", "500 playouts", "1000 playouts", "total"]
endings2 = ["p100", "p500", "p1000", "ptot"]

# Print the data.
print_tables(wins, symwins, hull, damage, time, depth, endings1, endings2, frames, ordering)
