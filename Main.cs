//Textracter
//Textracter will extract words from any type of data. Supports unicode sources.
//Developper contact: firexware@hotmail.com

//Created by...
//CRACKSTATION.NET
//	-Free MD5 and other hash Cracking
//	-Best wordlists & dictonaries on the net, for free download.
//	-Newer versions of this code
//	-visit http://crackstation.net
//This code is in the public domain, but please keep the credits to crackstation.net attached :)

using System;
using System.IO;
using System.Text;

namespace textracter
{
	class MainClass
	{
		
		const string SET_SPACE = " ";
		const string SET_ALPHA_LOW = "abcdefghijklmnopqrstuvwxyz";
		const string SET_ALPHA_HIGH = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		const string SET_NUMERIC = "0123456789";
		
		const string SET_SYMBOLS = "~`!@#$%^&*()-_=+{[}]\\|:;\"'<,>.?/";
		
		public static void Main (string[] args)
		{
			TextWriter output;
			//Defaults
			string usrMinLen = "3";
			string usrMaxLen = "64";
			output  = Console.Out;
			
			bool toLower = false;
			bool toUpper = false;
			bool original = false; //needs post processing to make original true if both tolower and toupper is false
			bool verbose = false;
			string userInclude = "";
			string userExclude = "";
			string userFileName = "";
			
			//Process command line arguments
			for(int i = 0; i < args.Length; i++)
			{
				if(args[i] == "--help")
				{
					PrintUsage("");
					return;
				}
				else if(args[i] == "-v")
				{
					verbose = true;
				}
				else if(args[i] == "-o")
				{
					if(i+1 < args.Length)
					{
						FileStream fs = new FileStream(args[i + 1], FileMode.Create, FileAccess.Write);
						output = new StreamWriter(fs);
					}
					else
					{
						PrintUsage("Invalid or missing argument to -o");	
						return;
					}
					i++;
				}
				else if(args[i] == "--minlen")
				{
					if(i+1 < args.Length)
					{
						usrMinLen = args[i+1];
					}
					else
					{
						PrintUsage("Missing argumentfor '--minlen'");
						return;
					}
					i++;
				}
				else if(args[i] == "--maxlen")
				{
					if(i+1 < args.Length)
					{
						usrMaxLen = args[i+1];
					}
					else
					{
						PrintUsage("Missing argumentfor '--maxlen'");
						return;
					}
					i++;
				}
				else if(args[i] == "--tolower")
				{
					toLower = true;
				}
				else if(args[i] == "--keeporiginal")
				{
					original = true;
				}
				else if(args[i] == "--toupper")
				{
					toUpper = true;
				}
				else if(args[i] == "--includeset")
				{
					if(i+1 < args.Length)
					{
						if(userInclude != "unicode")
						{
							bool valid = true;
							userInclude = GetSet(userInclude, args[i+1], ref valid);
							if(!valid)
							{
								PrintUsage("Argument to --includeset was invalid.");
								return;
							}
						}
					}
					else
					{
						PrintUsage("Missing argumentfor '--includeset'");
						return;
					}
					i++;
				}
				else if(args[i] == "--includechars")
				{
					if(i+1 < args.Length)
					{
						if(userInclude != "unicode")
						{
							userInclude += args[i+1];
						}
					}
					else
					{
						PrintUsage("Missing argumentfor '--includechars'");
						return;
					}
					i++;
				}
				else if(args[i] == "--excludeset") //for unicode only
				{
					if(i+1 < args.Length)
					{
						bool valid = true;
						if(args[i+1] == "unicode")
						{
							PrintUsage("Can't exclude unicode.");
							return;
						}
						userExclude = GetSet(userExclude, args[i+1], ref valid);
						if(!valid)
						{
							PrintUsage("Argument to --excludeset was invalid.");
							return;
						}
					}
					else
					{
						PrintUsage("Missing argumentfor '--excludeset'");
						return;
					}
					i++;
				}
				else if(args[i] == "--excludechars")
				{
					if(i+1 < args.Length)
					{
						userExclude += args[i+1];
					}
					else
					{
						PrintUsage("Missing argument for --excludechars");	
					}
					i++;
				}
				else
				{
					if(File.Exists(args[i]))
					{
						userFileName = args[i];	
					}
					else
					{
						PrintUsage("Invalid filename or mis-spelled command line argument");
						return;
					}
				}
			}
			
			//Post processing and defaults:
			
			if(string.IsNullOrEmpty(userInclude))
			{
				userInclude = SET_ALPHA_HIGH + SET_ALPHA_LOW + SET_NUMERIC;	
			}
			
			if(userInclude == "unicode" && string.IsNullOrEmpty(userExclude))
			{
					userExclude = " ";
			}
			
			//This check is also done later, but it doesn't hurt to do it explicitly.
			if(toLower == false && toUpper == false)
			{
				original = true;
			}
			
			int maxLen = 0;
			int minLen = 0;
			if(!int.TryParse(usrMaxLen, out maxLen) || !int.TryParse(usrMinLen, out minLen))
			{
				PrintUsage("minlen or maxlen argument isn't a number.");
			}
			
			Process(minLen,maxLen, userInclude, userExclude, toLower, toUpper, original, userFileName, verbose, output);
			output.Close();
		}
		
