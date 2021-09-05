# Xappium CLI

The Xappium CLI is a dotnet cli tool aimed to make it easier to set, and run your UI Tests in any environment whether that's on your local machine or as part of an automated CI build. In order to run Xappium UI Tests you must first build the app, build the UI Test project, ensure that Appium is installed, ensure that a device or simulator/emulator is ready and available. Finally start Appium, run your tests, and then stop Appium. The benefit of the CLI tool is that you simply need to point it at your App's project and your UI Test project, it will take care of the rest.
