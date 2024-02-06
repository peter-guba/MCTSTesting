# Generates rundom map coordinates. Originally meant for Children of the Galaxy battle specifications,
# but also used for microRTS.

import random

# The number of battleships.
num_of_b = 8

# The number of destroyers.
num_of_d = 8

# The q coordinate of the center point.
centerQ = 0

# The r coordinate of the center point.
centerR = 0

# The distance by which the units are offset from the centre.
offset = 14

radius = 6

# A list in which used pairs of coordinates are stored. This ensures that two units aren't placed on the same hex.
used_q_r = [(0, 0)]

output1 = ""
output2 = ""
output3 = ""

# Generate positions for battleships.
for _ in range(num_of_b):
    q = 0
    r = 0

    # The last two constraints were arbitrarily set to achieve the kinds of position distributions
    # we wanted.
    while (q, r) in used_q_r or q + r > radius:
        q = centerQ + random.randint(-radius, radius)
        r = centerR + random.randint(-radius, radius)

    used_q_r.append((q, r))

    output1 += "\t  <Unit Id=\"battleship_0\" Q=\"{}\" R=\"{}\" />\n".format(q - offset, r)
    output2 += "          <AddUnit Name=\"Battleship\" PlayerIndex=\"1\" FactoryItemId=\"5\" Rank=\"1\" Q=\"{}\" R=\"{}\"/>\n".format(q - offset, r)
    output3 += "          <AddUnit Name=\"Battleship\" PlayerIndex=\"2\" FactoryItemId=\"5\" Rank=\"1\" Q=\"{}\" R=\"{}\"/>\n".format(-q + offset, -r)

# Generate positions for destroyers.
for _ in range(num_of_d):
    q = 0
    r = 0

    # The last two constraints were arbitrarily set to achieve the kinds of position distributions
    # we wanted.
    while (q, r) in used_q_r or q + r > radius:
        q = centerQ + random.randint(-radius, radius)
        r = centerR + random.randint(-radius, radius)

    used_q_r.append((q, r))

    output1 += "\t  <Unit Id=\"destroyer_0\" Q=\"{}\" R=\"{}\" />\n".format(q - offset, r)
    output2 += "          <AddUnit Name=\"Destroyer\" PlayerIndex=\"1\" FactoryItemId=\"6\" Rank=\"1\" Q=\"{}\" R=\"{}\"/>\n".format(q - offset, r)
    output3 += "          <AddUnit Name=\"Destroyer\" PlayerIndex=\"2\" FactoryItemId=\"6\" Rank=\"1\" Q=\"{}\" R=\"{}\"/>\n".format(-q + offset, -r)

print(output1)
print(output2)
print(output3)
