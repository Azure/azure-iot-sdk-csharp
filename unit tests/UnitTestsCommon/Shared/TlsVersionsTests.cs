using FluentAssertions;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Net;
using System.Security.Authentication;
using Xunit;

namespace UnitTestsCommon.Shared
{
    // By setting a collection, it causes xunit to not run in parallel, which we need because these test a static object
    [Collection("TlsVersions")]
    [Trait("Unit", "")]
    public class TlsVersionsTests
    {
        [Fact]
        public void MinimumTlsVersions_DefaultsToTls12()
        {
            // assert
            TlsVersions.MinimumTlsVersions.Should().Be(SslProtocols.Tls12);
        }

        [Fact]
        public void Preferred_DefaultsToNone()
        {
            // assert
            TlsVersions.Preferred.Should().Be(SslProtocols.None);
        }

#if NET451
        [Fact]
        public void SetLegacyAcceptableVersions_Sets()
        {
            // arrange
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11;

            // act
            TlsVersions.SetLegacyAcceptableVersions();

            // assert
            ServicePointManager.SecurityProtocol.Should().Be(SecurityProtocolType.Tls12);
        }
#endif

        [Fact]
        public void SetMinimumTlsVersions_CanSetToNone()
        {
            // arrange

            // Need to change it to something other than none to know that it can be set back
            TlsVersions.SetMinimumTlsVersions(SslProtocols.Tls12);

            // act
            TlsVersions.SetMinimumTlsVersions(SslProtocols.None);

            // assert
            TlsVersions.MinimumTlsVersions.Should().Be(SslProtocols.Tls12);
            TlsVersions.Preferred.Should().Be(SslProtocols.None);
        }

        [Fact]
        public void SetMinimumTlsVersions_CanSetToTls12()
        {
            // arrange
            const SslProtocols expected = SslProtocols.Tls12;

            // act
            TlsVersions.SetMinimumTlsVersions(expected);

            // assert
            TlsVersions.MinimumTlsVersions.Should().Be(expected);
            TlsVersions.Preferred.Should().Be(expected);
        }

        [Theory]
        [InlineData(SslProtocols.Tls)]
        [InlineData(SslProtocols.Tls11)]
        [InlineData(SslProtocols.Tls | SslProtocols.Tls11)]
        [InlineData(SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12)]
        public void SetMinimumTlsVersions_CanSetToOlderTls(SslProtocols protocol)
        {
            // arrange
            SslProtocols expected = protocol | SslProtocols.Tls12;

            // act
            TlsVersions.SetMinimumTlsVersions(protocol);

            // assert
            TlsVersions.MinimumTlsVersions.Should().Be(expected);
            TlsVersions.Preferred.Should().Be(expected);
        }

#pragma warning disable 0618
        [Theory]
        [InlineData(SslProtocols.Ssl2)]
        [InlineData(SslProtocols.Ssl3)]
        [InlineData(SslProtocols.Ssl2 | SslProtocols.Ssl3)]
        public void SetMinimumTlsVersions_CannotSetOther(SslProtocols protocol)
        {
            // act
            TlsVersions.SetMinimumTlsVersions(protocol);

            // assert
            TlsVersions.MinimumTlsVersions.Should().Be(SslProtocols.Tls12);
            TlsVersions.Preferred.Should().Be(SslProtocols.None);
        }

#if NETSTANDARD2_1
        [Fact]
        public void SetMinimumTlsVersions_CannotSetDefault()
        {
            // act

            // SslProtocols.Default is a combination of Ssl3 and Tls (1.0).
            // Ssl3 is not allowed, but Tls (1.0) is, so Ssl3 should be filtered out.
            TlsVersions.SetMinimumTlsVersions(SslProtocols.Default);

            // assert
            var expected = SslProtocols.Tls | SslProtocols.Tls12;
            TlsVersions.MinimumTlsVersions.Should().Be(expected);
            TlsVersions.Preferred.Should().Be(expected);
        }
#endif
#pragma warning restore 0618
    }
}
