@echo off
pushd "%~dp0"
call build && dotnet pack --no-build -c Release %* src
popd
exit /b %ERRORLEVEL%
