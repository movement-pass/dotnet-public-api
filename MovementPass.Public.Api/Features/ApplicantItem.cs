namespace MovementPass.Public.Api.Features
{
    using System;

    public class ApplicantItem
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public int District { get; set; }

        public int Thana { get; set; }

        public DateTime DateOfBirth { get; set; }

        public string Gender { get; set; }

        public string IdType { get; set; }

        public string IdNumber { get; set; }

        public string Photo { get; set; }

        public DateTime CreatedAt { get; set; }

        public int AppliedCount { get; set; }

        public int ApprovedCount { get; set; }

        public int RejectedCount { get; set; }
    }
}