#!/bin/zsh

# Script to build and install the Ritten CLI tool locally.
# This allows you to run 'ritten' from anywhere in your terminal.

set -e  # Exit on error

echo "🔨 Building the project..."
dotnet build src/Ritten/Ritten.csproj --configuration Release
echo ""

echo "📦 Packing the tool..."
dotnet pack src/Ritten/Ritten.csproj --configuration Release
echo ""

echo "🗑️  Uninstalling previous version (if exists)..."
dotnet tool uninstall --global Ritten 2>/dev/null || true
echo ""

echo "⚙️  Installing the tool globally..."
dotnet tool install --global --add-source ./src/Ritten/dist Ritten
echo ""

echo "✅ Installation complete!"
echo "You can now run 'ritten' from anywhere in your terminal."
echo "Example usage:"
echo "  ritten build --help"
