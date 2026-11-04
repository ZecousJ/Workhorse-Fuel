#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"

build="$PWD/build"
mkdir -p "$build"
stage="$(mktemp -d)"
trap 'rm -rf "$stage"' EXIT
app="$stage/牛马兴奋剂.app"
sdk="$(xcrun --sdk macosx --show-sdk-path)"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources" "$build/AppIcon.iconset"

xcrun swiftc Sources/Brand.swift Tools/Icon/main.swift -o "$build/draw-icon"
"$build/draw-icon" "$build/AppIcon.iconset"
iconutil -c icns "$build/AppIcon.iconset" -o "$app/Contents/Resources/AppIcon.icns"

for arch in arm64 x86_64; do
    xcrun swiftc -sdk "$sdk" -swift-version 6 -enable-bare-slash-regex -O \
        -target "$arch-apple-macosx14.0" Sources/*.swift -o "$build/Niuma-$arch"
done
xcrun lipo -create "$build/Niuma-arm64" "$build/Niuma-x86_64" -output "$app/Contents/MacOS/Niuma"
cp Resources/Info.plist "$app/Contents/Info.plist"
codesign --force --sign - "$app"
ditto --norsrc --noextattr "$app" "$build/牛马兴奋剂.app"
