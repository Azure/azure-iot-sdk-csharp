// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Collections.Concurrent;

public class Program
{
    private static ConcurrentDictionary<string, DateTimeOffset> _twinResponseTimeouts = new();

    public static void Main()
    {
        _twinResponseTimeouts["1"] = DateTimeOffset.Now;

        try
        {
            var sd = _twinResponseTimeouts.TryRemove("2", out _);
            Console.WriteLine(sd);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}
