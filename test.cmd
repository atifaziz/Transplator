@echo off
pushd "%~dp0"
call :main %*
popd
goto :EOF

:main
     call build ^
  && call :test Debug --settings tests\coverlet.runsettings ^
  && call :report-coverage ^
  && call :test Release
goto :EOF

:test
dotnet test --no-build tests -c %*
goto :EOF

:report-coverage
set TEST_RESULTS_DIR=
for /f %%d in ('dir /b /od /ad tests\TestResults') do set TEST_RESULTS_DIR=tests\TestResults\%%~d
if not defined TEST_RESULTS_DIR (
    echo>&2 Test coverage XML not found!
    exit /b 1
)
     copy "%TEST_RESULTS_DIR%\coverage.opencover.xml" tests\TestResults > nul ^
  && dotnet reportgenerator -reports:tests\TestResults\coverage.opencover.xml ^
                            -reporttypes:Html;TextSummary ^
                            -targetdir:tests\TestResults\reports ^
  && type tests\TestResults\reports\Summary.txt
goto :EOF
