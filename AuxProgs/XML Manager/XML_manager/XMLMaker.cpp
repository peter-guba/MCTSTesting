#include <string>
#include <fstream>
#include <vector>
#include <sstream>
#include <regex>
#include <iostream>
#include <algorithm>
#include <filesystem>
#include <utility>
#include "XMLMaker.h"
#include "HelperFunctions.h"

namespace fs = std::filesystem;
using namespace std;

void XMLMaker::MakeAI(
	const string& fileName,
	const vector<string>& toExclude,
	const vector<string>& permittedTypes,
	const vector<string>& scripts
) {
	ifstream input(scripts_directory / (fileName + ".cs"));

	if (input.fail()) {
		PrintRedText("The specified file couldn't be opened.");
		return;
	}

	stringstream ss;
	ss << input.rdbuf();

	vector<string> data = GetClassNameAndParameters(ss.str(), toExclude, permittedTypes);

	// Allow the user to set the values of all the parameters.
	string line;
	for (auto it = (data.begin() + 1); it != data.end(); ++it) {
		PrintWhiteText("What value should argument " + *it + " have?");
		getline(cin, line);
		*it = *it + "=\"" + line + '\"';
	}

	PrintWhiteText("What should the file be called?");
	getline(cin, line);
	fs::path p = home_directory / "AIs" / (line + ".xml");

	OverwriteCheck(p);
	ofstream output(p);
	InvalidNameCheck(output, "AIs");

	output << header << endl;
	output << "<AI>" << endl;
	output << " <" << data[0] << endl;
	
	for (auto it = (data.begin() + 1); it != data.end(); ++it) {
		output << "  " << *it << endl;
	}

	output << " >" << endl;

	output << endl;

	for (auto it = scripts.begin(); it != scripts.end(); ++it) {
		output << "  <Script>" << *it << "</Script>" << endl;
	}

	output << endl;

	output << "</" << data[0] << '>' << endl;
	output << "</AI>" << endl;

	PrintGreenText("File created successfully.");
}

void XMLMaker::MakeMixedAI(const vector<string>& toMix, const string& fileName, bool showEndMessage) {
	for (auto it = toMix.begin(); it != toMix.end(); ++it) {
		if (!fs::exists(home_directory / "AIs" / (*it + ".xml"))) {
			PrintRedText("The file " + *it + ".xml doesn't exist.");
			return;
		}
	}

	int max_playouts = -1;
	int playout_round_limit = -1;
	bool max_playouts_set_manually = false;
	bool playout_round_limit_set_manually = false;
	vector<string> scripts;
	vector<vector<string>> parameters;
	vector<string> algorithm_names;

	string line = "";
	for (auto it = toMix.begin(); it != toMix.end(); ++it) {
		ifstream input(home_directory / "AIs" / (*it + ".xml"));
		vector<string> current_params;
		
		getline(input, line); // header
		getline(input, line); // <AI>
		getline(input, line); // name of the algorithm

		algorithm_names.push_back(line);

		getline(input, line);
		while (line.find('>') == string::npos) {
			if (line.rfind("  MaxPlayouts=", 0) == 0) {
				if (max_playouts == -1) {
					max_playouts = stoi(line.substr(sizeof("  MaxPlayouts=\"") - 1, line.length() - 3));
				}
				else {
					if (!max_playouts_set_manually && stoi(line.substr(sizeof("  MaxPlayouts=\"") - 1, line.length() - 3)) != max_playouts) {
						PrintWhiteText("The MaxPlayouts values in the files differ. What should the maximum number of playouts be?");
						getline(cin, line);
						max_playouts = stoi(line);
						max_playouts_set_manually = true;
					}
				}

				current_params.push_back("  MaxPlayouts=\"-1\"");
			}
			else if (line.rfind("  PlayoutRoundLimit=", 0) == 0) {
				if (playout_round_limit == -1) {
					playout_round_limit = stoi(line.substr(sizeof("  PlayoutRoundLimit=\"") - 1, line.length() - 3));
				}
				else {
					if (!playout_round_limit_set_manually && stoi(line.substr(sizeof("  PlayoutRoundLimit=\"") - 1, line.length() - 3)) != playout_round_limit) {
						PrintWhiteText("The PlayoutRoundLimit values in the files differ. What should the maximum number of rounds in a playout be?");
						getline(cin, line);
						playout_round_limit = stoi(line);
						playout_round_limit_set_manually = true;
					}
				}

				current_params.push_back("  PlayoutRoundLimit=\"-1\"");
			}
			else {
				current_params.push_back(line);
			}

			getline(input, line);
		}

		parameters.push_back(current_params);

		getline(input, line); // blank line
		getline(input, line);
		
		// Read the scripts.
		vector<string> temp_scripts;
		while (line.rfind("  <Script>", 0) == 0) {
			temp_scripts.push_back(line);
			getline(input, line);
		}

		// If the scripts vector already contains some scripts, check if they
		// are the same. If not, ask the user what to specify them.
		if (!scripts.empty()) {
			for (int i = 0; i < scripts.size(); ++i) {
				if (scripts.at(i).compare(temp_scripts.at(i)) != 0) {
					PrintWhiteText("The scripts in the given files differ. What scripts should be used?");
					
					scripts.clear();
					while (true) {
						getline(cin, line);

						if (line.compare("q") == 0) {
							break;
						}
						else {
							scripts.push_back("  <Script>" + line + "</Script>");
						}
					}
				}
			}
		}
		else {
			scripts = temp_scripts;
		}
	}

	fs::path p = home_directory / "AIs" / (fileName + ".xml");

	OverwriteCheck(p);
	ofstream output(p);
	InvalidNameCheck(output, "AIs");

	output << header << endl;
	output << "<AI>" << endl;
	output << " <MixMcts" << endl;
	output << "  MaxPlayouts=\"" << max_playouts << "\"" << endl;
	output << "  PlayoutRoundLimit=\"" << playout_round_limit << "\"" << endl;
	output << " >" << endl;

	output << endl;

	for (auto it = scripts.begin(); it != scripts.end(); ++it) {
		output << *it << endl;
	}

	output << endl;

	for (int i = 0; i < algorithm_names.size(); ++i) {
		output << "  <Constituent>" << endl;
		output << "   " << algorithm_names.at(i) << endl;
		for (auto it = parameters.at(i).begin(); it < parameters.at(i).end(); ++it) {
			output << "  " << *it << endl;
		}
		output << "    />" << endl;
		output << "  </Constituent>" << endl;
	}

	output << endl;

	output << "</MixMcts>" << endl;
	output << "</AI>" << endl;

	if (showEndMessage) {
		PrintGreenText("File created successfully.");
	}
}

