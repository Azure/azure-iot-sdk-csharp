package com.microsoft.azure.sdk.iot;

import com.github.monkeywie.proxyee.server.HttpProxyServer;
import com.github.monkeywie.proxyee.server.HttpProxyServerConfig;

public class Main
{
    private static final int PORT = 9000;

    public static void main(String[] args)
    {
        HttpProxyServerConfig configWithoutAuth = new HttpProxyServerConfig();
        configWithoutAuth.setHandleSsl(false);
        HttpProxyServer proxyServer = new HttpProxyServer().serverConfig(configWithoutAuth);
        System.out.println("Starting test HTTP proxy on port 9000...");
        proxyServer.start(PORT);
    }
}