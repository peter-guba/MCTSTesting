#include <string>
#include <vector>
#include <algorithm>
#include <iostream>
#include <filesystem>
#include <numeric>
#include "CommandProcessor.h"
#include "HelperFunctions.h"

namespace fs = std::filesystem;
using namespace std;

void CommandProcessor::ProcessCommand(const vector<string>& args) {
	if (args[0] == "makeai") {
		ProcessMakeAICmd(args);
	}
	else if (args[0] == "makemix") {
		ProcessMakeMixedAICmd(args);
	}
	else if (args[0] == "makemnb") {
		ProcessMakeMixesAndBmrksCmd(args);
	}
	else if (args[0] == "makebmrk") {
		ProcessMakeBmrkCmd(args);
	}
	else if (args[0] == "makebmrks") {
		ProcessMakeBmrksCmd(args);
	}
	else if (args[0] == "makebset") {
		ProcessMakeBSetCmd(args);
	}
	else if (args[0] == "makebsets") {
		ProcessMakeBSetsCmd(args);
	}
	else if (args[0] == "delete") {
		ProcessDeleteCmd(args);
	}
	else if (args[0] == "change") {
		ProcessChangeCmd(args);
	}
	else if (args[0] == "sethome") {
		SetHomeDirCmd(args);
	}
	else if (args[0] == "setscripts") {
		SetScriptsDirCmd(args);
	}
	else if (args[0] == "help") {
		ChangeTextToWhite();
		cout << "Possible commands are:" << endl;
		cout << "makeai     (Creates an xml file corresponding to an AI.)" << endl;
		cout << "makebmrk   (Creates a benchmark xml file.)" << endl;
		cout << "makebset   (Creates a benchmark set xml file.)" << endl;
		cout << "delete     (Deletes files matched by given regexes.)" << endl;
		cout << "change     (Changes a given value in files matched by given regexes.)" << endl;
		cout << "sethome    (Sets the home directory.)" << endl;
		cout << "setscripts (Sets the scripts directory.)" << endl;
		cout << "help       (You probably already know this one.)" << endl;
		cout << "quit       (Quits the application.)" << endl;
		ChangeTextToGrey();
	}
	else if (args[0] == "quit") {
		exit(0);
	}
	else {
		PrintRedText("Unknown command entered.");
	}
}

XMLMaker& CommandProcessor::GetMaker() {
	return maker;
}

void CommandProcessor::ProcessMakeAICmd(const vector<string>& args) {
	vector<string> separators{ "-e", "-p", "-s" };

	string name = args[1];
	vector<string> toExclude;
	vector<string> permittedTypes;
	vector<string> scripts;

	auto current_pos = args.begin() + 2;

	// Create vectors of variables to exclude, of permitted types and of scripts to use.
	while (current_pos != args.end()) {
		if (*current_pos == "-e") {
			auto end = GetNextPos(args, current_pos, separators);

			toExclude.insert(toExclude.end(), current_pos + 1, end);

			current_pos = end;
		}
		else if (*current_pos == "-p") {
			auto end = GetNextPos(args, current_pos, separators);

			permittedTypes.insert(permittedTypes.end(), current_pos + 1, end);

			current_pos = end;
		}
		else if (*current_pos == "-s") {
			auto end = GetNextPos(args, current_pos, separators);

			scripts.insert(scripts.end(), current_pos + 1, end);

			current_pos = end;
		}
		else {
			BadArgumentError();
			return;
		}
	}

	// If no permitted types were entered, use defaults.
	if (permittedTypes.size() == 0) {
		permittedTypes.emplace_back("int");
		permittedTypes.emplace_back("double");
	}

	maker.MakeAI(name, toExclude, permittedTypes, scripts);
}

void CommandProcessor::ProcessMakeMixedAICmd(const vector<string>& args) {
	vector<string> args_without_command(args.begin() + 1, args.end() - 1);
	maker.MakeMixedAI(args_without_command, args.back());
}

void CommandProcessor::ProcessMakeMixesAndBmrksCmd(const std::vector<std::string>& args) {
	maker.MakeMixesAndBenchmarks(MakePath(args, 1));
}

void CommandProcessor::ProcessMakeBmrkCmd(const vector<string>& args) {
	if (args.size() == 3) {
		maker.MakeBenchmark(args[1], args[2]);
	}
	else {
		IncorrectNumberOfArgsError();
	}
}

void CommandProcessor::ProcessMakeBmrksCmd(const vector<string>& args) {
	if (args.size() > 1) {
		vector<regex> toCombine;
		for (auto it = args.begin() + 1; it != args.end(); ++it) {
			toCombine.emplace_back(*it + "\\.xml");
		}

		maker.MakeAllBenchmarks(toCombine);
	}
	else {
		IncorrectNumberOfArgsError();
	}
}

void CommandProcessor::ProcessMakeBSetCmd(const vector<string>& args) {
	if (args.size() >= 2) {
		vector<regex> toInclude;
		for (auto it = args.begin() + 1; it != args.end(); ++it) {
			toInclude.emplace_back(*it + R"(\.xml)");
		}

		maker.MakeBenchmarkSet(toInclude);
	}
	else {
		IncorrectNumberOfArgsError();
	}
}

void CommandProcessor::ProcessMakeBSetsCmd(const vector<string>& args) {
	if (args.size() == 1) {
		maker.MakeAllBenchmarkSets();
	}
	else {
		IncorrectNumberOfArgsError();
	}
}

