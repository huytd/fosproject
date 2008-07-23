// Copyright (C) 2006-2008 NeoAxis Group
#define _CRT_SECURE_NO_WARNINGS
#include <windows.h>
#include <psapi.h>
#include <stdlib.h>
#include <malloc.h>
#include <memory.h>
#include <tchar.h>
#include <string>

#pragma comment (lib, "psapi.lib")

typedef int mono_main(int argc, char* argv[]);
typedef void mono_set_dirs(const char* assembly_dir, const char* config_dir);

std::wstring GetModuleFullDirectory()
{
	TCHAR moduleFileNameTemp[MAX_PATH + 1];
	GetModuleFileName(NULL, moduleFileNameTemp, sizeof(moduleFileNameTemp) / sizeof(TCHAR));

	std::wstring moduleFileName = moduleFileNameTemp;

	int index1 = (int)moduleFileName.find_last_of(_T("\\"));
	int index2 = (int)moduleFileName.find_last_of(_T("/"));
	int index = index1 > index2 ? index1 : index2;
	if(index == -1)
		return _T("");

	return moduleFileName.substr(0, index);
}

std::wstring GetModuleBaseName()
{
	HANDLE hProcess = GetCurrentProcess();
	HMODULE hModule = GetModuleHandle( NULL );

	TCHAR moduleBaseName[MAX_PATH + 1];
	memset(moduleBaseName, 0, sizeof(moduleBaseName));
	GetModuleBaseName( hProcess, hModule, moduleBaseName, sizeof(moduleBaseName) / sizeof(TCHAR) );
	return std::wstring(moduleBaseName);
}

std::string ToAnsiString(const std::wstring& str)
{
	std::string result;
	char* temp = new char[str.length() + 1];
	wcstombs( temp, str.c_str(), str.length() + 1 );
	result = temp;
	delete temp;
	return result;
}

std::wstring ToUnicodeString(const std::string& str)
{
	std::wstring result;
	TCHAR* temp = new TCHAR[str.length() + 1];
	mbstowcs( temp, str.c_str(), str.length() + 1 );
	result = temp;
	delete temp;
	return result;
}

std::wstring GetDestinationFileName()
{
	std::wstring baseName;

	//get from file name of this executable
	{
		std::wstring moduleBaseName = GetModuleBaseName();

		std::wstring moduleBaseNameLower = moduleBaseName;
		for(int n = 0; n < (int)moduleBaseNameLower.length(); n++)
			moduleBaseNameLower[n] = tolower(moduleBaseNameLower[n]);

		int index = (int)moduleBaseNameLower.find(_T("_mono"));
		if(index == -1)
		{
			MessageBox(0, _T("Invalid executable file name.\n\nDemands file name in format \"{destination base file name}_mono[any characters].exe\"."), 
				_T("Mono launcher error"), 
				MB_OK | MB_ICONEXCLAMATION);
			return _T("");
		}

		baseName = moduleBaseName.substr(0, index);
	}

	return GetModuleFullDirectory() + _T("\\") + baseName + _T(".exe");
}

int APIENTRY _tWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPTSTR lpCmdLine, int nCmdShow)
{
	std::wstring monoRuntimeFullPath = GetModuleFullDirectory() + _T("\\MonoRuntime");

	HMODULE hMonoDllModule = NULL;

	//load mono dlls
	{
		TCHAR* monoRuntimeDlls[] =
		{
			//in loading order
			_T("iconv.dll"),
			_T("intl.dll"),
			_T("libglib-2.0-0.dll"),
			_T("libgthread-2.0-0.dll"),
			_T("mono.dll"),
		};

		for(int n = 0; n < sizeof(monoRuntimeDlls) / sizeof(char*); n++)
		{
			std::wstring dllName = monoRuntimeDlls[n];

			std::wstring dllFullPath = monoRuntimeFullPath + std::wstring(_T("\\bin\\")) + dllName;

			HMODULE hDllModule = LoadLibrary( dllFullPath.c_str() );

			if(!hDllModule)
			{
				TCHAR error[MAX_PATH + 50];
				wsprintf(error, _T("Loading \"%s\" failed."), dllFullPath.c_str());
				MessageBox(0, error,  _T("Mono launcher error"), MB_OK | MB_ICONEXCLAMATION);
				return -1;
			}

			if(dllName == std::wstring(_T("mono.dll")))
				hMonoDllModule = hDllModule;
		}
	}

	mono_main* monoMainFunction = (mono_main*)GetProcAddress( hMonoDllModule, "mono_main" );
	if(!monoMainFunction)
	{
		MessageBox(0, _T("No \"mono_main\" procedure."), _T("Mono launcher error"), 
			MB_OK | MB_ICONEXCLAMATION);
		return -1;
	}

	mono_set_dirs* monoSetDirsFunction = (mono_set_dirs*)GetProcAddress( 
		hMonoDllModule, "mono_set_dirs" );
	if(!monoSetDirsFunction)
	{
		MessageBox(0, _T("No \"mono_set_dirs\" procedure."), _T("Mono launcher error"), 
			MB_OK | MB_ICONEXCLAMATION);
		return -1;
	}

	std::wstring destinationFileName = GetDestinationFileName();
	if(destinationFileName.empty())
		return -1;
	std::string destinationFileNameAnsi = ToAnsiString(destinationFileName);

	//it's will be modified
	char* lpCmdLineTemp = new char[wcslen(lpCmdLine) + 1];
	memset(lpCmdLineTemp, 0, wcslen(lpCmdLine) + 1);
	wcstombs(lpCmdLineTemp, lpCmdLine, wcslen(lpCmdLine));

	int argc = 0;
	char* argv[256];
	{
		argv[argc] = "none";
		argc++;

		argv[argc] = (char*)destinationFileNameAnsi.c_str();
		argc++;

		//parse windows command line
		char* cmdPointer = lpCmdLineTemp;
		while(*cmdPointer && argc < 256)
		{
			while(*cmdPointer && *cmdPointer <= ' ')
				cmdPointer++;
			if(*cmdPointer)
			{
				argv[argc++] = cmdPointer;
				while(*cmdPointer && *cmdPointer > ' ')
					cmdPointer++;
				if(*cmdPointer) 
					*(cmdPointer++) = 0;
			}
		}
	}

	//run mono

	std::string monoLibPathAnsi = ToAnsiString(monoRuntimeFullPath + _T("\\lib"));
	std::string monoEtcPathAnsi = ToAnsiString(monoRuntimeFullPath + _T("\\etc"));
	//1 - path to "lib" directory
	//2 - config file path (by default in the mono path to "etc" directory)
	monoSetDirsFunction(monoLibPathAnsi.c_str(), monoEtcPathAnsi.c_str());

	int result = monoMainFunction(argc, argv);

	delete[] lpCmdLineTemp;

	return result;
}