		//Process
		//	Takes data from a file or stdin and creates a wordlist which is output on stdout.
		//minLen
		//	Minimum word length to be outputted
		//maxLen
		//	Maximum word length to be outputted
		//include
		//	List of characters that valid words can be made from. "unicode" for all characters.
		//exclude
		//	List of characters that valid word's CAN'T contain.
		//toLower
		//	Output all words in lowercase ONLY.
		//toUpper
		//	Output all words in UPPERCASE ONLY.
		//original
		//	Output original case as well when toLower or toUpper is set
		//filename
		//	The name of file to use as data source. stdin is used if filename == ""
		//output
		//	A textreader to output the words on
		private static void Process(int minLen, int maxLen, string include, string exclude, 
		                            bool toLower, bool toUpper, bool original, string fileName, bool verbose, TextWriter output)
		{
			TextReader toProcess;
			bool unicode = include == "unicode";
			bool[] includeTable = Optimize(include);
			bool[] excludeTable = Optimize(exclude);
			if(string.IsNullOrEmpty(fileName))
			{
				toProcess = Console.In;
			}
			else
			{
				toProcess = new StreamReader(fileName);
			}
			
			long lineCount = 0;
			string origLine = "";
			do //Loop through each line of our input
			{
				origLine = toProcess.ReadLine();
				ExtractWords(ref origLine, minLen, maxLen, unicode, ref includeTable, ref excludeTable, toLower, toUpper, original, output);
				lineCount++;
				if(verbose && lineCount % 1000000 == 0)
				{
					Console.Error.WriteLine("Finished " + lineCount.ToString() + " lines of source data.");	
				}
			} while(toProcess.Peek() > -1);
			toProcess.Close();
		}
		
		//ExtractWords
		//	Extracts all of the valid words from a string of data and outputs them.
		//data
		//	The data
		//minLen
		//	Minimum word length.
		//maxLen
		//	Maximum word length.
		//unicode
		//	True if all characters are allowed
		//include
		//	bool array where index is ascii value, value is true if allowed
		//exclude
		//  boo array where index is ascii value, value is true to exclude
		//toLower
		//	All output is made lowercase
		//toUpper
		//	All output is made UPPERCASE
		//original
		// Original case is kept when toUpper or toLower are set
		//output
		//	A textreader to output the words on
		private static void ExtractWords(ref string data, int minLen, int maxLen, 
		                                 bool unicode, ref bool[] include, ref bool[] exclude, 
		                                 bool toLower, bool toUpper, bool original, TextWriter output)
		{
			int start = 0;
			int length = 0;

			for(int i = 0; i < data.Length; i++)
			{
				start = i;
				length = 0;
				
				//When we find a character that is valid, keep going until the next INvalid character.
				//We end up with the starting position and the length, so we can extract the sequence of valid characters once we reach the next INvalid character
				while(i < data.Length && 
				      !(data[i] <= 31 || data[i] == 127) && //Not a non-printable ASCII character
				      !(data[i] < 128 && exclude[data[i]]) && //Not explicitly excluded
				      (unicode || (data[i] < 128 && include[data[i]]))) //It must either be in the allowed set, or unicode (meaning all characters that aren't excluded or non-print ascii are allowed)
				{
					length++;
					i++;
				}
				
				//Now extract the sequence of valid characters
				if(length > 0 && length >= minLen && length <= maxLen)
				{
					Output(output, data.Substring(start,length),toLower, toUpper, original);
				}
			}
		}
		
		//Output
		//	Outputs a word to the console in the specified case.
		//output
		//	The textreader to output the words on.
		//toLower
		//	Make word lowercase then output.
		//toUpper
		//	Make word uppercase then output.
		//keepOrig
		//	Output in original case when toLower and toUpper are set.
		private static void Output(TextWriter output, string word, bool toLower, bool toUpper, bool keepOrig)
		{
			string low = "";
			string up = "";
			
			if(toLower)
			{
				low = word.ToLower();
				output.WriteLine(low);	
			}
			
			if(toUpper)
			{
				up = word.ToUpper();
				if(up != low) //Only output uppercase if toLower hasn't already output it.
				{
					output.WriteLine(up);
				}
			}
			
			if(!toLower && !toUpper || keepOrig)
			{
				if(word != low && word != up) //Only output the original if both toUpper toLower havn't already output it
				{
					output.WriteLine(word);	
				}
			}
		}
		
