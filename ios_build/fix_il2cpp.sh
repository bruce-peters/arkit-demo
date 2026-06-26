#!/bin/bash
# Run this on the Mac:  bash fix_il2cpp.sh

echo "=== Checking environment ==="

# Check for Rosetta
if /usr/bin/arch -x86_64 /bin/true 2>/dev/null; then
    echo "Rosetta 2: OK"
else
    echo "Rosetta 2: MISSING - run: softwareupdate --install-rosetta"
fi

# Check for .NET 9.0 SDK (needed as fallback)
if command -v dotnet &>/dev/null && dotnet --list-sdks 2>/dev/null | grep -q "^9\."; then
    echo ".NET 9 SDK: OK"
    DOTNET_OK=1
else
    echo ".NET 9 SDK: MISSING"
    echo "Install from: https://dotnet.microsoft.com/download/dotnet/9.0"
    echo "(download the macOS Arm64 SDK installer)"
    DOTNET_OK=0
fi

echo ""
echo "=== Fixing permissions ==="

# Fix both deploy dirs
for DEPLOY in deploy_arm64 deploy_x86_64; do
    DIR="$(pwd)/Il2CppOutputProject/IL2CPP/build/$DEPLOY"
    if [ -d "$DIR" ]; then
        xattr -cr "$DIR" 2>/dev/null
        chmod +x "$DIR/il2cpp" "$DIR/il2cpp-compile" "$DIR/UnityLinker" "$DIR/UnityLinkerOld" 2>/dev/null
        chmod +x "$DIR/"*.dylib 2>/dev/null
        chmod +x "$DIR/bee_backend/"*/* 2>/dev/null
        echo "Fixed: $DEPLOY"
    fi
done

echo ""
echo "=== Testing IL2CPP ==="

# Test arm64 native
ARM64="$(pwd)/Il2CppOutputProject/IL2CPP/build/deploy_arm64/il2cpp"
if [ -f "$ARM64" ]; then
    echo -n "arm64 native: "
    "$ARM64" --help >/dev/null 2>&1 && echo "OK" || echo "CRASHES"
fi

# Test x86_64 via Rosetta
X64="$(pwd)/Il2CppOutputProject/IL2CPP/build/deploy_x86_64/il2cpp"
if [ -f "$X64" ]; then
    echo -n "x86_64 (Rosetta): "
    "$X64" --help >/dev/null 2>&1 && echo "OK" || echo "CRASHES"
fi

# Test dotnet fallback (most reliable)
if [ "$DOTNET_OK" = "1" ]; then
    echo -n "dotnet il2cpp.dll: "
    (cd "$(pwd)/Il2CppOutputProject/IL2CPP/build/deploy_x86_64" && \
     dotnet il2cpp.dll --help >/dev/null 2>&1) && echo "OK" || echo "CRASHES"
fi

echo ""
echo "Done."
echo ""
echo "If everything above says CRASHES, go to Unity on Windows,"
echo "open Build Settings, and set IL2CPP Code Generation to 'Faster (smaller) builds'."
echo "Then rebuild the Xcode project."
echo ""
echo "Next steps:"
echo "  1. Install .NET 9 SDK if missing"
echo "  2. Open Xcode, Product > Clean Build Folder"
echo "  3. Product > Build"