void XMLMaker::MakeMixesAndBenchmarks(const filesystem::path& filePath) {
	int num_of_ai_files_created = 0;
	int num_of_benchmark_files_created = 0;
	string max_rounds, is_symmetric, repeats;
	string bSetName;

	PrintWhiteText("Enter MaxRounds value:");
	getline(cin, max_rounds);

	PrintWhiteText("Enter IsSymmetric value:");
	getline(cin, is_symmetric);

	PrintWhiteText("Enter Repeats value:");
	getline(cin, repeats);

	PrintWhiteText("Enter resulting benchmark set name:");
	getline(cin, bSetName);

	fs::path bSetPath = home_directory / "BenchmarkSets" / (bSetName + ".xml");

	OverwriteCheck(bSetPath);
	ofstream bSetOutput(bSetPath);
	InvalidNameCheck(bSetOutput, "BenchmarkSets");

	bSetOutput << header << endl;
	bSetOutput << "<BenchmarkSet>" << endl;
	
	if (!fs::exists(filePath)) {
		PrintRedText("The input file " + filePath.string() + " doesn't exits.");
		return;
	}

	ifstream input(filePath);
	string line;

	while (getline(input, line)) {
		vector<string> toMix;
		std::stringstream ss(line);
		std::string item;
		string fileNameStart = "Mix";
		string fileNameParameters = "";
		string numberOfPlayouts = "";

		// Split the line by space character and store the individual items.
		// Also compose the name of the mix file.
		while (getline(ss, item, ' ')) {
			if (!fs::exists(home_directory / "AIs" / (item + ".xml"))) {
				PrintRedText("The file " + item + ".xml doesn't exist.");
				return;
			}

			toMix.push_back(item);

			// If the name of the variant starts with MCTS...
			if (item.rfind("MCTS", 0) == 0) {
				fileNameStart += item.substr(sizeof("MCTS") - 1, item.find('_', sizeof("MCTS_") - 1) - (sizeof("MCTS") - 1));
				fileNameParameters += item.substr(item.find('_', sizeof("MCTS_") - 1) + 1, item.rfind('_') - (item.find('_', sizeof("MCTS_") - 1)));
			}
			else {
				fileNameStart += '_' + item.substr(0, item.find("MCTS") - 1);
				fileNameParameters += item.substr(item.find("MCTS") + 4, item.rfind('_') - (item.find("MCTS") + 4));
			}

			if (numberOfPlayouts.compare("") == 0) {
				numberOfPlayouts = item.substr(item.rfind('_'));
			}
			else {
				if (numberOfPlayouts.compare(item.substr(item.rfind('_'))) != 0) {
					PrintRedText("The number of playouts in the given files differ.");
					return;
				}
			}
		}

		string fileName = fileNameStart + "_MCTS" + fileNameParameters + numberOfPlayouts;

		MakeMixedAI(toMix, fileName, false);
		++num_of_ai_files_created;

		for (auto it = toMix.begin(); it != toMix.end(); ++it) {
			fs::path p = home_directory / "Benchmarks" / (fileName + "_vs_" + *it + ".xml");

			OverwriteCheck(p);
			ofstream output(p);
			InvalidNameCheck(output, "Benchmarks");

			MakeBenchmark(fileName, *it, output, max_rounds, is_symmetric, repeats, false);
			++num_of_benchmark_files_created;

			bSetOutput << " <Benchmark Id=\"" << fileName + "_vs_" + *it << "\"/>" << endl;
		}
	}

	bSetOutput << "</BenchmarkSet>" << endl;

	PrintGreenText(
		to_string(num_of_ai_files_created) +
		" AI files, " +
		to_string(num_of_benchmark_files_created) +
		" benchmark files and 1 benchmark set were created."
	);
}

