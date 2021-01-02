# LogMan

LogMan is a lightweight .NET console solution, inspired by the console input system used in Minecraft server consoles. It allows multiple threads to output text to the console, without breaking any input currently being typed by a user.

## Demo
![Uninterrupted logging](assets/demo.gif)

## Installation

LogMan is available as a NuGet package, but you can also compile it from source.

## Usage

```csharp
using LogMan;
using System;

class Program
{
    static void Main(string[] args)
    {
        LogMan.Initialize();
        LogMan.Info("Hello world!");

        string command;
        while ((command = LogMan.ReadLine()) != "stop")
        {
            // we got a line, let's handle it!
            HandleInput(command);
        }
    }

    static void HandleInput(string text)
    {
        // dummy code!
        if (text.StartsWith("hey logman!"))
            LogMan.Info("hey back!");
    }
}
```


## License
[MIT](https://choosealicense.com/licenses/mit/)