void CommandProcessor::ProcessDeleteCmd(const vector<string>& args) {
	if (args.size() >= 3) {
		string dir;

		if (args[1] == "ai") {
			dir = "AIs";
		}
		else if (args[1] == "benchmark") {
			dir = "Benchmarks";
		}
		else if (args[1] == "set") {
			dir = "BenchmarkSets";
		}
		else {
			BadArgumentError();
			return;
		}

		vector<regex> toDelete;
		for (auto it = args.begin() + 2; it != args.end(); ++it) {
			toDelete.emplace_back(*it + "\\.xml");
		}

		maker.DeleteFiles(toDelete, dir);
	}
	else {
		IncorrectNumberOfArgsError();
	}
}

void CommandProcessor::ProcessChangeCmd(const vector<string>& args) {
	if (args.size() < 7) {
		IncorrectNumberOfArgsError();
	}
	else {
		string dir;

		if (args[1] == "ai") {
			dir = "AIs";
		}
		else if (args[1] == "benchmark" || args[1] == "bmrk") {
			dir = "Benchmarks";
		}
		else if (args[1] == "set") {
			dir = "BenchmarkSets";
		}
		else {
			BadArgumentError();
			return;
		}

		bool isAttribute = true;

		if (args[4] == "attr") {
			isAttribute = true;
		}
		else if (args[4] == "elem") {
			isAttribute = false;
		}
		else {
			BadArgumentError();
			return;
		}

		int occurence = 0;

		if (!IsStringNumber(args[5]) || (occurence = stoi(args[5])) <= 0) {
			BadArgumentError();
			return;
		}

		vector<regex> toChange;
		for (auto it = args.begin() + 5; it != args.end(); ++it) {
			toChange.emplace_back(*it + R"(\.xml)");
		}

		maker.ChangeValue(toChange, args[2], args[3], dir, isAttribute, occurence);
	}
}

void CommandProcessor::SetHomeDirCmd(const vector<string>& args) {
	fs::path p = MakePath(args, 1);

	if (!fs::exists(p)) {
		PrintRedText("The given directory doesn't exist.");
		return;
	}

	if (!CheckDirectoryStructure(p)) {
		return;
	}

	// Edit the config file.
	string line;
	ifstream input("config.txt");
	stringstream ss;
	ss << input.rdbuf();
	input.close();

	ofstream output("config.txt");

	output << "home: " << p.string() << endl;
	// Skip the first line.
	getline(ss, line);
	getline(ss, line);
	output << line << endl;

	maker.SetHomeDirectory(p);	

	PrintGreenText("Home directory updated successfully.");
}

void CommandProcessor::SetScriptsDirCmd(const vector<string>& args) {
	fs::path p = MakePath(args, 1);

	if (!fs::exists(p)) {
		PrintRedText("The given directory doesn't exist.");
		return;
	}

	// Edit the config file.
	string line;
	ifstream input("config.txt");
	stringstream ss;
	ss << input.rdbuf();
	input.close();

	ofstream output("config.txt");

	getline(ss, line);
	output << line << endl;
	output << "scripts: " << p.string() << endl;

	maker.SetScriptsDirectory(p);

	PrintGreenText("Scripts directory updated successfully.");
}

void CommandProcessor::BadArgumentError() {
	PrintRedText("The value of one or more of the entered arguments is incorrect.");
}

void CommandProcessor::IncorrectNumberOfArgsError() {
	PrintRedText("Incorrect number of arguments entered.");
}

string CommandProcessor::MakePath(const vector<string>& args, int start) {
	return accumulate(
		args.begin() + start,
		args.end(),
		string(),
		[](string& a, const string& b)
		{
			return a.empty() ? b : a + ' ' + b;
		}
	);
}

bool CommandProcessor::IsStringNumber(const string& s) {
	return s.find_first_not_of("0123456789") == string::npos;
}

vector<string>::const_iterator CommandProcessor::GetNextPos(
	const vector<string>& data,
	const vector<string>::const_iterator& start,
	const vector<string>& separators
) {
	vector<string>::const_iterator best = data.end();

	// Find the next occurence of all the strings in the separators vector and pick
	// the earliest one.
	for (auto sit = separators.begin(); sit != separators.end(); ++sit) {
		auto newPos = find(start + 1, data.end(), *sit);
		if (newPos - data.begin() < best - data.begin()) {
			best = newPos;
		}
	}

	return best;
}

bool CommandProcessor::CheckDirectoryStructure(const fs::path& directoryPath) {
	bool containsAIs = false, containsBenchmarks = false, containsBenchmarkSets = false;

	for (auto const& dir_entry : fs::directory_iterator(directoryPath))
	{
		if (dir_entry.path().filename() == "AIs") {
			containsAIs = true;
		}
		else if (dir_entry.path().filename() == "Benchmarks") {
			containsBenchmarks = true;
		}
		else if (dir_entry.path().filename() == "BenchmarkSets") {
			containsBenchmarkSets = true;
		}
	}

	if (!containsAIs || !containsBenchmarks || !containsBenchmarkSets) {
		PrintYellowText("The given home directory doesn't contain one or more of the necessary subdirectories (AIs, Benchmarks, BenchmarkSets).");
		PrintYellowText("Do you still want to set it as the home directory? y/n");

		string l;
		getline(cin, l);

		while (l != "y" && l != "n") {
			PrintRedText("Learn to read, dumbass. y/n");
			getline(cin, l);
		}

		if (l == "n") {
			return false;
		}
	}

	return true;
}