// Unfinished
/*void XMLMaker::MixAll(int mix_quota, const vector<regex>& toCombine) {
	string file = "";
	int num_of_files_created = 0;
	vector<string> file_names;

	for (const auto& entry : fs::directory_iterator(home_directory / "AIs")) {
		file = entry.path().filename().string();
		for (auto it = toCombine.begin(); it != toCombine.end(); ++it) {
			if (regex_match(file, *it)) {
				file_names.push_back(file.substr(0, file.size() - 4));
				break;
			}
		}
	}


	int counter = BinomialCoefficient(file_names.size(), mix_quota);
	while (counter != 0) {
		string current_file_name = "";



		fs::path p = home_directory / "AIs" / current_file_name;


	}



	for (int i = 0; i < file_names.size(); ++i) {
		for (int j = i + 1; j < file_names.size(); ++j) {
			// Unless such a file already exists, the file name will be a composition of the names
			// of the two input files and it will be saved into the Benchmarks folder.
			fs::path p = home_directory / "Benchmarks" / (file_names[i] + "_vs_" + file_names[j] + ".xml");

			OverwriteCheck(p);
			ofstream output(p);
			InvalidNameCheck(output, "Benchmarks");

			output << header << endl;
			output << "<Benchmark>" << endl;
			output << " <MaxRounds>" << max_rounds << "</MaxRounds>" << endl;
			output << " <IsSymmetric>" << is_symmetric << "</IsSymmetric>" << endl;
			output << " <Repeats>" << repeats << "</Repeats>" << endl;
			output << endl;

			output << " <Player Index=\"0\">" << endl;
			output << "  <AIRef Id=\"" + file_names[i] + "\" />" << endl;
			output << " </Player>" << endl;
			output << endl;

			output << " <Player Index=\"1\">" << endl;
			output << "  <AIRef Id=\"" + file_names[j] + "\" />" << endl;
			output << " </Player>" << endl;
			output << endl;

			output << " <BattleSet Id=\"BasicBattleSet\" />" << endl;
			output << "</Benchmark>" << endl;

			++num_of_files_created;
		}
	}

	PrintGreenText(to_string(num_of_files_created) + " AI files were created.");
}*/

