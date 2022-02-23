# BejeweledBot
Vision based Bejeweled (and similar format) bot. Uses Emgu (C# OpenCV Wrapper) to identify tile colors from a grid and determines the best move from there. Automatically moves the cursor and clicks / drags pieces. Not the most user-friendly, but plays extremely fast. Often faster than the game can process new moves. Tile colors were measured from Bejeweled 3. 

**NOTE: I am in no way associated with Bejeweled or EA, the Bejeweled franchise is property of Electronic Arts.**

# Keys
Read the full list of bindings within the keyboardHook.OnKeyPress event handler. This readme is not guaranteed to be kept up-to-date. 

- KEY_P: Shows the preview window, will be useful when setting up the grid
- KEY_T: Sets the corners of the grid. A window will appear showing the current snapshot area and grid. Press and release T once to set the first corner, and so for the second. Once the second corner has been set, you are ready to play
- KEY_F: Executes one automatic move
- KEY_G: Toggles automatic moves. Note this program moves your cursor, so if at any point it is running and you press the G key, it will begin executing moves (even if they are not valid)
- KEY_B: Toggles/Removes thottling restrictions. Throttling was put in place because without them the program will often move the same tile many times within a second, nullifying the original move. In some game-modes it can be useful.
