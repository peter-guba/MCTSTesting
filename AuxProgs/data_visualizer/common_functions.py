# Common functions used by multiple other files.

# Fetches the number of wins from the given string representation of data.
def get_wins(s):
    return int(float(s[1:s.index('w')]))


# Fetches the confidence bound for wins.
def get_wins_cfb(s):
    return float(s[s.index('w')+3:s.index('wcb')])


# Fetches the number of symwins from the given string representation of data.
def get_symwins(s):
    return int(float(s[s.index('wcb')+5:s.index('sw')]))


# Fetches the confidence bound for wins.
def get_symwins_cfb(s):
    return float(s[s.index('sw')+4:s.index('swcb')])


# Fetches the remaining hull from the given string representation of data.
def get_hull(s):
    return float(s[s.index('swcb')+6:s.index('h')].replace(',', '.'))


# Fetches the confidence bound for wins.
def get_hull_cfb(s):
    return float(s[s.index('h')+3:s.index('hcb')])


# Fetches the damage dealt to the enemy from the given string representation of data.
def get_damage(s):
    return float(s[s.index('hcb')+5:s.index('d')].replace(',', '.'))


# Fetches the confidence bound for wins.
def get_damage_cfb(s):
    return float(s[s.index('d')+3:s.index('dcb')])


# Fetches the number of draws from the given string representation of data.
def get_draws(s):
    return int(float(s[s.index('dcb')+5:s.index('dr')]))


# Fetches the confidence bound for wins.
def get_draws_cfb(s):
    return float(s[s.index('dr')+4:s.index('drcb')])


# Fetches the number of draws from the given string representation of data.
def get_unfinished(s):
    return int(float(s[s.index('drcb')+6:s.index('u')]))


# Fetches the confidence bound for wins.
def get_unfinished_cfb(s):
    return float(s[s.index('u')+3:s.index('ucb')])


# Returns a function which applies the function given in the first parameter to a given line
# of string and the divides the data by a number passed as the second parameter.
def process_data(function, div_factor):
    def f(line):
        fields = line[:-1].split(';')[1:]
        return function(fields[-1]) / div_factor
    return f


# Applies the function passed as the first parameter to all the given data.
def get_data(function, div_factor, lines):
    return list((map(process_data(function, div_factor), lines)))


# Computes the rank of every variant according to the given field.
# The returned  result is given sorted according to the indices passed
# in the second parameter.
def get_rankings(field, frames, variant_ordering):
    rankings = []

    for frame in frames:
        ranks = frame[field].rank(ascending=False)
        r = []
        for v_index in variant_ordering:
            r.append(ranks[v_index])
        rankings.append(r)

    return rankings


# Converts the names of variants in the files that store data about their
# time and depth into a different format.
def convert_name(name):
    # WP MCTS used to be called AP MCTS, so that's the reason the "ap_" prefix
    # is present here.
    if name.startswith("ap_") or name.startswith("wp_"):
        return "WP"
    elif name.startswith("basic_"):
        return "UCT"
    elif name.startswith("fap_"):
        return "FAP"
    elif name.startswith("mcts_"):
        return "HP"
    elif name.startswith("q_bonus_"):
        return "QB"
    elif name.startswith("r_bonus_"):
        return "RB"
    elif name.startswith("rq_bonus_"):
        return "RQB"
    elif name.startswith("sigmoid_"):
        return "Sig"
    elif name.startswith("sr_cr_") or name.startswith("simple_"):
        return "SR"
    elif name.startswith("ucb_"):
        return "U-T"
    elif name.startswith("voi_"):
        return "VOI"

    print(name)
    raise Exception("bad name")


