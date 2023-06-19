dotnet publish -r win-x64 -c Release -o release /p:SelfContained=true /p:PublishSingleFile=true /p:PublishReadyToRun=true /p:IncludeNativeLibrariesForSelfExtract=true
xcopy Readme.txt release\Readme.txt /Y
pause