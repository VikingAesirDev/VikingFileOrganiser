# VikingFileOrganiser



This is a very simple app that I made to automate the process of organising individual files into their own folders.

Some media players require specific formatting for media files, I had thousands that needed to be in their own folder for that media player to see them.

Which gave birth to this app.

Feel free to muck around with it. Its based on the following batch file script:
```
 @echo off
for %%i in (*) do (
 if not "%%~ni" == "organize" (
  md "%%~ni" && move "%%~i" "%%~ni"
 )
)
```
Future plans???
I'd like to add a renaming process for the files before theyre organised.

Please feel free to provide feedback or feature you may want added.
