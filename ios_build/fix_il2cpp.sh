#!/bin/bash
DIR="$(cd "$(dirname "$0")" && pwd)"
IL2CPP_DIR="$DIR/Il2CppOutputProject/IL2CPP/build/deploy_x86_64"
echo "Fixing IL2CPP binaries at $IL2CPP_DIR..."
xattr -cr "$IL2CPP_DIR" 2>/dev/null
chmod +x "$IL2CPP_DIR/il2cpp" "$IL2CPP_DIR/il2cpp-compile" 2>/dev/null
chmod +x "$IL2CPP_DIR/"*.dylib 2>/dev/null
chmod +x "$IL2CPP_DIR/bee_backend/mac-x64/bee_backend" 2>/dev/null
echo "Testing IL2CPP binary..."
"$IL2CPP_DIR/il2cpp" --help >/dev/null 2>&1 && echo "OK - IL2CPP works" || echo "IL2CPP test failed"
echo "Done. Now open Xcode and build."