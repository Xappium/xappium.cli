#!/bin/bash

dotnet run --project src/Xappium.Cli/Xappium.Cli.csproj -c Release -- ios test -uitest sample/TestApp.UITests/TestApp.UITests.csproj --app-project sample/TestApp.iOS/TestApp.iOS.csproj
