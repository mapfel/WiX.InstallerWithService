pushd %~dp0

SET TOOL_PATH=./../tools/.fake

IF NOT EXIST "%TOOL_PATH%\fake.exe" (
  dotnet tool install fake-cli --tool-path %TOOL_PATH%
)

.\..\.paket\paket restore

.\..\tools\.fake\fake.exe run build.fsx

popd
exit /b %errorlevel%