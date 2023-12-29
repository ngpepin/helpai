# HelpAI Command Line Tool

## Overview
HelpAI is a command-line interface tool designed to interact with an AI language model. It is specifically configured to work with the 'llm' command and the 'mistral-7b-openorca' model. The tool is intended for a Linux x64 environment and offers a streamlined interface for AI interactions.

## Key Features
- **Contextual Interaction**: Maintains a history of user prompts and AI responses to provide context for subsequent interactions.
- **History Management**: Supports commands to reset the interaction history and enable debug mode.
- **Markdown Output**: Dynamically generates a Markdown file from the dialogue history for easy reading and sharing.
- **Character Handling**: Manages special characters to ensure the integrity of prompts and responses.
- **Performance Metrics**: Measures and displays the response time of the AI model.

## Usage
Compile `helpai.cs` into a standalone executable and run it from the command line. The tool accepts the prompt for the AI as an argument.

### Command Line Options
- `-r` or `--reset`: Resets the interaction history.
- `-d` or `--debug`: Enables debug mode to output additional information.

### Example
```bash
./helpai "What is the capital of France?"
./helpai -r "Start a new session"
./helpai -d "Debug mode example"
```

## Implementation Details

* **History File**: Exchanges (prompts and responses) are stored in a text file to provide context to the AI model.
* **Character Limit**: The history file is trimmed if it exceeds a specified character limit to prevent buffer overflow.
* **Special Character Handling**: Ensures prompts and responses are free from syntax or format issues.
* **Markdown Formatting**: Creates a readable transcript in Markdown format, with appropriate formatting for various elements.

## Author

Nicolas Pepin

## Date

2023

## Version

1.0

## Note

Ensure that the 'llm' command and the 'mistral-7b-openorca' model are properly configured and accessible in the environment where this tool is executed.
