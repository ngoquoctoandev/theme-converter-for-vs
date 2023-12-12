**GENERATE A THEME FROM JSON FILE**

*✽ Step 1: Getting a theme's json file*
o Open VS Code.
o Install the desired color theme and switch to this theme in VS Code. Please note that this tool will not convert icon themes.
o “Ctrl + Shift + P” and run “Developer: Generate Color Theme from current settings.”
o In the generated JSON, uncomment all code. When you uncomment, please be careful about missing commas! Make sure the JSON is valid.
o Save this as a “JSON” file for the conversion, using the theme's name as the file name. Please ensure that the file’s extension is .json. (The file shouldn’t be saved as a JSONC file.)
o Note: Because some part of VS UI does not support customized alpha channel, we recommend reducing the usage of not fully opaque colors for better conversion result.

*✽ Step 2:*
Copy the generated JSON file to the same folder as ThemeConverter.exe.

*✽ Step 3:*
Run ThemeConverter.exe with the following command line arguments:
```
ThemeConverter.exe -i "./One Dark Pro.json" -o ./
```