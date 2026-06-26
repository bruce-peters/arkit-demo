#!/bin/bash
# Run on Mac:  bash fix_il2cpp.sh
set -e

DIR="$(cd "$(dirname "$0")" && pwd)"
DEPLOY="$DIR/Il2CppOutputProject/IL2CPP/build/deploy_x86_64"

echo "=== Stripping self-contained runtime from $DEPLOY ==="

# Remove all .NET runtime assemblies — dotnet will provide its own
cd "$DEPLOY"
rm -f System.*.dll Microsoft.*.dll mscorlib.dll netstandard.dll WindowsBase.dll
rm -f libhostfxr.dylib libhostpolicy.dylib libcoreclr.dylib libclrjit.dylib
rm -f libclrgc*.dylib libmscordaccore.dylib libmscordbi.dylib libSystem.*.dylib
rm -f il2cpp il2cpp-compile UnityLinker UnityLinkerOld

# Rewrite runtimeconfig to use system .NET, not self-contained
cat > il2cpp.runtimeconfig.json << 'RTCFG'
{
  "runtimeOptions": {
    "tfm": "net9.0",
    "framework": {
      "name": "Microsoft.NETCore.App",
      "version": "9.0.0"
    },
    "configProperties": {
      "System.GC.Server": true,
      "System.Globalization.Invariant": true
    }
  }
}
RTCFG

# remove quarantine from remaining files
xattr -cr . 2>/dev/null || true
chmod +x bee_backend/mac-x64/bee_backend 2>/dev/null || true

cd "$DIR"
echo ""
echo "=== Testing ==="
if dotnet exec --runtimeconfig "$DEPLOY/il2cpp.runtimeconfig.json" "$DEPLOY/il2cpp.dll" --help >/dev/null 2>&1; then
    echo "dotnet il2cpp.dll: OK"
else
    echo "dotnet il2cpp.dll: FAILED"
fi

echo ""
echo "Done. Now open Xcode and build."