void XMLMaker::MakeBenchmark(const string& fileName1, const string& fileName2) {
	if (!fs::exists(home_directory / "AIs" / (fileName1 + ".xml")) || !fs::exists(home_directory / "AIs" / (fileName2 + ".xml"))) {
		PrintRedText("At least one of the entered AI files doesn't exist.");
		return;
	}

	// Unless such a file already exists, the file name will be a composition of the names
	// of the two input files and it will be saved into the Benchmarks folder.
	fs::path p = home_directory / "Benchmarks" / (fileName1 + "_vs_" + fileName2 + ".xml");

	OverwriteCheck(p);
	ofstream output(p);
	InvalidNameCheck(output, "Benchmarks");

	string line;

	PrintWhiteText("Enter MaxRounds value:");
	getline(cin, line);
	string maxRounds = line;

	PrintWhiteText("Enter IsSymmetric value:");
	getline(cin, line);
	string isSymmetric = line;

	PrintWhiteText("Enter Repeats value:");
	getline(cin, line);
	string repeats = line;

	MakeBenchmark(fileName1, fileName2, output, maxRounds, isSymmetric, repeats, true);
}

void XMLMaker::MakeBenchmark(
	const string& fileName1,
	const string& fileName2,
	ofstream& output,
	const string& maxRounds,
	const string& isSymmetric,
	const string& repeats,
	bool printMessage
) {
	output << header << endl;
	output << "<Benchmark>" << endl;

	output << " <MaxRounds>" << maxRounds << "</MaxRounds>" << endl;
	output << " <IsSymmetric>" << isSymmetric << "</IsSymmetric>" << endl;
	output << " <Repeats>" << repeats << "</Repeats>" << endl;
	output << endl;

	output << " <Player Index=\"0\">" << endl;
	output << "  <AIRef Id=\"" + fileName1 + "\" />" << endl;
	output << " </Player>" << endl;
	output << endl;

	output << " <Player Index=\"1\">" << endl;
	output << "  <AIRef Id=\"" + fileName2 + "\" />" << endl;
	output << " </Player>" << endl;
	output << endl;

	output << " <BattleSet Id=\"BasicBattleSet\" />" << endl;
	output << "</Benchmark>" << endl;

	if (printMessage) {
		PrintGreenText("File created successfully.");
	}
}

void XMLMaker::MakeAllBenchmarks(const std::vector<std::regex>& toCombine) {
	string file = "";
	int num_of_files_created = 0;
	vector<string> file_names;

	for (const auto& entry : fs::directory_iterator(home_directory / "AIs")) {
		file = entry.path().filename().string();
		for (auto it = toCombine.begin(); it != toCombine.end(); ++it) {
			if (regex_match(file, *it)) {
				file_names.push_back(file.substr(0, file.size() - 4));
				break;
			}
		}
	}

	string max_rounds, is_symmetric, repeats;

	PrintWhiteText("Enter MaxRounds value:");
	getline(cin, max_rounds);

	PrintWhiteText("Enter IsSymmetric value:");
	getline(cin, is_symmetric);

	PrintWhiteText("Enter Repeats value:");
	getline(cin, repeats);

	for (int i = 0; i < file_names.size(); ++i) {
		for (int j = i + 1; j < file_names.size(); ++j) {
			// Unless such a file already exists, the file name will be a composition of the names
			// of the two input files and it will be saved into the Benchmarks folder.
			fs::path p = home_directory / "Benchmarks" / (file_names[i] + "_vs_" + file_names[j] + ".xml");

			OverwriteCheck(p);
			ofstream output(p);
			InvalidNameCheck(output, "Benchmarks");

			MakeBenchmark(file_names[i], file_names[j], output, max_rounds, is_symmetric, repeats, false);

			++num_of_files_created;
		}
	}

	PrintGreenText(to_string(num_of_files_created) + " benchmark files were created.");
}

void XMLMaker::MakeBenchmarkSet(const vector<regex>& toInclude) {
	string line;
	
	PrintWhiteText("Enter benchmark set name:");
	getline(cin, line);
	fs::path p = home_directory / "BenchmarkSets" / (line + ".xml");

	OverwriteCheck(p);

	ofstream output(p);

	InvalidNameCheck(output, "BenchmarkSets");

	output << header << endl;
	output << "<BenchmarkSet>" << endl;

	string file = "";

	// Go through all the files, check if they match any of the regexes and if so, add them to the output.
	for (const auto& entry : fs::directory_iterator(home_directory / "Benchmarks")) {
		file = entry.path().filename().string();
		for (auto it = toInclude.begin(); it != toInclude.end(); ++it) {
			if (regex_match(file, *it)) {
				output << " <Benchmark Id=\"" << file.substr(0, file.length() - 4) << "\"/>" << endl;
				break;
			}
		}
	}

	output << "</BenchmarkSet>" << endl;

	PrintGreenText("File created successfully.");
}

