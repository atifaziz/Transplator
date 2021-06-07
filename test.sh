#!/usr/bin/env bash
set -e
cd "$(dirname "$0")"
./build.sh
dotnet test --no-build tests -c Debug --settings tests/coverlet.runsettings
TEST_RESULTS_DIR="$(ls -dc tests/TestResults/* | head -1)"
cp "$TEST_RESULTS_DIR/coverage.opencover.xml" tests/TestResults
dotnet reportgenerator -reports:tests/TestResults/coverage.opencover.xml \
                       -reporttypes:Html\;TextSummary \
                       -targetdir:tests/TestResults/reports
cat tests/TestResults/reports/Summary.txt
dotnet test --no-build tests -c Release
