# Creates latex tables that give the average values and the rankings of the different algorithms with respect to the average number of wins,
# symwins, the average amount of remaining hull, average amount of damage dealt to the enemy, average time and average depth. The tables are
# created for various combat settings.

import pandas as pd
from operator import add
from common_functions import *


# This hasn't been adapted as it's not clear that you'll use it.


# The number by which the average number of wins is supposed to be divided.
wins_divisor = 1

# The number by which the average number of symwins is supposed to be divided.
symwins_divisor = 1

# The number by which the remaining hull is supposed to be divided.
hull_divisor = 6 * 3

# The number by which the amount of damage dealt to the enemy is supposed to be divided.
damage_divisor = 6 * 3

# Path to the files which contain data about the number of wins, symwins, hull and damage for the
# different combat settings.
file = ""

# Path to the files that contain data about time and depth for the different combat settings.
tdfile = ""

# A reordering applied to time and depth data, as the files that contain these values store them in a different
# order than the other files.
reorder = [2, 3, 4, 5, 6, 7, 1, 8, 9, 0]

# A list in which all the created dataframes are stored.
frames = []

# Loads data for each of the battle settings.

data4v4 = open(file + "4v4_2D-2B.csv")
data8v8 = open(file + "8v8_4D-4B.csv")
data16v16 = open(file + "16v16_8D-8B.csv")
data32v32 = open(file + "32v32_16D-16B.csv")
data48v48 = open(file + "48v48_24D-24B.csv")
data64v64 = open(file + "64v64_32D-32B.csv")

tddata4v4 = open(tdfile + "_4v4.csv")
tddata8v8 = open(tdfile + "_8v8.csv")
tddata16v16 = open(tdfile + "_16v16.csv")
tddata32v32 = open(tdfile + "_32v32.csv")
tddata48v48 = open(tdfile + "_48v48.csv")
tddata64v64 = open(tdfile + "_64v64.csv")
tddatat = open(tdfile + ".csv")

names = data4v4.readline().split(';')[1:]

# Create pandas dataframes for all the settings.

lines = data4v4.readlines()
tdlines = list(map(lambda l: l.split(";"), tddata4v4.readlines()))[1:]
df4v4 = pd.DataFrame({
    "names": names,
    "wins": get_data(get_wins, wins_divisor, lines),
    "symwins": get_data(get_symwins, symwins_divisor, lines),
    "hull": get_data(get_hull, 4 * hull_divisor, lines),
    "damage": get_data(get_damage, 4 * damage_divisor, lines),
    "time": [[-float(item[1]) for item in tdlines][i] for i in reorder],
    "depth": [[float(item[3]) for item in tdlines][i] for i in reorder]
})
frames.append(df4v4)

lines = data8v8.readlines()[1:]
tdlines = list(map(lambda l: l.split(";"), tddata8v8.readlines()))[1:]
df8v8 = pd.DataFrame({
    "names": names,
    "wins": get_data(get_wins, wins_divisor, lines),
    "symwins": get_data(get_symwins, symwins_divisor, lines),
    "hull": get_data(get_hull, 8 * hull_divisor, lines),
    "damage": get_data(get_damage, 8 * damage_divisor, lines),
    "time": [[-float(item[1]) for item in tdlines][i] for i in reorder],
    "depth": [[float(item[3]) for item in tdlines][i] for i in reorder]
})
frames.append(df8v8)

lines = data16v16.readlines()[1:]
tdlines = list(map(lambda l: l.split(";"), tddata16v16.readlines()))[1:]
df16v16 = pd.DataFrame({
    "names": names,
    "wins": get_data(get_wins, wins_divisor, lines),
    "symwins": get_data(get_symwins, symwins_divisor, lines),
    "hull": get_data(get_hull, 16 * hull_divisor, lines),
    "damage": get_data(get_damage, 16 * damage_divisor, lines),
    "time": [[-float(item[1]) for item in tdlines][i] for i in reorder],
    "depth": [[float(item[3]) for item in tdlines][i] for i in reorder]
})
frames.append(df16v16)

lines = data32v32.readlines()[1:]
tdlines = list(map(lambda l: l.split(";"), tddata32v32.readlines()))[1:]
df32v32 = pd.DataFrame({
    "names": names,
    "wins": get_data(get_wins, wins_divisor, lines),
    "symwins": get_data(get_symwins, symwins_divisor, lines),
    "hull": get_data(get_hull, 32 * hull_divisor, lines),
    "damage": get_data(get_damage, 32 * damage_divisor, lines),
    "time": [[-float(item[1]) for item in tdlines][i] for i in reorder],
    "depth": [[float(item[3]) for item in tdlines][i] for i in reorder]
})
frames.append(df32v32)

lines = data48v48.readlines()[1:]
tdlines = list(map(lambda l: l.split(";"), tddata48v48.readlines()))[1:]
df48v48 = pd.DataFrame({
    "names": names,
    "wins": get_data(get_wins, wins_divisor, lines),
    "symwins": get_data(get_symwins, symwins_divisor, lines),
    "hull": get_data(get_hull, 48 * hull_divisor, lines),
    "damage": get_data(get_damage, 48 * damage_divisor, lines),
    "time": [[-float(item[1]) for item in tdlines][i] for i in reorder],
    "depth": [[float(item[3]) for item in tdlines][i] for i in reorder]
})
frames.append(df48v48)

lines = data64v64.readlines()[1:]
tdlines = list(map(lambda l: l.split(";"), tddata64v64.readlines()))[1:]
df64v64 = pd.DataFrame({
    "names": names,
    "wins": get_data(get_wins, wins_divisor, lines),
    "symwins": get_data(get_symwins, symwins_divisor, lines),
    "hull": get_data(get_hull, 64 * hull_divisor, lines),
    "damage": get_data(get_damage, 64 * damage_divisor, lines),
    "time": [[-float(item[1]) for item in tdlines][i] for i in reorder],
    "depth": [[float(item[3]) for item in tdlines][i] for i in reorder]
})
frames.append(df64v64)

tdlines = list(map(lambda l: l.split(";"), tddatat.readlines()))[1:]
dft = pd.DataFrame({
    "names": df4v4["names"],
    "wins": [x / 6 for x in map(add, map(add, map(add, map(add, map(add, df4v4["wins"], df8v8["wins"]), df16v16["wins"]), df32v32["wins"]), df48v48["wins"]), df64v64["wins"])],
    "symwins": [x / 6 for x in map(add, map(add, map(add, map(add, map(add, df4v4["symwins"], df8v8["symwins"]), df16v16["symwins"]), df32v32["symwins"]), df48v48["symwins"]), df64v64["symwins"])],
    "hull": [x / 6 for x in map(add, map(add, map(add, map(add, map(add, df4v4["hull"], df8v8["hull"]), df16v16["hull"]), df32v32["hull"]), df48v48["hull"]), df64v64["hull"])],
    "damage": [x / 6 for x in map(add, map(add, map(add, map(add, map(add, df4v4["damage"], df8v8["damage"]), df16v16["damage"]), df32v32["damage"]), df48v48["damage"]), df64v64["damage"])],
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

endings1 = ["4 vs 4", "8 vs 8", "16 vs 16", "32 vs 32", "48 vs 48", "64 vs 64", "total"]
endings2 = ["b4v4", "b8v8", "b16v16", "b32v32", "b48v48", "b64v64", "btot"]

# Print the data.
print_tables(wins, symwins, hull, damage, time, depth, endings1, endings2, frames, ordering)
