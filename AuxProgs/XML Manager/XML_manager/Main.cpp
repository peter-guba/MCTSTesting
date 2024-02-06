// This is a program meant for creating AI, Benchmark and BenchmarkSet configuration files.

#include <vector>
#include <string>
#include <regex>
#include <iostream>
#include <filesystem>
#include "XMLMaker.h"
#include "CommandProcessor.h"
#include "HelperFunctions.h"

#ifdef _WIN32
#include <windows.h>
#endif

namespace fs = std::filesystem;
using namespace std;

/// <summary>
/// Splits a string at every occurence of the separator character.
/// </summary>
vector<string> SplitString(string& s, char separator) {
	vector<string> result;

	size_t split_pos = s.find(separator);

	while (split_pos != string::npos) {
		result.push_back(s.substr(0, split_pos));
		s = s.substr(split_pos + 1);
		split_pos = s.find(separator);
	}

	result.push_back(s);
	
	return result;
}

/// <summary>
/// Tries to read the home directory and the scripts directory form the config file.
/// If the file isn't found or the paths contained in it can't be accessed, it returns
/// false. Otherwise, it loads the locations of the two directories and returns true.
/// </summary>
/// <param name="cmdp"> The command processor that is used by this
/// program to process the user's input. </param>
bool ReadConfigFile(CommandProcessor& cmdp) {
	ifstream config("config.txt");

	// If the config file cannot be opened, return false.
	if (config.fail()) {
		return false;
	}

	// The rest of the code checks if the data provided in the config file is valid and
	// if so, loads it.
	string line;
	getline(config, line);

	if (!fs::exists(line.substr(6))) {
		config.close();
		return false;
	}

	if (!cmdp.CheckDirectoryStructure(line.substr(6))) {
		config.close();
		return false;
	}

	cmdp.GetMaker().SetHomeDirectory(line.substr(6));

	getline(config, line);

	if (!fs::exists(line.substr(9))) {
		config.close();
		return false;
	}
	cmdp.GetMaker().SetScriptsDirectory(line.substr(9));

	config.close();
	return true;
}

/// <summary>
/// Creates a config file with the supplied home and scripts directory paths.
/// </summary>
void MakeConfigFile(const string& home, const string& scripts) {
	ofstream config("config.txt");

	config << "home: " << home << endl;
	config << "scripts: " << scripts << endl;

	config.close();
}

int main() {
	ChangeTextToGrey();

	CommandProcessor cmdp;
	string line;

	// Try loading data from the config file. If it doesn't work, prompt the user
	// to enter paths to the home and scripts directories and create a new config file.
	if (!ReadConfigFile(cmdp)) {
		string home, scripts;
		
		PrintWhiteText("Enter home directory path:");
		getline(cin, home);

		bool allSet = false;

		while (!allSet) {
			while (!fs::exists(home)) {
				if (home == "quit") {
					exit(0);
				}

				PrintRedText("The entered directory doesn't exist, please enter a different path.");
				getline(cin, home);
			}

			allSet = true;

			if (!cmdp.CheckDirectoryStructure(home)) {
				allSet = false;
				PrintWhiteText("Enter a different directory name:");
				getline(cin, home);
			}
		}

		PrintWhiteText("Enter scripts directory path:");
		getline(cin, scripts);

		while (!fs::exists(scripts)) {
			if (scripts == "quit") {
				exit(0);
			}

			PrintRedText("The entered directory doesn't exist, please enter a different path.");
			getline(cin, scripts);
		}

		MakeConfigFile(home, scripts);
		ReadConfigFile(cmdp);
	}

	// Keep processing the user's input.
	while (true) {
		getline(cin, line);
		cmdp.ProcessCommand(SplitString(line, ' '));
	}
}
