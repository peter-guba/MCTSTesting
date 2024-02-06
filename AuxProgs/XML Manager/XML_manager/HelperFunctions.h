#ifndef HELPER_FUNCTIONS_H_
#define HELPER_FUNCTIONS_H_

#ifdef _WIN32
#include <windows.h>
#endif

#include <iostream>
#include <string>

inline void ChangeTextToWhite() {
#ifdef _WIN32
	HANDLE hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
	SetConsoleTextAttribute(hConsole, 15);
#endif
}

inline void ChangeTextToGrey() {
#ifdef _WIN32
	HANDLE hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
	SetConsoleTextAttribute(hConsole, 7);
#endif
}

inline void PrintGreenText(const std::string& msg) {
#ifdef _WIN32
	HANDLE hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
	SetConsoleTextAttribute(hConsole, 10);
#endif

	std::cout << msg << std::endl;

#ifdef _WIN32
	SetConsoleTextAttribute(hConsole, 7);
#endif
}

inline void PrintRedText(const std::string& msg) {
#ifdef _WIN32
	HANDLE hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
	SetConsoleTextAttribute(hConsole, 12);
#endif

	std::cout << msg << std::endl;

#ifdef _WIN32
	SetConsoleTextAttribute(hConsole, 7);
#endif
}

inline void PrintYellowText(const std::string& msg) {
#ifdef _WIN32
	HANDLE hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
	SetConsoleTextAttribute(hConsole, 14);
#endif

	std::cout << msg << std::endl;

#ifdef _WIN32
	SetConsoleTextAttribute(hConsole, 7);
#endif
}

inline void PrintWhiteText(const std::string& msg) {
#ifdef _WIN32
	HANDLE hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
	SetConsoleTextAttribute(hConsole, 15);
#endif

	std::cout << msg << std::endl;

#ifdef _WIN32
	SetConsoleTextAttribute(hConsole, 7);
#endif
}

#endif
