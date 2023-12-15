// DragonFruit.Data Copyright DragonFruit Network
// Licensed under the MIT License. Please refer to the LICENSE file at the root of this project for details

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using DragonFruit.Data.Converters;
using DragonFruit.Data.Requests;
using DragonFruit.Data.Tests.Requests;
using Xunit;

namespace DragonFruit.Data.Tests
{
    public class RequestBuildingTests
    {
        [Theory]
        [InlineData(typeof(BasicEchoRequest))]
        [InlineData(typeof(InheritedEchoRequest))]
        public void TestBasicEchoRequest(Type requestType)
        {
            var request = Activator.CreateInstance(requestType) as ApiRequest;

            Assert.NotNull(request);

            using var sourceGenMessage = ((IRequestBuilder)request).BuildRequest(null);
            using var reflectionGenMessage = ReflectionRequestMessageBuilder.CreateHttpRequestMessage(request, null);

            Assert.NotNull(sourceGenMessage.RequestUri);
            Assert.NotNull(reflectionGenMessage.RequestUri);

            // test reflection-generated requests match source-generated ones.
            Assert.Equal(sourceGenMessage.RequestUri, reflectionGenMessage.RequestUri);

            // test query string contains correct parameters
            Assert.Contains("q1=test_query_1", sourceGenMessage.RequestUri.Query);
            Assert.Contains("q2=test_query_2", sourceGenMessage.RequestUri.Query);
            Assert.Contains("q3=test_query_3", sourceGenMessage.RequestUri.Query); // static property
        }

        [Fact]
        public async void TestInheritedRequest()
        {
            var request = new InheritedEchoRequest();

            using var sourceGenMessage = ((IRequestBuilder)request).BuildRequest(null);
            using var reflectionGenMessage = ReflectionRequestMessageBuilder.CreateHttpRequestMessage(request, null);

            Assert.NotNull(sourceGenMessage.Content);
            Assert.NotNull(reflectionGenMessage.Content);

            // check form contents match
            var sourceGenContent = await sourceGenMessage.Content.ReadAsStringAsync();
            var reflectionGenContent = await reflectionGenMessage.Content.ReadAsStringAsync();

            Assert.Equal(sourceGenContent, reflectionGenContent);
        }

        [Fact]
        public async void TestMultipartFormRequest()
        {
            // actually send requests because it's easier than inspecting the multipart content
            using var httpClient = new HttpClient();

            var request = new MultipartFormRequest();
            var processedResponses = new List<byte[]>();

            var formats = new[]
            {
                // sourcegen
                ((IRequestBuilder)request).BuildRequest(null),

                // reflection
                ReflectionRequestMessageBuilder.CreateHttpRequestMessage(request, null)
            };

            foreach (var message in formats)
            {
                JsonObject json;

                using (message)
                {
                    using var response = await httpClient.SendAsync(message);
                    using var contentStream = await response.Content.ReadAsStreamAsync();

                    json = await JsonSerializer.DeserializeAsync<JsonObject>(contentStream);
                }

                // check querystring
                Assert.Equal("content", json["args"]["c"].ToString());

                // check form contents
                Assert.Equal("content", json["form"]["file"].ToString());
                Assert.Equal("content", json["form"]["bytes"].ToString());

                json.Remove("headers");
                processedResponses.Add(JsonSerializer.SerializeToUtf8Bytes(json));
            }

            // compare remaining json
            Assert.All(processedResponses, x => Assert.Equal(processedResponses[0], x));
        }

        [Fact]
        public void TestSpecialTypeHandling()
        {
            var request = new SpecialTypeRequest();

            using var sourceGenMessage = ((IRequestBuilder)request).BuildRequest(null);
            using var reflectionGenMessage = ReflectionRequestMessageBuilder.CreateHttpRequestMessage(request, null);

            // check query strings match expected output
            Assert.Equal(sourceGenMessage.RequestUri!.Query, reflectionGenMessage.RequestUri!.Query);

            Assert.Contains("users=test:test_1a", sourceGenMessage.RequestUri.Query);
            Assert.Contains("ids=1,2", sourceGenMessage.RequestUri.Query);
        }
    }
}