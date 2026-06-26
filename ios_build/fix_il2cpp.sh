#!/bin/bash
DIR="$(cd "$(dirname "$0")" && pwd)"
IL2CPP_DIR="$DIR/Il2CppOutputProject/IL2CPP/build/deploy_x86_64"
echo "Fixing IL2CPP binaries at $IL2CPP_DIR..."
xattr -cr "$IL2CPP_DIR" 2>/dev/null
chmod +x "$IL2CPP_DIR/il2cpp" "$IL2CPP_DIR/il2cpp-compile" 2>/dev/null
chmod +x "$IL2CPP_DIR/"*.dylib 2>/dev/null
chmod +x "$IL2CPP_DIR/bee_backend/mac-x64/bee_backend" 2>/dev/null
echo "Done. Now open Xcode and build."
