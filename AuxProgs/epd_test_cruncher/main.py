# Used to process epd chess tests and create C# checkmate test specifications from them.

import os

input_dir = "./test files"
output_dir = "./outputs"
num_of_tests = 100


def get_move(fen_move):
    offset = 0
    if fen_move[0].isupper():
        offset = 1

    start_pos = (int(fen_move[1 + offset]) - 1) * 8 + ord(fen_move[0 + offset]) - ord('a')
    end_pos = (int(fen_move[4 + offset]) - 1) * 8 + ord(fen_move[3 + offset]) - ord('a')

    return str(start_pos) + ", " + str(end_pos)


for filename in os.listdir(input_dir):
    f = os.path.join(input_dir, filename)
    if os.path.isfile(f):
        read_stream = open(f, 'r')
        output = open(os.path.join(output_dir, filename + ".txt"), 'w')
        num_of_moves = int(filename[filename.index('.') - 1])

        counter = 0
        line = read_stream.readline()
        while line and counter < num_of_tests:
            try:
                fields = line.split(' ')
                output_line = "            new Setting(\"" + fields[0] + ' ' + fields[1] + ' ' + fields[2] + ' ' + fields[3]
                output_line += "\", " + str(num_of_moves) + ", true, "
                output_line += "new List<Move>()  { new Move(" + get_move(fields[5]) + ") }),\n"

                output.write(output_line)
                counter += 1
                line = read_stream.readline()
            except:
                print(filename)
                print(line)
                print()