void XMLMaker::MakeAllBenchmarkSets() {
	vector<string> names;
	string file, ai_name;
	int num_of_files = 0;

	// Go through all the AI specifications and gather their names.
	for (const auto& entry : fs::directory_iterator(home_directory / "AIs")) {
		file = entry.path().filename().string();

		if (file.find("MCTS") != string::npos) {
			ai_name = file.substr(0, file.find_last_of('_'));

			// If the ai name corresponds to the name convention and the list doesn't already contain the name.
			if (file != ai_name && find(names.begin(), names.end(), ai_name) == names.end()) {
				names.push_back(ai_name);
			}
		}
	}

	for (int i = 0; i < names.size(); ++i) {
		for (int j = i + 1; j < names.size(); ++j) {
			regex r1(names[i] + "_([0-9]*)_vs_" + names[j] + "_([0-9]*)\.xml");
			regex r2(names[j] + "_([0-9]*)_vs_" + names[i] + "_([0-9]*)\.xml");
			fs::path p = home_directory / "BenchmarkSets" / (names[i] + "_vs_" + names[j] + ".xml");

			vector<string> benchmarks;

			for (const auto& entry : fs::directory_iterator(home_directory / "Benchmarks")) {
				file = entry.path().filename().string();
				if (regex_match(file, r1) || regex_match(file, r2)) {
					benchmarks.push_back(file.substr(0, file.length() - 4));
				}
			}

			if (benchmarks.size() != 0) {
				OverwriteCheck(p);
				ofstream output(p);
				InvalidNameCheck(output, "BenchmarkSets");

				output << header << endl;
				output << "<BenchmarkSet>" << endl;

				for (const auto& bmrk : benchmarks) {
					output << " <Benchmark Id=\"" << bmrk << "\"/>" << endl;
				}

				output << "</BenchmarkSet>" << endl;

				++num_of_files;
			}
		}
	}

	PrintGreenText(to_string(num_of_files) + " files were created.");
}

void XMLMaker::DeleteFiles(const vector<regex>& toDelete, const string& dir) {
	string file = "";
	int succesful_files_count = 0;
	int unsuccesful_files_count = 0;

	// Go through all the files in the specified directory, check if they match any of the
	// regexes and if so, delete them.
	for (const auto& entry : fs::directory_iterator(home_directory / dir)) {
		file = entry.path().filename().string();
		for (auto it = toDelete.begin(); it != toDelete.end(); ++it) {
			if (regex_match(file, *it)) {
				if (fs::remove(entry.path())) {
					++succesful_files_count;
				}
				else {
					++unsuccesful_files_count;
				}
				break;
			}
		}
	}

	PrintGreenText(to_string(succesful_files_count) + " files deleted successfully.");
	PrintRedText(to_string(unsuccesful_files_count) + " files failed to delete.");
}

void XMLMaker::ChangeValue(const vector<regex>& toChange, string valueName, const string& newValue, const string& dir, bool isAttribute, int occurence) {
	string file = "";
	int succesful_files_count = 0;
	int unsuccesful_files_count = 0;

	if (isAttribute) {
		valueName = valueName + "=\"";
	}
	else {
		valueName = '<' + valueName + '>';
	}

	// Go through all the files in the specified directory, check if they match any of the
	// regexes and if so, try to change the specified value in them.
	for (const auto& entry : fs::directory_iterator(home_directory / dir)) {
		file = entry.path().filename().string();
		for (auto it = toChange.begin(); it != toChange.end(); ++it) {
			if (regex_match(file, *it)) {
				if (ChangeSingleFile(entry.path(), valueName, newValue, isAttribute, occurence)) {
					++succesful_files_count;
				}
				else {
					++unsuccesful_files_count;
				}
				break;
			}
		}
	}

	PrintGreenText(to_string(succesful_files_count) + " files changed successfully.");
	PrintRedText(to_string(unsuccesful_files_count) + " files failed to change.");
}

void XMLMaker::SetHomeDirectory(const fs::path& directoryPath) {
	home_directory = directoryPath;
}

void XMLMaker::SetScriptsDirectory(const fs::path& directoryPath) {
	scripts_directory = directoryPath;
}

