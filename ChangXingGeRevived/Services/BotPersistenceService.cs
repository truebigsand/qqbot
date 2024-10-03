using ChangXingGeRevived.Models;
using Lagrange.Core.Common;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChangXingGeRevived.Services;

public class BotPersistenceService
{
    private static readonly JsonSerializerOptions preserveReferenceHanlerJsonSerializerOptions = new()
    {
        ReferenceHandler = ReferenceHandler.Preserve
    };
    private readonly AppConfig _config;

    public BotPersistenceService(IOptions<AppConfig> appConfig)
    {
        _config = appConfig.Value;
    }

    public BotDeviceInfo GetDeviceInfo()
    {
        if (File.Exists(_config.DeviceInfoPath))
        {
            var info = JsonSerializer.Deserialize<BotDeviceInfo>(File.ReadAllText(_config.DeviceInfoPath));
            if (info != null) return info;
        }

        var deviceInfo = BotDeviceInfo.GenerateInfo();
        File.WriteAllText(_config.DeviceInfoPath, JsonSerializer.Serialize(deviceInfo));
        return deviceInfo;
    }

    public void SaveKeystore(BotKeystore keystore) =>
        File.WriteAllText(_config.KeystorePath, JsonSerializer.Serialize(keystore));

    public BotKeystore LoadKeystore()
    {
        try
        {
            var text = File.ReadAllText(_config.KeystorePath);
            return JsonSerializer.Deserialize<BotKeystore>(text, preserveReferenceHanlerJsonSerializerOptions) ?? new BotKeystore();
        }
        catch
        {
            return new BotKeystore();
        }
    }
}
