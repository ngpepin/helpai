/*
 * HelpAI Command Line Tool
 * -------------------------
 * Filename: helpai.cs
 *
 * This tool is a command line interface for interacting with an AI language model,
 * specifically designed to work with the 'llm' command (and, as configured, the 'mistral-7b-openorca' model).
 *
 * Key Features:
 *  - Maintains a history of user prompts and AI responses to provide context for subsequent interactions.
 *  - Supports commands to reset history and enable debug mode.
 *  - Dynamically generates a Markdown file from the dialogue history for easy reading and sharing.
 *  - Handles special characters and ensures the integrity of the prompts and responses.
 *  - Measures and displays the response time of the AI model.
 *
 * Usage:
 *  - To use the tool, compile this file to a standalone executable for a Linux x64 environment.
 *  - Run the executable from the command line, passing the prompt for the AI as an argument.
 *  - Optional flags include '-r' or '--reset' to reset the history, and '-d' or '--debug' for debug output.
 *
 * Implementation Details:
 *  - The program maintains a history of exchanges (both prompts and responses) in a text file.
 *    This history is prepended to new prompts to provide context to the AI model.
 *  - The history file is trimmed if it exceeds a specified character limit to avoid buffer overflow issues.
 *  - Special characters in the prompts and responses are handled to prevent syntax or format issues.
 *  - The tool also creates a Markdown formatted transcript of the dialogue, with appropriate formatting
 *    for code segments, lists, and headers.
 *
 * Author: [Your Name]
 * Date: [Date of Creation]
 * Version: 1.0
 *
 * Note: Ensure that the 'llm' command and the 'mistral-7b-openorca' model are properly configured and accessible
 * in the environment where this tool is executed.
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

class Program
{
    private const string commandName = "helpai";
    private const string HistoryFile = $".{commandName}-history.txt";
    private const string markdownFile = $"{commandName}-transcript.md";
    private const string Model = "mistral-7b-openorca";
    private const string PastUserPromptLabel = "THIS IS A PAST PROMPT I GAVE YOU:";
    private const string CurrentPromptLabel = "PLEASE RESPOND TO THIS PROMPT NOW:";
    private const string PastAiResponseLabel = "THIS IS A PAST RESPONSE YOU GAVE ME:";
    private const string singleQuote = "\u0027";
    private const string doubleQuote = "\u0022";
    private static bool giveDebug = false;
    private static long maxCharsInHistory = 9000;

    static void Main(string[] args)
    {
        bool shouldReset = args.Contains("-r") || args.Contains("--reset");
        giveDebug = args.Contains("-d") || args.Contains("--debug");
        if (shouldReset)
        {
            ResetHistory();
            args = args.Where(arg => arg != "-r" && arg != "--reset").ToArray();
        }
        if (giveDebug)
        {
            args = args.Where(arg => arg != "-d" && arg != "--debug").ToArray();
        }
        if (args.Length == 0) return;

        // make sure the history is not so long that it will break the LLM's content buffer
        // (set macCharsHistory conservatively so there is cushion to account for the current prompt)
        long numChars = 0;
        long oldnumChars = 0;
        do
        {
            numChars = TrimHistoryFile(HistoryFile, maxCharsInHistory);
            if (numChars == oldnumChars)
            {
                // Trimming isn't getting anywhere so break 
                break;
            }
            else
            {
                oldnumChars = numChars;
            }
        } while (numChars > maxCharsInHistory);

        // Prepare and execure prompt command
        string history = ReadHistory();
        string currentPrompt = FormatPrompt(args);
        string fullPrompt = CurrentPromptLabel + " " + currentPrompt + " " + history;
        string fullCommand = "llm -m " + Model + " " + singleQuote + fullPrompt + singleQuote;
        string response = ExecuteBashCommand(fullCommand);

        // Output response
        Console.WriteLine(response);

        // Add last exchange to history
        AppendToHistory(currentPrompt, response);

        //Generate Markdown of dialog so far
        GenerateMarkdownFromHistory(HistoryFile, markdownFile);

    }


    public static void GenerateMarkdownFromHistory(string historyFilePath, string markdownFilePath)
    {
        string historyContent = File.ReadAllText(historyFilePath);

        // Replace labels with Markdown headers and format content
        string markdownContent = historyContent
            .Replace(PastUserPromptLabel, "<br>\n## My Prompt:\n<br>")
            .Replace(PastAiResponseLabel, "<br>\n## AI Response:\n<br>")
            .Replace("\n", " "); // Remove line breaks

        // Detect and format code segments
        // markdownContent = Regex.Replace(markdownContent, @"(```[a-z]*\n[\s\S]*?\n```)", "<br>\n$1\n<br>", RegexOptions.Multiline);

        // Format unordered lists: Look for *, -, or + followed by a space
        // Insert newlines before and after each list item
        historyContent = Regex.Replace(historyContent, @"([\*\-\+])\s", "<br>\n\n$1 <br>", RegexOptions.Multiline);

        // Format ordered lists: Look for numbers followed by a dot and a space
        // Insert newlines before and after each list item
        historyContent = Regex.Replace(historyContent, @"(\d+\.)\s", "<br>\n\n$1 <br>", RegexOptions.Multiline);

        File.WriteAllText(markdownFilePath, markdownContent);
    }

    public static string HandleSomeCharacters(string input)
    {
        string outString = input.Replace("\r", " ").Replace("\n", " ").Replace("\"", "").Replace(doubleQuote, "").Replace(singleQuote, "");
        return outString;
    }

    static void ResetHistory()
    {
        if (File.Exists(HistoryFile))
        {
            File.Copy(HistoryFile, $"{HistoryFile}.bak", true);
            File.Delete(HistoryFile);

            if (File.Exists(markdownFile))
            {
                File.Copy(markdownFile, $"{markdownFile}.bak", true);
                File.Delete(markdownFile);
            }
        }
    }

    static string ReadHistory()
    {
        return File.Exists(HistoryFile) ? File.ReadAllText(HistoryFile) : "";
    }

    static string FormatPrompt(string[] args)
    {
        string prompt = string.Join(" ", args);
        prompt = HandleSomeCharacters(prompt);
        return prompt;
    }

    public static long TrimHistoryFile(string historyFilePath, long maxLength = 10000)
    {
        if (!File.Exists(historyFilePath))
        {
            // Console.WriteLine("History file does not exist.");
            return 0;
        }

        string historyContent = File.ReadAllText(historyFilePath);

        // Check if the history exceeds the maximum length
        if (historyContent.Length <= maxLength) return historyContent.Length;

        string[] pairs = historyContent.Split(new[] { PastAiResponseLabel }, StringSplitOptions.RemoveEmptyEntries);

        // Remove the oldest pairs until the total length is within the limit
        while (pairs.Length > 1 && string.Join(PastAiResponseLabel, pairs).Length > maxLength)
        {
            pairs = pairs.Skip(1).ToArray();
        }

        // Rejoin the pairs and write back to the file
        string trimmedContent = string.Join(PastAiResponseLabel, pairs);
        File.WriteAllText(historyFilePath, trimmedContent);

        return trimmedContent.Length;
    }

    static void AppendToHistory(string prompt, string response)
    {
        string formattedPrompt = " " + PastUserPromptLabel + " " + prompt;
        string formattedResponse = " " + PastAiResponseLabel + " " + response;
        string formattedWhole = HandleSomeCharacters(formattedPrompt + formattedResponse);
        File.AppendAllText(HistoryFile, formattedWhole);

    }

    static string ExecuteBashCommand(string command)
    {
        string args = "";
        if (giveDebug)
        {
            // Echo the command to console if debug output requested
            args = "-l -c " + doubleQuote + "echo Running command: " + command + doubleQuote;
            var echoProcessInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var echoProcess = Process.Start(echoProcessInfo))
            {
                echoProcess.WaitForExit();
            }
        }

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // Execute LLM command and capture output
        args = "-l -c " + doubleQuote + command + doubleQuote;
        if (giveDebug)
        {
            Console.WriteLine("Command: ->" + args + "<-");
        }
        var llmProcessInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = args,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        string responseGiven = "";
        using (var llmProcess = Process.Start(llmProcessInfo))
        {
            using (var reader = llmProcess.StandardOutput)
            {
                responseGiven = reader.ReadToEnd();
            }
        }

        // Add time elapsed info
        stopwatch.Stop();
        TimeSpan responseTime = stopwatch.Elapsed;
        responseGiven += $"\n\nResponse time: {responseTime.Minutes}m {responseTime.Seconds}s {responseTime.Milliseconds.ToString("D3")}ms";

        return (responseGiven);
    }
}
