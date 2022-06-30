// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client.Common
{
    /// <summary>
    /// URL encoded serializer for message properties.
    /// </summary>
    public class UrlEncodedDictionarySerializer
    {
        /// <summary>
        /// The character that separates the name and value of a property.
        /// </summary>
        public const char KeyValueSeparator = '=';

        /// <summary>
        /// The character that separates different properties.
        /// </summary>
        public const char PropertySeparator = '&';

        /// <summary>
        /// The character that marks the start of a query string.
        /// </summary>
        public const string QueryStringIdentifier = "?";

        /// <summary>
        /// The length of property separator string.
        /// </summary>
        public const int PropertySeparatorLength = 1;

        // We assume that in average 20% of strings are the encoded characters.
        private const float EncodedSymbolsFactor = 1.2f;

        private readonly IDictionary<string, string> _output;
        private readonly Tokenizer _tokenizer;

        private UrlEncodedDictionarySerializer(
            IDictionary<string, string> output,
            string value,
            int startIndex)
        {
            _output = output;
            _tokenizer = new Tokenizer(value, startIndex);
        }

        private void Deserialize()
        {
            string key = null;

            foreach (Token token in _tokenizer.GetTokens())
            {
                if (token.Type == TokenType.Key)
                {
                    key = token.Value;
                }
                if (token.Type == TokenType.Value)
                {
                    _output[key] = token.Value;
                }
            }
        }

        /// <summary>
        /// Deserialize the string of properties to a dictionary.
        /// </summary>
        /// <param name="value">Value to deserialize.</param>
        /// <param name="startIndex">Index in the value to deserialize from.</param>
        /// <returns>Deserialized dictionary of properties.</returns>
        public static Dictionary<string, string> Deserialize(string value, int startIndex)
        {
            var properties = new Dictionary<string, string>();
            Deserialize(value, startIndex, properties);
            return properties;
        }

        /// <summary>
        /// Deserialize the string of properties to a dictionary.
        /// </summary>
        /// <param name="value">Value to deserialize.</param>
        /// <param name="startIndex">Index in the value to deserialize from.</param>
        /// <param name="properties">The output dictionary.</param>
        /// <returns>Deserialized dictionary of properties.</returns>
        public static void Deserialize(string value, int startIndex, IDictionary<string, string> properties)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "The value parameter cannot be null.");
            }

            if (value.Length < startIndex)
            {
                return;
            }

            var parser = new UrlEncodedDictionarySerializer(properties, value, startIndex);
            parser.Deserialize();
        }

        /// <summary>
        /// Serializes the dictionary to a string with URL encoding.
        /// </summary>
        /// <param name="properties">The dictionary to serialize.</param>
        /// <returns>Serialized(URL encoded) string of properties.</returns>
        public static string Serialize(IEnumerable<KeyValuePair<string, string>> properties)
        {
            IList<KeyValuePair<string, string>> keyValuePairs = properties as IList<KeyValuePair<string, string>> ?? properties.ToList();
            if (properties == null || !keyValuePairs.Any())
            {
                return string.Empty;
            }

            KeyValuePair<string, string>? firstProperty = null;
            int propertiesCount = 0;
            int estimatedLength = 0;
            foreach (KeyValuePair<string, string> property in keyValuePairs)
            {
                if (propertiesCount == 0)
                {
                    firstProperty = property;
                }

                // In case of value, '=' and ',' take up length, otherwise just ','
                estimatedLength += property.Key.Length + (property.Value?.Length + 2 ?? 1);
                propertiesCount++;
            }

            // Optimization for most common case: only correlation Id is present
            if (propertiesCount == 1 && firstProperty.HasValue)
            {
                return firstProperty.Value.Value == null
                    ? Uri.EscapeDataString(firstProperty.Value.Key)
                    : Uri.EscapeDataString(firstProperty.Value.Key) + KeyValueSeparator + Uri.EscapeDataString(firstProperty.Value.Value);
            }

            var propertiesBuilder = new StringBuilder((int)(estimatedLength * EncodedSymbolsFactor));

            foreach (KeyValuePair<string, string> property in keyValuePairs)
            {
                propertiesBuilder.Append(Uri.EscapeDataString(property.Key));
                if (property.Value != null)
                {
                    propertiesBuilder
                        .Append(KeyValueSeparator)
                        .Append(Uri.EscapeDataString(property.Value));
                }
                propertiesBuilder.Append(PropertySeparator);
            }
            return propertiesBuilder.Length == 0
                ? string.Empty
                : propertiesBuilder.ToString(0, propertiesBuilder.Length - PropertySeparatorLength);
        }

        /// <summary>
        /// The token type.
        /// </summary>
        public enum TokenType
        {
            /// <summary>
            /// Property name token.
            /// </summary>
            Key,

            /// <summary>
            /// Property value token.
            /// </summary>
            Value,
        }

        private struct Token
        {
            public TokenType Type { get; }
            public string Value { get; }

            public Token(TokenType tokenType, string value)
            {
                Type = tokenType;
                Value = value == null ? null : Uri.UnescapeDataString(value);
            }
        }

        /// <summary>
        /// Tokenizer state machine
        /// </summary>
        private class Tokenizer
        {
            private enum TokenizerState
            {
                ReadyToReadKey,
                ReadKey,
                ReadValue,
                Error,
                Finish
            }

            private readonly string _value;
            private int _position;

            private TokenizerState _currentState = TokenizerState.ReadyToReadKey;

            public Tokenizer(string value, int startIndex)
            {
                _value = value;
                _position = startIndex;
            }

            public IEnumerable<Token> GetTokens()
            {
                if (_position >= _value.Length)
                {
                    yield break;
                }
                int readCount = 0;
                bool readCompleted = false;
                string errorMessage = null;
                while (!readCompleted)
                {
                    switch (_currentState)
                    {
                        case TokenizerState.ReadyToReadKey:
                            {
                                if (_position >= _value.Length)
                                {
                                    errorMessage = "Unexpected string end in '{0}' state.".FormatInvariant(_currentState);
                                    _currentState = TokenizerState.Error;
                                    break;
                                }
                                char currentChar = _value[_position];
                                switch (currentChar)
                                {
                                    case '=':
                                    case '&':
                                        errorMessage = "Unexpected character '{0}' in '{1}' state.".FormatInvariant(currentChar, _currentState);
                                        _currentState = TokenizerState.Error;
                                        break;

                                    case '/':
                                        _currentState = TokenizerState.Finish;
                                        break;

                                    default:
                                        readCount++;
                                        _currentState = TokenizerState.ReadKey;
                                        break;
                                }
                                break;
                            }

                        case TokenizerState.ReadKey:
                            {
                                if (_position >= _value.Length)
                                {
                                    yield return CreateToken(TokenType.Key, readCount);
                                    yield return CreateToken(TokenType.Value, 0);
                                    readCount = 0;
                                    _currentState = TokenizerState.Finish;
                                    break;
                                }
                                char currentChar = _value[_position];
                                switch (currentChar)
                                {
                                    case '=':
                                        yield return CreateToken(TokenType.Key, readCount);
                                        readCount = 0;
                                        _currentState = TokenizerState.ReadValue;
                                        break;

                                    case '&':
                                        yield return CreateToken(TokenType.Key, readCount);
                                        yield return CreateToken(TokenType.Value, 0);
                                        readCount = 0;
                                        _currentState = TokenizerState.ReadyToReadKey;
                                        break;

                                    case '/':
                                        yield return CreateToken(TokenType.Key, readCount);
                                        yield return CreateToken(TokenType.Value, 0);
                                        readCount = 0;
                                        _currentState = TokenizerState.Finish;
                                        break;

                                    default:
                                        readCount++;
                                        break;
                                }
                                break;
                            }

                        case TokenizerState.ReadValue:
                            {
                                if (_position >= _value.Length)
                                {
                                    yield return CreateToken(TokenType.Value, readCount);
                                    readCount = 0;
                                    _currentState = TokenizerState.Finish;
                                    break;
                                }
                                char currentChar = _value[_position];
                                switch (currentChar)
                                {
                                    case '=':
                                        errorMessage = "Unexpected character '{0}' in '{1}' state.".FormatInvariant(currentChar, _currentState);
                                        _currentState = TokenizerState.Error;
                                        break;

                                    case '&':
                                        yield return CreateToken(TokenType.Value, readCount);
                                        readCount = 0;
                                        _currentState = TokenizerState.ReadyToReadKey;
                                        break;

                                    case '/':
                                        yield return CreateToken(TokenType.Value, readCount);
                                        readCount = 0;
                                        _currentState = TokenizerState.Finish;
                                        break;

                                    default:
                                        readCount++;
                                        break;
                                }
                                break;
                            }

                        case TokenizerState.Finish:
                        case TokenizerState.Error:
                            readCompleted = true;
                            break;

                        default:
                            throw new NotSupportedException();
                    }
                    _position++;
                }

                if (_currentState == TokenizerState.Error)
                {
                    throw new FormatException(errorMessage);
                }
            }

            private Token CreateToken(TokenType tokenType, int readCount)
            {
                // '?' is not a valid character for message property names or values, but instead signifies the start of a query string
                // in the case of an MQTT topic. For this reason, we'll replace the '?' from the property key before adding it into
                // application properties collection.
                // https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-construct
                string tokenValue = readCount == 0
                    ? null
                    : _value.Substring(_position - readCount, readCount).Replace(QueryStringIdentifier, string.Empty);

                return new Token(tokenType, tokenValue);
            }
        }
    }
}
