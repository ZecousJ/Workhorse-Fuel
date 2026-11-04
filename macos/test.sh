#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
mkdir -p build
xcrun swiftc -swift-version 6 -enable-bare-slash-regex Sources/Payroll.swift Sources/Model.swift Tests/main.swift -o build/payroll-tests
build/payroll-tests
