#!/bin/bash

dotnet run --project src/Xappium.Cli/Xappium.Cli.csproj -c Release -- test -uitest sample/TestApp.UITests/TestApp.UITests.csproj -app-project sample/TestApp.Android/TestApp.Android.csproj
