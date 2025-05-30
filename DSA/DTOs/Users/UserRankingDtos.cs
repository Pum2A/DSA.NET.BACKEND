using System;
using System.Collections.Generic;

namespace DSA.DTOs.Users
{
    public class UserRankingRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string OrderBy { get; set; } = "XpPoints"; // XpPoints, CompletedLessons, CompletedQuizzes
        public bool Descending { get; set; } = true;
    }

    public class UserRankingResponse
    {
        public List<UserRankingItemDto> Users { get; set; } = new();
        public int TotalUsers { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class UserRankingItemDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public int XpPoints { get; set; }
        public int CompletedLessons { get; set; }
        public int CompletedQuizzes { get; set; }
        public int CurrentStreak { get; set; }
        public int Rank { get; set; }
    }
}