def print_tables(wins, symwins, hull, damage, time, depth, endings1, endings2, frames, ordering):
    for i in range(len(wins)):
        # Print main metric data.

        print("""\
\\begin{{table}}
\\begin{{tabular}}{{ |l||c|c|c|c|c|c|c|c|c|c|  }}
\\hline
\\multicolumn{{11}}{{|c|}}{{primary metric data {x}}} \\\\
\\hline
&UCT&SR&VOI&U-T&Sig&RB&QB&HP&FAP&WP\\\\
\\hline""".format(x=endings1[i]))

        line = "wins"
        for o in ordering:
            line += "&" + str(round(frames[i]["wins"][o], 2))
        print(line + "\\\\")

        line = "symwins"
        for o in ordering:
            line += "&" + str(round(frames[i]["symwins"][o], 2))
        print(line + "\\\\")

        line = "hull"
        for o in ordering:
            line += "&" + str(round(frames[i]["hull"][o], 2))
        print(line + "\\\\")

        line = "damage"
        for o in ordering:
            line += "&" + str(round(frames[i]["damage"][o], 2))
        print(line + "\\\\")

        print("""\
\\hline
\\end{{tabular}}
\\label{{tbl:data{x}}}
\\end{{table}}""".format(x=endings2[i]))

        print()

        # Print main metric rankings.

        print("""\
\\begin{{table}}
\\begin{{tabular}}{{ |l||c|c|c|c|c|c|c|c|c|c|  }}
\\hline
\\multicolumn{{11}}{{|c|}}{{primary metric ranks {x}}} \\\\
\\hline
&UCT&SR&VOI&U-T&Sig&RB&QB&HP&FAP&WP\\\\
\\hline""".format(x=endings1[i]))

        line = "wins"
        for o in ordering:
            line += "&" + str(wins[i][o])
        print(line + "\\\\")

        line = "symwins"
        for o in ordering:
            line += "&" + str(symwins[i][o])
        print(line + "\\\\")

        line = "hull"
        for o in ordering:
            line += "&" + str(hull[i][o])
        print(line + "\\\\")

        line = "damage"
        for o in ordering:
            line += "&" + str(damage[i][o])
        print(line + "\\\\")

        print("\\hline")

        line = "avg rank"
        for o in ordering:
            line += "&" + str((wins[i][o] + symwins[i][o] + hull[i][o] + damage[i][o]) / 4)
        print(line + "\\\\")

        print("""\
\\hline
\\end{{tabular}}
\\label{{tbl:rank{x}}}
\\end{{table}}""".format(x=endings2[i]))

        print()

        # Print time and depth data.

        print("""\
\\begin{{table}}
\\begin{{tabular}}{{ |l||c|c|c|c|c|c|c|c|c|c|  }}
\\hline
\\multicolumn{{11}}{{|c|}}{{secondary metric data {x}}} \\\\
\\hline
&UCT&SR&VOI&U-T&Sig&RB&QB&HP&FAP&WP\\\\
\\hline""".format(x=endings1[i]))

        line = "time"
        for o in ordering:
            line += "&" + str(round(-1.0 * frames[i]["time"][o], 2))
        print(line + "\\\\")

        line = "depth"
        for o in ordering:
            line += "&" + str(round(frames[i]["depth"][o], 2))
        print(line + "\\\\")

        print("""\
\\hline
\\end{{tabular}}
\\label{{tbl:tddata{x}}}
\\end{{table}}""".format(x=endings2[i]))

        print()

        # Print time and depth rankings.

        print("""\
\\begin{{table}}
\\begin{{tabular}}{{ |l||c|c|c|c|c|c|c|c|c|c|  }}
\\hline
\\multicolumn{{11}}{{|c|}}{{secondary metric ranks {x}}} \\\\
\\hline
&UCT&SR&VOI&U-T&Sig&RB&QB&HP&FAP&WP\\\\
\\hline""".format(x=endings1[i]))

        line = "time"
        for o in ordering:
            line += "&" + str(time[i][o])
        print(line + "\\\\")

        line = "depth"
        for o in ordering:
            line += "&" + str(depth[i][o])
        print(line + "\\\\")

        print("""\
\\hline
\\end{{tabular}}
\\label{{tbl:tdranks{x}}}
\\end{{table}}""".format(x=endings2[i]))

        print()
        print()
        print()