		//Creates a table where the index is the ASCII value of the character
		//The value of the bool at that index is TRUE when the character was found in toUnique
		private static bool[] Optimize(string toUnique)
		{
			bool[] table = new bool[128];
			foreach(char c in toUnique)
			{
				if(c<128)
				{
					table[c] = true;	
				}
			}
			return table;
		}
		
		//Prints the usage statement to STDERR
		//message is the error message to show, "" for no error.
		private static void PrintUsage(string message)
		{
			if(!string.IsNullOrEmpty(message))
			{
				Console.Error.WriteLine("ERROR: " + message + "\n");
			}

			Console.Error.WriteLine("Usage: textracter [options] [filename]\n");
			Console.Error.WriteLine("STDIN will be used if no filename is specified.\nIf no options are specified, only alphanumeric strings longer than 3 characters and shorter than 64 characters are printed.\n");
			Console.Error.WriteLine("Options:");
			Console.Error.WriteLine("--minlen           Minimum word length.");
			Console.Error.WriteLine("--maxlen           Maximum word length.");
			Console.Error.WriteLine("--tolower          Make all words lowercase.");
			Console.Error.WriteLine("--toupper          Make all words UPPERcase.");
			Console.Error.WriteLine("--keeporiginal     Print original case also when --tolower or --toupper is used.");
			Console.Error.WriteLine("--includeset <o>   Specify a character set to include in output. \n                     Options are: alpha, alphalow, alphahigh, space, numeric, \n                     symbols, alphanumeric, ascii, unicode. \n                     Can be used multiple times.");
			Console.Error.WriteLine("--excludeset <o>   Specify a character set to exclude from output.\n                     See --includeset for options.");
			Console.Error.WriteLine("--includechars <o> Includes the following characters in output.");
			Console.Error.WriteLine("--excludechars <o> Excludes the following characters from output.");
			Console.Error.WriteLine("-v                 Show # of lines processed every million lines on stderr.");
			Console.Error.WriteLine("-o <file>          Output file. This is MUCH faster than piping to file.");
			
			Console.Error.WriteLine("\n\nExamples:\n");
			Console.Error.WriteLine("Get all alphanumeric strings:\n        textracter file.dat > wordlist.lst\n");
			Console.Error.WriteLine("Get all alpha and symbol strings:\n        textracter --includeset alpha --includeset symbols file.dat > words.lst\n");
			Console.Error.WriteLine("Make all words lowercase:\n        textracter --tolower file.dat > wordlist.lst\n");
			Console.Error.WriteLine("Make all words lowercase and keep original case (output both):\n        textracter --tolower --keeporiginal file.dat > wordlist.lst\n");
			Console.Error.WriteLine("Print all characters except for symbols:\n        textracter --includeset unicode --excludeset symbols file.dat > wrds.lst\n");
			Console.Error.WriteLine("Print only words made up of 'a', 'b' and 'c':\n        textracter --includechars abc file.dat > wordlist.lst\n");
		}	
		
		//Processes user input to see if they chose a valid set.
		//Returns the users choice added to the original set.
		//Will return "unicode" if original set is "unicode" or user choice is "unicode"
		private static string GetSet(string oldSet, string setname, ref bool valid)
		{
			if(oldSet == "unicode")
			{
				return "unicode";	
			}
			
			switch(setname)
			{
			case "alphalow":
				return oldSet + SET_ALPHA_LOW;
			case "alphahigh":
				return oldSet + SET_ALPHA_HIGH;
			case "alpha":
				return oldSet + SET_ALPHA_HIGH + SET_ALPHA_LOW ;
			case "space":
				return oldSet + SET_SPACE;
			case "numeric":
				return oldSet + SET_NUMERIC;
			case "symbols":
				return oldSet + SET_SYMBOLS;
			case "alphanumeric":
				return oldSet + SET_ALPHA_HIGH + SET_ALPHA_LOW + SET_NUMERIC;
			case "ascii":
				return oldSet + SET_ALPHA_HIGH + SET_ALPHA_LOW + SET_NUMERIC + SET_SYMBOLS;
			case "unicode":
				return "unicode";
			default:
				valid = false;
				return oldSet;
			}
		}
	}
}
