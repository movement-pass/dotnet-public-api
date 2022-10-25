namespace MovementPass.Public.Api.Features;

using System;

public class PassItem
{
    public string Id { get; set; }

    public string FromLocation { get; set; }

    public string ToLocation { get; set; }

    public int District { get; set; }

    public int Thana { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public string Type { get; set; }

    public string Reason { get; set; }

    public bool IncludeVehicle { get; set; }

    public string VehicleNo { get; set; }

    public bool SelfDriven { get; set; }

    public string DriverName { get; set; }

    public string DriverLicenseNo { get; set; }

    public string Status { get; set; }

    public DateTime CreatedAt { get; set; }
}