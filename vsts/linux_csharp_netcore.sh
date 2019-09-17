#!/usr/bin/env bash
# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

./build.sh -clean -configuration Debug
if [ "$?" -ne 0 ]; then
	exit 1
fi 

# Run the TPM Simulator
pkill -f Simulator
$tpm_simulator_path\Simulator &

./build.sh -clean -configuration Release
if [ "$?" -ne 0 ]; then
	exit 1
fi 

echo
echo Linux C# build completed successfully.
echo
