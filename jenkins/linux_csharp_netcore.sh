#!/usr/bin/env bash
# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

./build.sh -clean -configuration Debug
if [ "$?" -ne 0 ]; then
	exit 1
fi 

# Start the TPM simulator
killall -9 Simulator
$TPM_SIMULATOR_PATH &

./build.sh -clean -configuration Release -e2etests
if [ "$?" -ne 0 ]; then
	exit 1
fi 

echo
echo Linux C# build completed successfully.
echo
