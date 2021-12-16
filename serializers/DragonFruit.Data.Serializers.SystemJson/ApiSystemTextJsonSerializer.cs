﻿// DragonFruit.Data Copyright DragonFruit Network
// Licensed under the MIT License. Please refer to the LICENSE file at the root of this project for details

using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DragonFruit.Data.Serializers.SystemJson
{
    public class ApiSystemTextJsonSerializer : ApiSerializer, IAsyncSerializer
    {
        public override string ContentType => "application/json";

        // System.Text.Json doesn't accept different encoding
        public override Encoding Encoding => Encoding.UTF8;

        public JsonSerializerOptions SerializerOptions { get; set; }

        public override HttpContent Serialize<T>(T input)
        {
            var stream = GetStream(false);
            JsonSerializer.Serialize(stream, input, SerializerOptions);

            return GetHttpContent(stream);
        }

        public override T Deserialize<T>(Stream input) => JsonSerializer.Deserialize<T>(input, SerializerOptions);

        public Task<T> DeserializeAsync<T>(Stream input) where T : class => JsonSerializer.DeserializeAsync<T>(input, SerializerOptions).AsTask();

        /// <summary>
        /// Registers <see cref="JsonDocument"/> to always use the <see cref="ApiSystemTextJsonSerializer"/>
        /// </summary>
        public static void RegisterDefaults() => SerializerResolver.Register<JsonDocument, ApiSystemTextJsonSerializer>();
    }
}