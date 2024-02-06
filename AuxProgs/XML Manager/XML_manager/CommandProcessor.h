#ifndef COMMAND_PROCESSOR_H_
#define COMMAND_PROCESSOR_H_

#include <vector>
#include <string>
#include <regex>
#include <filesystem>
#include "XMLMaker.h"

/// <summary>
/// A class that takes care of processing commands and forwarding them to the XMLMaker.
/// </summary>
class CommandProcessor {
public:
	void ProcessCommand(const std::vector<std::string>& args);
	XMLMaker& GetMaker();

	/// <summary>
	/// Checks if the given directory contains the AIs, Benchmarks and BenchmarkSets directories.
	/// If not, it informs the user and asks him whether he really wants to use the given path.
	/// </summary>
	bool CheckDirectoryStructure(const std::filesystem::path& directoryPath);
private:
	/// <summary>
	/// All the ProcessSomething methods just process the given arguments,
	/// check them for errors and call the corresponding XMLMaker functions.
	/// </summary>
	void ProcessMakeAICmd(const std::vector<std::string>& args);
	void ProcessMakeMixedAICmd(const std::vector<std::string>& args);
	void ProcessMakeMixesAndBmrksCmd(const std::vector<std::string>& args);
	void ProcessMakeBmrkCmd(const std::vector<std::string>& args);
	void ProcessMakeBmrksCmd(const std::vector<std::string>& args);
	void ProcessMakeBSetCmd(const std::vector<std::string>& args);
	void ProcessMakeBSetsCmd(const std::vector<std::string>& args);
	void ProcessDeleteCmd(const std::vector<std::string>& args);
	void ProcessChangeCmd(const std::vector<std::string>& args);

	/// <summary>
	/// The SetSomething functions set either the home directory or the
	/// scripts directory both in the XMLMaker and in the config file.
	/// </summary>
	void SetHomeDirCmd(const std::vector<std::string>& args);
	void SetScriptsDirCmd(const std::vector<std::string>& args);

	/// <summary>
	/// The error functions print some error message to standard output.
	/// </summary>
	void BadArgumentError();
	void IncorrectNumberOfArgsError();

	/// <summary>
	/// Creates a path by concatenating the given arguments, starting at 'start'.
	/// The arguments are joined by spaces.
	/// </summary>
	std::string MakePath(const std::vector<std::string>& args, int start);

	/// <summary>
	/// Checks if the given string is a number.
	/// </summary>
	bool IsStringNumber(const std::string& s);

	/// <summary>
	/// Returns the next position of the next element in 'data' starting from 'current',
	/// that is equal to one of the separators (or data.end(), if there is no such element).
	/// </summary>
	std::vector<std::string>::const_iterator GetNextPos(
		const std::vector<std::string>& data,
		const std::vector<std::string>::const_iterator& start,
		const std::vector<std::string>& separators
	);

	XMLMaker maker;
};

#endif