vector<string> XMLMaker::GetClassNameAndParameters(const string& fileContent, const vector<string>& toExclude, const vector<string>& permittedTypes) {
	vector<string> data;
	smatch m;

	// Find the class name in the file.
	regex class_name_regex("class (\\w+)");
	regex_search(fileContent, m, class_name_regex);

	string class_name = m[1].str();
	data.push_back(class_name);

	// Using the class name, find the constructor.
	regex constructor_regex("public " + class_name + "[\\s]?\\(([^)]+)\\)");
	regex_search(fileContent, m, constructor_regex);

	// Create a stream from the found parameters.
	stringstream parameters(m[1].str());
	string current = "";

	// For every parameter, determine if it is supposed to be included in the output and if so,
	// add it to the 'data' variable.
	while (parameters >> current) {
		// If the type of of the parameter is permitted...
		if (count(permittedTypes.begin(), permittedTypes.end(), current) != 0) {
			parameters >> current;

			// If the parameter isn't supposed to be excluded, save it to the 'data' variable.
			if (count(toExclude.begin(), toExclude.end(), current.substr(0, current.length() - 1)) == 0) {
				// Remove a trailing comma if there is one.
				if (current[current.length() - 1] == ',') {
					current = current.substr(0, current.length() - 1);
				}
				current[0] = toupper(current[0]);
				data.push_back(current);
			}
		}
		// Otherwise, skip the parameter name by loading two words in a row
		// (once here and another time at the start of the while cycle).
		else {
			parameters >> current;
		}
	}

	return data;
}

bool XMLMaker::ChangeSingleFile(const fs::path& filePath, const string& valueName, const string& newValue, bool isAttribute, int occurence) {
	// First store the entire file in a buffer, then close the ifstream and open
	// on ofstream, thereby deleting the contents of the file. I tried to do this with
	// an fstream but it turned out that I couldn't easily change the size of the data,
	// so if I changed the value 100 to 5 for example, the remaining bytes would be padded
	// by zeros.
	ifstream input(filePath);

	if (input.fail()) {
		return false;
	}

	stringstream ss;
	ss << input.rdbuf();
	input.close();

	ofstream output(filePath);

	int linePos;
	int currentOccurence = 0;
	bool lineFound = false;
	int size = 0;
	
	// Search the file line by line.
	for (string line; getline(ss, line);) {

		// If you find the name of the value that is supposed to be changed...
		if (!lineFound && (linePos = line.find(valueName)) < line.size()) {
			++currentOccurence;

			// Find the next occurence of the value name on the line until you either get to the occurence specified in the
			// parameters or you get to the end of the line. There shouldn't ever be two references to the same value on
			// a single row but I decided to include this just in case.
			while (currentOccurence != occurence && (linePos = line.find(valueName, linePos + 1)) < line.size()) {
				++currentOccurence;
			}

			// If the requested occurence has been reached...
			if (currentOccurence == occurence) {
				lineFound = true;

				// Find out the size of the text that is to be changed by finding the position where the
				// current value ends.
				if (isAttribute) {
					size = line.find('\"', linePos + valueName.size()) - linePos - valueName.size();
					line = line.substr(0, linePos + valueName.size()) + newValue + line.substr(linePos + valueName.size() + size);
				}
				else {
					size = line.find("</", linePos + valueName.size()) - linePos - valueName.size();
					line = line.substr(0, linePos + valueName.size()) + newValue + line.substr(linePos + valueName.size() + size);
				}
			}
		}

		output << line << endl;
	}

	if (lineFound) {
		return true;
	}
	else {
		return false;
	}
}

void XMLMaker::OverwriteCheck(fs::path& filePath) {
	string line;

	while (fs::exists(filePath)) {
		PrintYellowText("A file with this name already exists. Do you want to overwrite it? y/n");
		getline(cin, line);
		while (line != "y" && line != "n") {
			PrintRedText("Learn to read, dumbass. y/n");
			getline(cin, line);
		}

		if (line == "n") {
			PrintWhiteText("Enter a different file name:");
			getline(cin, line);
			filePath = home_directory / "AIs" / (line + ".xml");
		}
		else {
			break;
		}
	}
}

void XMLMaker::InvalidNameCheck(ofstream& output, const string& dir) {
	string line;
	fs::path p;

	while (output.fail()) {
		PrintRedText("File name is invalid, enter a different name:");
		getline(cin, line);
		p = home_directory / dir / (line + ".xml");

		OverwriteCheck(p);
		output.open(p);
	}
}

int XMLMaker::BinomialCoefficient(int n, int k) {
	if (k == 0 || k == n)
		return 1;

	return BinomialCoefficient(n - 1, k - 1) + BinomialCoefficient(n - 1, k);
}
