namespace TimePlanning.Pn.Infrastructure.Models.RegistrationDevice;

public class TimePlanningRegistrationDeviceModel
{
    public int Id { get; set; }
    public string SoftwareVersion { get; set; }
    public string Model { get; set; }
    public string Manufacturer { get; set; }
    public string OsVersion { get; set; }
    public string LastIp { get; set; }
    public string LastKnownLocation { get; set; }
    public bool OtpEnabled { get; set; }
    public string OtpCode { get; set; }
}