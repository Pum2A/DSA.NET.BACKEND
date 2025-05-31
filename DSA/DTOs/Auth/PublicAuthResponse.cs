using System;
using System.Collections.Generic;

namespace DSA.DTOs.Auth
{
    public class PublicAuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserDto? User { get; set; }
        public List<string>? Errors { get; set; }
    }
}