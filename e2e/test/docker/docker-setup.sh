#!/bin/bash
LOG_INFO="\x1b[33;42;1m[INFO]:\x1b[0m"
LOG_TODO="\x1b[37;43;1m[TODO]:\x1b[0m"
LOG_ERR="\x1b[33;41;1m[ERROR]:\x1b[0m"

echo "============"
echo "Start Docker"
echo "============"
echo "running from $(pwd)"
sudo service docker start

echo "========================"
echo "Extract self-signed cert"
echo "========================"
PROXY_CERT_ZFILE="./haproxy/haproxy.bin"
PROXY_CERT_FILE="./haproxy/haproxy.pem"
if [ -f "$PROXY_CERT_FILE" ];
then
	echo -e "$LOG_INFO Use existing Proxy Service Cert found."
elif [ -f "$PROXY_CERT_ZFILE" ];
then
	gzip -d -N -k -q -S ".bin" ./haproxy/haproxy.bin
	echo -e "$LOG_INFO Proxy Service Cert extracted."
else
	echo -e "$LOG_ERR Missing Proxy Service Cert! Check your repo."
	exit
fi

echo "==============="
echo "Inspect network"
echo "==============="
ip -4 addr
CVTEST_HOST_IP=$(ip -4 addr show eth0 | grep -Po 'inet \K[\d.]+')
CVTEST_HOST_NETWORK=$(ip -4 addr show eth0 | grep -Po 'inet \K[\d.]+/*\d.')
CVTEST_HOST_SUBNET=$(ip -4 addr show eth0 | grep -Po 'inet \K[\d.]+' | grep -Po '[\d]{1,3}.[\d]{1,3}.[\d]{1,3}')
CVTEST_HOST_SUBNET_MASK=$(ip -4 addr show eth0 | grep -Po 'inet \K[\d.]+/*\d.' | grep -Po '/[\d]{1,2}')
CVTEST_CONTAINER_GW=$(ip -4 addr show docker0 | grep -Po 'inet \K[\d.]+')
CVTEST_HOST_WSL_IP=$(ip -4 addr show eth0 | grep -Po 'inet \K[\d.]+')
CVTEST_ROUTE_NET=$(route -n | grep 'eth0' | grep -v 'UG' | awk -F" " '{print $1}' | awk -F"." '{print $1"."$2"."$3"."$4"/29"}')
CVTEST_TST_NET=$CVTEST_HOST_NETWORK
CVTEST_GDE_IP=$(ip -4 addr show eth0 | grep -Po 'inet \K[\d.]+' | awk -F"." '{print $1"."$2"."$3"."$4+3}')
CVTEST_DPS_IP=$(ip -4 addr show eth0 | grep -Po 'inet \K[\d.]+' | awk -F"." '{print $1"."$2"."$3"."$4+4}')
CVTEST_HUB_IP=$(ip -4 addr show eth0 | grep -Po 'inet \K[\d.]+' | awk -F"." '{print $1"."$2"."$3"."$4+5}')
echo "HOST=$CVTEST_HOST_IP"
echo "HOST NETWORK=$CVTEST_HOST_NETWORK"
echo "HOST SUBNET=$CVTEST_HOST_SUBNET"
echo "HOST SUBNET MASK=$CVTEST_HOST_SUBNET_MASK"
echo "Container GW=$CVTEST_CONTAINER_GW"
echo "WSL=$CVTEST_HOST_WSL_IP"
echo "Container NET=$CVTEST_TST_NET"
echo "Container GDE=$CVTEST_GDE_IP"
echo "Container DPS=$CVTEST_DPS_IP"
echo "Container HUB=$CVTEST_HUB_IP"
ping -c 2 $CVTEST_HOST_IP

echo "===================="
echo "Setup docker network"
echo "===================="
docker images
docker ps -a
docker network rm testnet
docker network create -d ipvlan --subnet=$CVTEST_TST_NET -o ipvlan_mode=l2 -o parent=eth0 testnet
docker network ls

echo "======================"
echo "Setup docker instances"
echo "======================"
AZURE_ACR_TOKEN=$(az acr login -n aziotacr -t --output tsv --query accessToken)
echo $AZURE_ACR_TOKEN | docker login aziotacr.azurecr.io --username 00000000-0000-0000-0000-000000000000 --password-stdin
docker run -h invalidcertgde1.westus.cloudapp.azure.com --name invalid-gde --expose=443 --expose=5671 --expose=8883 --network=testnet --ip=$CVTEST_GDE_IP -v $(pwd)/haproxy:/usr/local/etc/haproxy:ro -d aziotacr.azurecr.io/haproxy haproxy -f /usr/local/etc/haproxy/haproxygde.cfg
docker run -h invalidcertdps1.westus.cloudapp.azure.com --name invalid-dps --expose=443 --expose=5671 --expose=8883 --network=testnet --ip=$CVTEST_DPS_IP -v $(pwd)/haproxy:/usr/local/etc/haproxy:ro -d aziotacr.azurecr.io/haproxy haproxy -f /usr/local/etc/haproxy/haproxydps.cfg
docker run -h invalidcertiothub1.westus.cloudapp.azure.com --name invalid-hub --expose=443 --expose=5671 --expose=8883 --network=testnet --ip=$CVTEST_HUB_IP -v $(pwd)/haproxy:/usr/local/etc/haproxy:ro -d aziotacr.azurecr.io/haproxy haproxy -f /usr/local/etc/haproxy/haproxyhub.cfg
docker run --name e2etest-pxy -p 127.0.0.1:8888:8888 -d aziotacr.azurecr.io/aziotbld/testproxy

echo "================="
echo "Inspect instances"
echo "================="
docker ps -a

echo "========================================================"
echo -e "$LOG_TODO update host file for local E2E Tests"
echo "        (on your host Windows OS)"
echo "        add/update hosts file with the following entries"
echo "        (at C:\Windows\System32\drivers\etc\hosts)"
echo "========================================================"
echo "$(docker inspect invalid-gde | grep -Po -m 1 '"IPAddress": "\K[\d.]+') invalidcertgde1.westus.cloudapp.azure.com"
echo "$(docker inspect invalid-dps | grep -Po -m 1 '"IPAddress": "\K[\d.]+') invalidcertdps1.westus.cloudapp.azure.com"
echo "$(docker inspect invalid-hub | grep -Po -m 1 '"IPAddress": "\K[\d.]+') invalidcertiothub1.westus.cloudapp.azure.com"
