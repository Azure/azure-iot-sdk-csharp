#!/usr/bin/env bash
# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

./build.sh -clean -configuration Debug
if [ "$?" -ne 0 ]; then
	exit 1
fi

# Start the TPM simulator
pkill -9 Simulator
for i in $(seq 1 5); do $TPM_SIMULATOR_PATH; done &

./build.sh -clean -configuration Release -e2etests
if [ "$?" -ne 0 ]; then
	exit 1
fi

echo
echo Linux C# build completed successfully.
echo
