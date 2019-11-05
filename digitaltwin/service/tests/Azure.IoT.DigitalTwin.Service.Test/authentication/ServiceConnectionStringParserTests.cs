using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Azure.IoT.DigitalTwin.Service.Test.authentication
{
    public class ServiceConnectionStringParserTests
    {
        const string iotHubName = "b.c.d";
        const string hostName = "HOSTNAME." + iotHubName;
        const string sharedAccessKeyName = "ACCESSKEYNAME";
        const string policyName = "SharedAccessKey";
        const string sharedAccessKey = "1234567890abcdefghijklmnopqrstvwxyz=";
        const string SharedAccessSignatureString = "SharedAccessSignature sr=asdf&sig=ghjk&se=111111111111";
        const string connectionStringWithSharedAccessKey = "HostName=" + hostName + ";SharedAccessKeyName=" + sharedAccessKeyName + ";" + policyName + "=" + sharedAccessKey;
        const string connectionStringWithoutHostname = "SharedAccessKeyName=" + sharedAccessKeyName + ";" + policyName + "=" + sharedAccessKey;
        const string connectionStringWithSharedAccessSignature = "HostName=" + hostName + ";SharedAccessSignatureString=" + SharedAccessSignatureString;

        [Fact]
        public void validConnectionStringWithSharedAccessKey()
        {
            ServiceConnectionStringParser parser = ServiceConnectionStringParser.Create(connectionStringWithSharedAccessKey);
            Assert.Equal(hostName, parser.HostName);
            Assert.Equal(sharedAccessKeyName, parser.SharedAccessKeyName);
            Assert.Equal(sharedAccessKey, parser.SharedAccessKey);
        }

        [Fact]
        public void validConnectionStringWithSharedAccessSignature()
        {
            ServiceConnectionStringParser parser = ServiceConnectionStringParser.Create(connectionStringWithSharedAccessSignature);
            Assert.Equal(hostName, parser.HostName);
            Assert.Equal(SharedAccessSignatureString, parser.SharedAccessSignatureString);
        }


        [Fact]
        public void connectionStringNullThrows()
        {
            Assert.Throws<ArgumentNullException>(() => ServiceConnectionStringParser.Create(null));
        }

        [Fact]
        public void connectionStringMissingHostnameThrows()
        {
            Assert.Throws<ArgumentException>(() => ServiceConnectionStringParser.Create(connectionStringWithoutHostname));
        }
    }
}
