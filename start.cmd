@echo off
rem Starts the app with hot reload; dotnet watch opens the browser at https://localhost:7156 when it is ready.
dotnet watch --project "%~dp0RecipeBook" --launch-profile RecipeBook
