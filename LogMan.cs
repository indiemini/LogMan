using LogManager.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LogManager
{
    public static class LogMan
    {
        private static bool _init;
        private static string _current;
        private static int _historyIndex;
        private static CircularBuffer<string> _history;
        private static object _l = new object();

        /// <summary>
        /// Prepares the LogMan for usage with the given command history size.
        /// Call LogMan.Initialize() once, at the entry point of your program, not
        /// at all--as long as you're okay with the default history length of 250.
        /// </summary>
        public static void Initialize(int commandHistory = 250)
        {
            lock (_l)
            {
                // initialize our command history with the specified length
                _history = new CircularBuffer<string>(commandHistory);

                // show the input caret if this is our first init
                if (!_init)
                {
                    _init = true;
                    _nextOutput(false);
                }
            }
        }

        // If the user chooses not to explicitly initialize,
        // we will initialize for them with a default command history of 250.
        static LogMan()
        {
            // default initializer.
            Initialize();
        }

        /// <summary>
        /// Logs some information to the console, without interrupting input.
        /// </summary>
        /// <param name="data">The data to output.</param>
        public static void Info(object data)
        {
            // The data must be ToString'ed here
            // (avoids potential deadlock)
            string text = data?.ToString();
            lock (_l)
            {
                // calculate how many vertical lines we've moved from line breaks (long cmds)
                int vert = _current.Length / Console.BufferWidth;
                // reset cur to 0 to overwrite any command text
                Console.SetCursorPosition(0, Console.CursorTop - vert);
                // output intended log data
                Console.Write(text);
                // output empty text to clear line, leaving space for a new line
                Console.Write(new string(' ', Console.BufferWidth - Console.CursorLeft));
                // newline char
                Console.WriteLine();
                // remember our current origin
                int oy = Console.CursorTop;
                // re-output any command text
                Console.Write(_current);
                // get old x location from mod math
                int x = (_current.Length - _endIndex) % Console.BufferWidth;
                // and get old y similarly.
                int y = (_current.Length - _endIndex) / Console.BufferWidth + oy;
                // reset cur to old position in command text
                Console.SetCursorPosition(x, y);
            }
        }
        // All next/sim methods are called from ReadLine
        // and have synchronization there!
        private static int _endIndex = 0;
        private static void _nextOutput(bool newLine = true)
        {
            // make sure that we jump down to
            // our bottom-most input line.
            _simEndKey();

            // now, reset.
            _endIndex = 0;
            _current = "> ";
            if (newLine) Console.Write("\n" + _current);
            else Console.Write(_current);
        }

        private static void _clearCurrentCommand()
        {
            // move cursor to top
            _simHomeKey();
            // remember these coords
            int x = Console.CursorLeft;
            int y = Console.CursorTop;
            // output empty text to clear the line
            Console.Write(new string(' ', Console.BufferWidth - Console.CursorLeft - 2));
            // figure out how many additional lines we need to clear
            int linesForConsoleCursor = _current.Length / Console.BufferWidth;
            // clear them.
            for (int cy = y; cy < y + linesForConsoleCursor; cy++)
                Console.Write(new string(' ', Console.BufferWidth));
            // reset our command text.
            _current = "> ";
            _endIndex = 0;
            // move back to our old position.
            Console.CursorLeft = x;
            Console.CursorTop = y;
        }

        private static void _simLeftArr()
        {
            int maxAllowed = _current.Length - 2;
            if (_endIndex + 1 > maxAllowed)
            {
                // This movement is past the
                // "> " prefix. Disallow it.
            }
            else
            {
                if (Console.CursorLeft - 1 < 0)
                {
                    // we didn't move past our "> " prefix,
                    // but we still reached the bounds of the buffer.
                    // this means that this is a multi-line command.
                    // move our cursor to the end of the previous line.
                    Console.SetCursorPosition(Console.BufferWidth - 1, Console.CursorTop - 1);
                }
                else
                {
                    // just move the cursor left.
                    Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                }

                // move our simulated cursor.
                _endIndex++;
            }
        }
        private static void _simRightArr()
        {
            int minAllowed = 0;
            if (_endIndex - 1 < minAllowed)
            {
                // This movement is past the
                // end of the input. Disallow it.
            }
            else
            {
                if (Console.CursorLeft + 1 >= Console.BufferWidth)
                {
                    // we didn't move past the end of our input,
                    // but we still reached the bounds of the buffer.
                    // this means that this is a multi-line command.
                    // move our cursor to the start of the following line.
                    Console.SetCursorPosition(0, Console.CursorTop + 1);
                }
                else
                {
                    // just move the cursor right.
                    Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
                }

                // move our simulated cursor.
                _endIndex--;
            }
        }
        private static void _simUpArr()
        {
            // erase any existing command text
            _clearCurrentCommand();
            // move back one in command history
            string command = _history.Pull(++_historyIndex);
            // add input caret back
            _current = "> " + command;
            // output new command text
            Console.Write(_current.Substring(2));
            // reset cur to end of line
            _simEndKey();
        }
        private static void _simDownArr()
        {
            // erase any existing command text
            _clearCurrentCommand();
            // move forward one in command history
            string command = _history.Pull(--_historyIndex);
            // add input caret back
            _current = "> " + command;
            // output new command text
            Console.Write(_current.Substring(2));
            // reset cur to end of line
            _simEndKey();
        }
        private static void _simEndKey()
        {
            // Is our simulated cursor above our console cursor?
            if (_current != null)
            {
                // Figure out where our simulated cursor is, relative to
                // where it would be at the end of the command string,
                // measured in lines (rows).
                int ind = _current.Length - _endIndex;
                // how many lines do we expect the text to occupy, up to cursor pos?
                int linesForSimCursor = ind / Console.BufferWidth;
                // how many lines do we expect the text to occupy, up to the end?
                int linesForConsoleCursor = _current.Length / Console.BufferWidth;
                // calculate diff.
                int diff = linesForConsoleCursor - linesForSimCursor;

                // Is our simulated cursor above our console cursor?
                if (diff != 0)
                {
                    // If so, jump the cursor back down to the bottom.
                    Console.CursorTop += diff;
                }

                // meanwhile, our x is simply calculated with modulo.
                int x = _current.Length % Console.BufferWidth;
                // apply to console cursor.
                Console.SetCursorPosition(x, Console.CursorTop);
            }

            // move our simulated cursor.
            _endIndex = 0;
        }
        private static void _simHomeKey()
        {
            // Is our simulated cursor above our console cursor?
            if (_current != null)
            {
                // Figure out where our simulated cursor is, relative to
                // where it would be at the start of the command string,
                // measured in lines (rows).
                int ind = _current.Length - _endIndex;
                // how many lines do we expect the text to occupy, up to cursor pos?
                int linesForSimCursor = ind / Console.BufferWidth;
                // this is our diff, no?
                int diff = linesForSimCursor;

                // Is our simulated cursor above our console cursor?
                if (diff != 0)
                {
                    // If so, jump the cursor back up to the top.
                    Console.CursorTop -= diff;
                }

                // meanwhile, our x is simply the offset from "> ".
                int x = 2;
                // apply to console cursor.
                Console.SetCursorPosition(x, Console.CursorTop);
            }

            // move our simulated cursor.
            _endIndex = _current.Length - 2;
        }
        private static void _simBackspace()
        {
            int maxAllowed = _current.Length - 2;
            if (_endIndex < maxAllowed)
            {
                // for simplicity's sake, grab the current cursor pos
                int x = Console.CursorLeft;
                int y = Console.CursorTop;
                // get simulated cursor index - 1
                int ind = _current.Length - _endIndex - 1;
                // remove at that index.
                _current = _current.Remove(ind, 1);
                // if we're 
                Console.Write("\b \b");
                // Re-output remaining chars
                Console.Write(_current.Substring(ind));
                // Replace final char
                Console.Write(" \b");
                // Move cursor back to original location, offset by 1
                x -= 1;
                // If we're at the start of a line, we need to go up a row
                if (x < 0)
                {
                    // end of the previous line
                    x = Console.BufferWidth - 1;
                    // previous line.
                    y -= 1;
                    // move to that location.
                    Console.SetCursorPosition(x, y);
                    // output a space to clear the char...
                    Console.Write(' ');
                }
                // move to that location.
                Console.SetCursorPosition(x, y);
            }
        }
        private static void _simDelete()
        {
            int minAllowed = 0;
            if (_endIndex > minAllowed)
            {
                // for simplicity's sake, grab the current cursor pos
                int x = Console.CursorLeft;
                int y = Console.CursorTop;
                // get simulated cursor index
                int ind = _current.Length - _endIndex;
                // remove at that index.
                _current = _current.Remove(ind, 1);
                // Re-output remaining chars
                Console.Write(_current.Substring(ind));
                // Replace final char
                Console.Write(" \b");
                // Move cursor back to original location
                Console.SetCursorPosition(x, y);
                // Adjust end index because it has moved closer to end
                _endIndex--;
            }
        }
        private static void _nothingKey()
        {
            // used for keys that might ordinarily do some magic
            // but here, don't.
        }
        private const int RETURN = 13;
        // ReadLine should be called from one location at a time.
        // Since this is a console-managing class, this is generally only
        // called in the main function for a program.
        /// <summary>
        /// Reads a line from the console.
        /// LogMan.ReadLine() blocks until a line is available.
        /// </summary>
        /// <returns>The line input.</returns>
        public static string ReadLine()
        {
            while (true)
            {
                // This work-around is required due to the
                // odd behavior of Console.CursorLeft/Console.CursorTop used in LogMan.Info
                // under Unix platforms (see https://github.com/dotnet/runtime/issues/23850)
                while (!Console.KeyAvailable)
                    Thread.Sleep(25);

                ConsoleKeyInfo ch = Console.ReadKey(true);
                if (ch.KeyChar != RETURN)
                {
                    lock (_l)
                    {
                        if (ch.Key == ConsoleKey.LeftArrow)
                            _simLeftArr();
                        else if (ch.Key == ConsoleKey.RightArrow)
                            _simRightArr();
                        else if (ch.Key == ConsoleKey.UpArrow)
                            _simUpArr();
                        else if (ch.Key == ConsoleKey.DownArrow)
                            _simDownArr();
                        else if (ch.Key == ConsoleKey.Delete)
                            _simDelete();
                        else if (ch.Key == ConsoleKey.Backspace)
                            _simBackspace();
                        else if (ch.Key == ConsoleKey.End)
                            _simEndKey();
                        else if (ch.Key == ConsoleKey.Home)
                            _simHomeKey();
                        else if (_isWackKey(ch))
                            _nothingKey();
                        else
                        {
                            // If outputing this character causes us to
                            // break a line, we will need to be able to undo that.
                            // So let's remember the current l+r values of the console:
                            int cursorLeft = Console.CursorLeft;
                            int cursorTop = Console.CursorTop;
                            // write out that character.
                            Console.Write(ch.KeyChar);
                            // update simulated cursor
                            int ind = _current.Length - _endIndex;
                            _current = _current.Insert(ind, $"{ch.KeyChar}");
                            // Clear history index
                            _historyIndex = 0;
                            // Hold current in history
                            _history.Hold(_current.Substring(2));
                            // Re-output remaining chars
                            Console.Write(_current.Substring(ind + 1));
                            // Did this cause us to break a line?
                            if (Console.CursorTop > cursorTop ||
                                (ind + 1) % Console.BufferWidth == 0)
                            {
                                // yes. our original x location is
                                // then given by the normal modulo math...
                                int x = (ind + 1) % Console.BufferWidth;
                                // and our y should be the old value...
                                int y = cursorTop;
                                // unless the new character that we wrote
                                // specifically lands on the buffer bounds.
                                if ((ind + 1) % Console.BufferWidth == 0)
                                {
                                    //... can we just add 1 here?
                                    y += 1;
                                }
                                // these are our new cursor values.
                                Console.SetCursorPosition(x, y);
                            }
                            else
                            {
                                // Move cursor back to original location
                                Console.SetCursorPosition((ind + 1) % Console.BufferWidth, Console.CursorTop);
                            }
                        }
                    }
                }
                else break;
            }

            lock (_l)
            {
                string ret = _current.Substring(2);
                // Save input in history if it's a meaningful command
                if (!string.IsNullOrWhiteSpace(ret)) _history.Push(ret);
                // Save new history index
                _historyIndex = 0;
                // Clear line and re-output input caret
                _nextOutput();
                return ret;
            }
        }
        // Returns whether a key is super wack
        // and should not be handled OR displayed.
        #region
        private static bool _isWackKey(ConsoleKeyInfo keyInfo)
        {
            // First, deal with some oddball cases
            // where ConsoleKey gives stuff like "Oem2"
            if (keyInfo.KeyChar == '?'  ||
                keyInfo.KeyChar == '/'  ||
                keyInfo.KeyChar == ';'  ||
                keyInfo.KeyChar == ':'  ||
                keyInfo.KeyChar == ','  ||
                keyInfo.KeyChar == '.'  ||
                keyInfo.KeyChar == '@'  ||
                keyInfo.KeyChar == '"'  ||
                keyInfo.KeyChar == '|'  ||
                keyInfo.KeyChar == '`'  ||
                keyInfo.KeyChar == '~'  ||
                keyInfo.KeyChar == '{'  ||
                keyInfo.KeyChar == '}'  ||
                keyInfo.KeyChar == '['  ||
                keyInfo.KeyChar == ']'  ||
                keyInfo.KeyChar == '\'' ||
                keyInfo.KeyChar == '\\')
            {
                return false;
            }

            ConsoleKey key = keyInfo.Key;
            return
                key == ConsoleKey.PageUp ||
                key == ConsoleKey.PageDown ||
                key == ConsoleKey.Escape ||
                key == ConsoleKey.Zoom ||
                key == ConsoleKey.Applications ||
                key == ConsoleKey.Attention ||
                key == ConsoleKey.BrowserBack ||
                key == ConsoleKey.BrowserFavorites ||
                key == ConsoleKey.BrowserForward ||
                key == ConsoleKey.BrowserHome ||
                key == ConsoleKey.BrowserRefresh ||
                key == ConsoleKey.BrowserSearch ||
                key == ConsoleKey.BrowserStop ||
                key == ConsoleKey.Clear ||
                key == ConsoleKey.CrSel ||
                key == ConsoleKey.EraseEndOfFile ||
                key == ConsoleKey.Execute ||
                key == ConsoleKey.ExSel ||
                key == ConsoleKey.F1 ||
                key == ConsoleKey.F2 ||
                key == ConsoleKey.F3 ||
                key == ConsoleKey.F4 ||
                key == ConsoleKey.F5 ||
                key == ConsoleKey.F6 ||
                key == ConsoleKey.F7 ||
                key == ConsoleKey.F8 ||
                key == ConsoleKey.F9 ||
                key == ConsoleKey.F10 ||
                key == ConsoleKey.F11 ||
                key == ConsoleKey.F12 ||
                key == ConsoleKey.F13 ||
                key == ConsoleKey.F14 ||
                key == ConsoleKey.F15 ||
                key == ConsoleKey.F16 ||
                key == ConsoleKey.F17 ||
                key == ConsoleKey.F18 ||
                key == ConsoleKey.F19 ||
                key == ConsoleKey.F20 ||
                key == ConsoleKey.F21 ||
                key == ConsoleKey.F22 ||
                key == ConsoleKey.F23 ||
                key == ConsoleKey.F24 ||
                key == ConsoleKey.Help ||
                key == ConsoleKey.Insert ||
                key == ConsoleKey.LaunchApp1 ||
                key == ConsoleKey.LaunchApp2 ||
                key == ConsoleKey.LaunchMail ||
                key == ConsoleKey.LaunchMediaSelect ||
                key == ConsoleKey.LeftWindows ||
                key == ConsoleKey.MediaNext ||
                key == ConsoleKey.MediaPlay ||
                key == ConsoleKey.MediaPrevious ||
                key == ConsoleKey.MediaStop ||
                key == ConsoleKey.NoName ||
                key == ConsoleKey.Oem1 ||
                key == ConsoleKey.Oem2 ||
                key == ConsoleKey.Oem3 ||
                key == ConsoleKey.Oem4 ||
                key == ConsoleKey.Oem5 ||
                key == ConsoleKey.Oem6 ||
                key == ConsoleKey.Oem7 ||
                key == ConsoleKey.Oem8 ||
                key == ConsoleKey.Oem102 ||
                key == ConsoleKey.OemClear ||
                key == ConsoleKey.Pa1 ||
                key == ConsoleKey.Packet ||
                key == ConsoleKey.Pause ||
                key == ConsoleKey.Play ||
                key == ConsoleKey.Print ||
                key == ConsoleKey.PrintScreen ||
                key == ConsoleKey.Process ||
                key == ConsoleKey.RightWindows ||
                key == ConsoleKey.Select ||
                key == ConsoleKey.Separator ||
                key == ConsoleKey.Sleep ||
                key == ConsoleKey.VolumeDown ||
                key == ConsoleKey.VolumeMute ||
                key == ConsoleKey.VolumeUp;
        }
        #endregion
    }
}
