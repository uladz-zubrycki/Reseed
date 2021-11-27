@echo off
SET "src=%~dp0src"
SET "props=%~dp0publish.props"
SET "key=%NUGET_RESEED_KEY%"
SET "outpath=%src%\Reseed\bin\publish"
SET version=0.1.10

if exist %outpath%\ (
    rmdir /s /q %outpath%
)

dotnet pack "%src%\Reseed\Reseed.csproj" -p:PackageVersion=%version%  -p:DirectoryBuildPropsPath="%props%" -p:OutputPath=%outpath% 
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet nuget push --api-key %key% --source https://api.nuget.org/v3/index.json "%outpath%\Reseed.%version%.nupkg"
if %errorlevel% neq 0 exit /b %errorlevel%