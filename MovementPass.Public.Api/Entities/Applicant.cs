namespace MovementPass.Public.Api.Entities;

using System;
using System.ComponentModel.DataAnnotations.Schema;

public class Applicant
{
    [Column("id")]
    public string Id { get; set; }

    [Column("name")]
    public string Name { get; set; }

    [Column("district")]
    public int District { get; set; }

    [Column("thana")]
    public int Thana { get; set; }

    [Column("dateOfBirth")]
    public DateTime DateOfBirth { get; set; }

    [Column("gender")]
    public string Gender { get; set; }

    [Column("idType")]
    public string IdType { get; set; }

    [Column("idNumber")]
    public string IdNumber { get; set; }

    [Column("photo")]
    public string Photo { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; }

    [Column("appliedCount")]
    public int AppliedCount { get; set; }

    [Column("approvedCount")]
    public int ApprovedCount { get; set; }

    [Column("rejectedCount")]
    public int RejectedCount { get; set; }
}