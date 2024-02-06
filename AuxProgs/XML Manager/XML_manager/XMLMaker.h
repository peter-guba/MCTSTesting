#ifndef XMLMAKER_H_
#define XMLMAKER_H_

#include <string>
#include <fstream>
#include <vector>
#include <sstream>
#include <regex>
#include <filesystem>

/// <summary>
/// A class that takes care of creating, deleting and altering XML files.
/// </summary>
class XMLMaker {
public:

	/// <summary>
	/// Creates an xml file corresponding to an AI from a file containing the implementation of a class.
	/// </summary>
	/// <param name="filePath"> The path to the file to be processed. </param>
	/// <param name="toExclude"> A list of parameters that aren't supposed to be included in the xml file. </param>
	/// <param name="permittedTypes"> A list of C# variable types. If a variable has one of these types,
	/// it can be converted into a parameter in the xml file. </param>
	void MakeAI(
		const std::string& filePath,
		const std::vector<std::string>& toExclude,
		const std::vector<std::string>& permittedTypes,
		const std::vector<std::string>& scripts
	);

	/// <summary>
	/// Mixes a number xml AI files into one mixed AI file. The given files have to contain definitions of basic
	/// AIs, not mixed AIs.
	/// </summary>
	/// <param name="toMix"> The files to mix. </param>
	/// <param name="fileName"> The name of the created file. </param>
	/// <param name="showEndMessage"> Determines whether a message should be printned when the method finishes. </param>
	void MakeMixedAI(const std::vector<std::string>& toMix, const std::string& fileName, bool showEndMessage = true);

	/// <summary>
	/// Creates mixes from the combinations given in a file the path to which is passed as the filePath parameter.
	/// It then also creates benchmarks where it pits these mixes against their constituent parts and one benchmark
	/// set where all these benchmarks are present.
	/// </summary>
	void MakeMixesAndBenchmarks(const std::filesystem::path& filePath);

	/// <summary>
	/// Creates a benchmark from the two given files.
	/// </summary>
	void MakeBenchmark(const std::string& fileName1, const std::string& fileName2);

	/// <summary>
	/// Creates a benchmark from the two given files without prompting the user for additional parameters.
	/// </summary>
	void MakeBenchmark(
		const std::string& fileName1,
		const std::string& fileName2,
		std::ofstream& output,
		const std::string& maxRounds,
		const std::string& isSymmetric,
		const std::string& repeats,
		bool printMessage
	);
	
	/// <summary>
	/// Combines all the matched files into all possible benchmarks.
	/// </summary>
	/// <param name="toCombine"></param>
	void MakeAllBenchmarks(const std::vector<std::regex>& toCombine);

	/// <summary>
	/// Creates a benchmark set from all the files that mach the given regexes.
	/// </summary>
	void MakeBenchmarkSet(const std::vector<std::regex>& toInclude);

	/// <summary>
	/// Creates a benchmark set for every pair of algorithms from the AIs folder.
	/// </summary>
	void MakeAllBenchmarkSets();

	/// <summary>
	/// Deletes all the files in directory 'dir' that correspond to the given regexes.
	/// </summary>
	void DeleteFiles(const std::vector<std::regex>& toDelete, const std::string& dir);

	/// <summary>
	/// Changes the given value in all the the files in directory 'dir' that match the given regexes.
	/// </summary>
	void ChangeValue(
		const std::vector<std::regex>& toChange,
		std::string valueName,
		const std::string& newValue,
		const std::string& dir,
		bool isAttribute,
		int occurence
	);

	void SetHomeDirectory(const std::filesystem::path& directoryPath);

	void SetScriptsDirectory(const std::filesystem::path& directoryPath);

private:

	/// <summary>
	/// Fetches the name of a class and its parameters from text.
	/// </summary>
	/// <param name="s"> The text that to process. </param>
	/// <param name="toExclude"> A list of parameters that aren't supposed to be included in the output. </param>
	/// <param name="permittedTypes"> A list of C# variable types. If a variable has one these types, it can be present in the output. </param>
	/// <returns></returns>
	std::vector<std::string> GetClassNameAndParameters(
		const std::string& s,
		const std::vector<std::string>& toExclude,
		const std::vector<std::string>& permittedTypes
	);

	bool ChangeSingleFile(
		const std::filesystem::path& filePath,
		const std::string& valueName,
		const std::string& newValue,
		bool isAttribute,
		int occurence
	);

	/// <summary>
	/// Check whether a file with the given name doesn't already exist. If it does, it asks the user
	/// if he/she wants to overwrite it.
	/// </summary>
	void OverwriteCheck(std::filesystem::path& filePath);

	/// <summary>
	/// Checks if the output stream was successfully opened. If not, prompts the user to
	/// enter a different file name.
	/// </summary>
	/// <param name="output"></param>
	/// <param name="dir"></param>
	void InvalidNameCheck(std::ofstream& output, const std::string& dir);

	int BinomialCoefficient(int n, int k);

	/// <summary>
	/// A header used at the start of all the xml files.
	/// </summary>
	const std::string header = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>";

	/// <summary>
	/// The path to the home directory.
	/// </summary>
	std::filesystem::path home_directory;

	/// <summary>
	/// The path to he directory where scripts from which AIs are supposed to be made are stored.
	/// </summary>
	std::filesystem::path scripts_directory;
};

#endif
