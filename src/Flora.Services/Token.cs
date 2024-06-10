namespace Flora.Services
{
    using AiCodo;
    using Newtonsoft.Json;
    using System;
    public class Token
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
        [JsonProperty("token_type")]
        public string TokenType { get; set; }
        [JsonProperty("profile")]
        public DynamicEntity Profile { get; set; }
        [JsonProperty("expires_at")]
        public DateTime ExpiresAt { get; set; }
    }
}
